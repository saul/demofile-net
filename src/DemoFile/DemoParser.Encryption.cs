using System.Buffers.Binary;

namespace DemoFile;

public partial class DemoParser<TGameParser>
{
    // Engine limit on the size of an encrypted message (engine2 CmdEncryptedDataMessageCodec)
    private const int MaxEncryptedDataBytes = 0x138800;

    private DemoDecryptionKey? _decryptionKey;
    private DemoDecryptionKey? _publicDecryptionKey;
    private IceKey? _ice;
    private IceKey? _publicIce;

    /// <summary>
    /// Key for <c>svc_EncryptedData</c> messages encrypted with the server's <c>tv_encryptdata_key</c>
    /// (<c>key_type</c> 1, the engine's <c>cl_decryptdata_key</c>). Used for private communication such as team chat.
    /// </summary>
    /// <remarks>
    /// Encrypted messages are decrypted and raised through their usual events (e.g. a chat user message).
    /// Without the key, or with the wrong key, they are skipped, as the engine does.
    /// The key isn't recorded in demos or broadcasts: the game client receives it from the Game Coordinator
    /// (<c>WatchableMatchInfo.cl_decryptdata_key</c>, also in a matchmaking demo's <c>.dem.info</c> file),
    /// and community servers set it with <c>tv_encryptdata_key</c>.
    /// </remarks>
    public DemoDecryptionKey? DecryptionKey
    {
        get => _decryptionKey;
        set
        {
            _decryptionKey = value;
            _ice = CreateIceKey(value);
        }
    }

    /// <summary>
    /// Key for <c>svc_EncryptedData</c> messages encrypted with the server's <c>tv_encryptdata_key_pub</c>
    /// (<c>key_type</c> 2, the engine's <c>cl_decryptdata_key_pub</c>). Used for public communication.
    /// See <see cref="DecryptionKey"/>.
    /// </summary>
    public DemoDecryptionKey? PublicDecryptionKey
    {
        get => _publicDecryptionKey;
        set
        {
            _publicDecryptionKey = value;
            _publicIce = CreateIceKey(value);
        }
    }

    private static IceKey? CreateIceKey(DemoDecryptionKey? key)
    {
        if (key is not { } k)
            return null;

        var ice = new IceKey(2);
        ice.Set(k.GetBytes());
        return ice;
    }

    /// <summary>
    /// Decrypt an <c>svc_EncryptedData</c> message and dispatch the message embedded in it.
    /// Mirrors engine2's <c>CmdEncryptedDataMessageCodec</c>: the payload is ICE-encrypted (ECB, 8-byte blocks) and
    /// decrypts to <c>[padding length][padding][uint32 big-endian size][message]</c>, where the message is framed
    /// like one in a packet (<c>UBitVar</c> type, <c>varint</c> size, protobuf bytes).
    /// </summary>
    private void OnEncryptedData(CSVCMsg_EncryptedData msg)
    {
        var ice = msg.KeyType switch
        {
            1 => _ice,
            2 => _publicIce,
            _ => null
        };

        if (ice == null || !msg.HasEncrypted)
            return;

        var encrypted = msg.Encrypted.Span;
        if (encrypted.Length == 0 || encrypted.Length % IceKey.BlockSize != 0 || encrypted.Length > MaxEncryptedDataBytes)
            return;

        var rented = _bytePool.Rent(encrypted.Length);
        try
        {
            var data = rented.AsSpan(..encrypted.Length);
            encrypted.CopyTo(data);
            ice.DecryptInPlace(data);

            if (!TryGetEmbeddedMessage(data, out var embedded))
                return;

            var buffer = new BitBuffer(embedded);
            var msgType = (int)buffer.ReadUBitVar();
            var size = (int)buffer.ReadUVarInt32();
            if (size < 0 || size > buffer.RemainingBytes)
                return;

            var msgRented = _bytePool.Rent(size);
            try
            {
                var msgBuf = msgRented.AsSpan(..size);
                buffer.ReadBytes(msgBuf);
                DispatchNetMessage(msgType, msgBuf);
            }
            finally
            {
                _bytePool.Return(msgRented);
            }
        }
        finally
        {
            _bytePool.Return(rented);
        }
    }

    /// <summary>
    /// Strip the padding and length header from decrypted <c>svc_EncryptedData</c>.
    /// Returns <c>false</c> for anything the engine would ignore, which includes data decrypted with the wrong key.
    /// </summary>
    internal static bool TryGetEmbeddedMessage(ReadOnlySpan<byte> decrypted, out ReadOnlySpan<byte> embedded)
    {
        embedded = default;
        if (decrypted.IsEmpty)
            return false;

        // The engine encrypts with at least 16 bytes of padding; a zero padding length means "no message"
        var padding = decrypted[0];
        if (padding == 0)
            return false;

        var headerLength = padding + 1;
        if (decrypted.Length <= headerLength + 4)
            return false;

        var size = BinaryPrimitives.ReadInt32BigEndian(decrypted[headerLength..]);
        if (size < 0 || (long)headerLength + 4 + size != decrypted.Length)
            return false;

        embedded = decrypted.Slice(headerLength + 4, size);
        return true;
    }
}
