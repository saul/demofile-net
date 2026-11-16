using System.Buffers.Binary;
using System.Text;
using Google.Protobuf;

namespace DemoFile.Test.Unit;

/// <summary>
/// <c>svc_EncryptedData</c> decryption, verified against engine2 (patch 14197): ICE (<c>IceKey(2)</c>, ECB) and the
/// <c>CmdEncryptedDataMessageCodec</c> layout. See ENGINE_PARITY.md.
/// </summary>
[TestFixture]
public class EncryptedDataTest
{
    private const string PrivateKey = "0123456789ABCDEF";
    private const ulong PublicKeyValue = 0xFEDCBA9876543210;

    /// <summary>
    /// Outputs of engine2's own ICE implementation (<c>IceKey(2)</c>, <c>set</c>, <c>decrypt</c>, called directly
    /// from the DLL) for hex keys as the client formats them (<c>%016llX</c>) and arbitrary string keys.
    /// </summary>
    [TestCase("91B7584A2265B1F5", "cd613e30d8f16adf", "bc4f5a3352cd9939")]
    [TestCase("iGp@58WAm!dX3a5I", "cd447e35b8b6d8fe", "d498debb6f54b05c")]
    [TestCase("0000000000002222", "9755d4c13a902931", "9fed91f907983e2e")]
    [TestCase("B43ADC4FC7AF3626", "82458cc89f7a7daf", "f0d59f053ce54b29")]
    [TestCase("04A1BDE44806AA81", "336b1a45282ee0bc", "4685273ae8de9e2d")]
    [TestCase("7E0B6723524550A4", "1997e8f3edb924d8", "94db62af88f25443")]
    public void IceDecrypt_MatchesEngine(string key, string ciphertextHex, string expectedPlaintextHex)
    {
        var ice = new IceKey(2);
        ice.Set(Encoding.ASCII.GetBytes(key));

        var plaintext = new byte[8];
        ice.Decrypt(Convert.FromHexString(ciphertextHex), plaintext);

        Assert.That(Convert.ToHexString(plaintext), Is.EqualTo(expectedPlaintextHex).IgnoreCase);
    }

    [Test]
    public void IceEncrypt_RoundTrips()
    {
        var ice = new IceKey(2);
        ice.Set(Encoding.ASCII.GetBytes(PrivateKey));

        var random = new Random(1);
        var block = new byte[8];
        var encrypted = new byte[8];
        var decrypted = new byte[8];
        for (var i = 0; i < 100; i++)
        {
            random.NextBytes(block);
            ice.Encrypt(block, encrypted);
            ice.Decrypt(encrypted, decrypted);
            Assert.That(decrypted, Is.EqualTo(block));
        }
    }

    [Test]
    public void Key_FromUInt64_FormattedLikeTheClient()
    {
        // client.dll: "cl_decryptdata_key %016llX;"
        Assert.That(new DemoDecryptionKey(0x00AB_CDEF_0123_4567).Value, Is.EqualTo("00ABCDEF01234567"));

        DemoDecryptionKey implicitKey = PublicKeyValue;
        Assert.That(implicitKey.Value, Is.EqualTo("FEDCBA9876543210"));
    }

    [TestCase("")]
    [TestCase("0123456789ABCDE")]
    [TestCase("0123456789ABCDEF0")]
    public void Key_WrongLength_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => _ = new DemoDecryptionKey(key));
    }

    [Test]
    public void PrivateKey_DecryptsEmbeddedMessage()
    {
        var demo = new CsDemoParser { DecryptionKey = new DemoDecryptionKey(PrivateKey) };
        var chat = RecordChat(demo, out var encryptedCount);

        demo.OnDemoPacket(new BitBuffer(EncryptedPacket(Encrypt(SayText2("hello team"), PrivateKey, keyType: 1))));

        Assert.That(chat, Is.EqualTo(new[] { "hello team" }));
        Assert.That(encryptedCount(), Is.EqualTo(1), "raw svc_EncryptedData is still raised");
    }

    [Test]
    public void PublicKey_DecryptsEmbeddedMessage()
    {
        var demo = new CsDemoParser { PublicDecryptionKey = PublicKeyValue };
        var chat = RecordChat(demo, out _);

        demo.OnDemoPacket(new BitBuffer(EncryptedPacket(Encrypt(SayText2("hello all"), PublicKeyValue.ToString("X16"), keyType: 2))));

        Assert.That(chat, Is.EqualTo(new[] { "hello all" }));
    }

    [Test]
    public void MessagesAroundEncryptedData_StayInOrder()
    {
        var demo = new CsDemoParser { DecryptionKey = new DemoDecryptionKey(PrivateKey) };
        var chat = RecordChat(demo, out _);

        demo.OnDemoPacket(new BitBuffer(Packet(
            (118, SayText2("before")),
            (78, Encrypt(SayText2("secret"), PrivateKey, keyType: 1)),
            (118, SayText2("after")))));

        Assert.That(chat, Is.EqualTo(new[] { "before", "secret", "after" }));
    }

    [TestCase(null, null, 1)]
    [TestCase("FFFFFFFFFFFFFFFF", null, 1)] // wrong key
    [TestCase(null, PrivateKey, 1)] // public key set, but the message uses the private key
    [TestCase(PrivateKey, null, 3)] // unknown key type
    public void WithoutTheRightKey_MessageIsSkipped(string? privateKey, string? publicKey, int keyType)
    {
        var demo = new CsDemoParser
        {
            DecryptionKey = privateKey == null ? null : new DemoDecryptionKey(privateKey),
            PublicDecryptionKey = publicKey == null ? null : new DemoDecryptionKey(publicKey),
        };
        var chat = RecordChat(demo, out var encryptedCount);

        demo.OnDemoPacket(new BitBuffer(EncryptedPacket(Encrypt(SayText2("hello team"), PrivateKey, keyType))));

        Assert.That(chat, Is.Empty);
        Assert.That(encryptedCount(), Is.EqualTo(1));
    }

    [Test]
    public void ZeroPadding_IsIgnored()
    {
        // The engine treats a zero padding length as "no message"
        var plaintext = new byte[16];
        Assert.That(DemoParser<CsDemoParser>.TryGetEmbeddedMessage(plaintext, out _), Is.False);
    }

    [TestCase(-1)]
    [TestCase(1)]
    public void LengthMismatch_IsIgnored(int sizeDelta)
    {
        // [padding length 3][3 padding bytes][size][8 message bytes]: the size must account for the rest exactly
        var plaintext = new byte[1 + 3 + 4 + 8];
        plaintext[0] = 3;
        BinaryPrimitives.WriteInt32BigEndian(plaintext.AsSpan(4), 8 + sizeDelta);
        Assert.That(DemoParser<CsDemoParser>.TryGetEmbeddedMessage(plaintext, out _), Is.False);

        BinaryPrimitives.WriteInt32BigEndian(plaintext.AsSpan(4), 8);
        Assert.That(DemoParser<CsDemoParser>.TryGetEmbeddedMessage(plaintext, out var embedded), Is.True);
        Assert.That(embedded.Length, Is.EqualTo(8));
    }

    private static List<string> RecordChat(CsDemoParser demo, out Func<int> encryptedCount)
    {
        var chat = new List<string>();
        demo.BaseUserMessageEvents.UserMessageSayText2 += msg => chat.Add(msg.Param2);

        var count = 0;
        demo.PacketEvents.SvcEncryptedData += _ => count++;
        encryptedCount = () => count;
        return chat;
    }

    private static byte[] SayText2(string text) => new CUserMessageSayText2
    {
        Entityindex = 1,
        Chat = true,
        Messagename = "Cstrike_Chat_All",
        Param1 = "player",
        Param2 = text,
    }.ToByteArray();

    /// <summary>
    /// Net messages packed as in a packet: each is <c>UBitVar</c> type, <c>varint</c> size, bytes, with no
    /// byte alignment between them.
    /// </summary>
    private static byte[] Packet(params (uint MsgType, byte[] Payload)[] messages)
    {
        var writer = new TestBitWriter();
        foreach (var (msgType, payload) in messages)
        {
            writer.UBitVar(msgType).VarInt((ulong)payload.Length);
            foreach (var b in payload)
                writer.Bits(b, 8);
        }

        return writer.ToArray();
    }

    private static byte[] Framed(uint msgType, byte[] payload) => Packet((msgType, payload));

    private static byte[] EncryptedPacket(byte[] encryptedData) => Framed(78, encryptedData);

    /// <summary>
    /// Build an <c>svc_EncryptedData</c> message as engine2's <c>SVCMsg_EncryptedData_EncryptMessage</c> does:
    /// <c>[padding length][random padding][uint32 big-endian size][framed message]</c>, padded to the ICE block size
    /// and encrypted block by block.
    /// </summary>
    private static byte[] Encrypt(byte[] sayText2, string key, int keyType)
    {
        var embedded = Framed(118, sayText2); // UM_SayText2

        var random = new Random(embedded.Length);
        var r = random.Next(0x10, 0x49);
        var padding = IceKey.BlockSize + (r - (embedded.Length + 5 + r) % IceKey.BlockSize);

        var plaintext = new byte[1 + padding + 4 + embedded.Length];
        plaintext[0] = (byte)padding;
        for (var i = 0; i < padding; i++)
            plaintext[1 + i] = (byte)random.Next(0x10, 0xfb);
        BinaryPrimitives.WriteInt32BigEndian(plaintext.AsSpan(1 + padding), embedded.Length);
        embedded.CopyTo(plaintext, 1 + padding + 4);
        Assert.That(plaintext.Length % IceKey.BlockSize, Is.Zero);

        var ice = new IceKey(2);
        ice.Set(Encoding.ASCII.GetBytes(key));
        var ciphertext = new byte[plaintext.Length];
        for (var offset = 0; offset < plaintext.Length; offset += IceKey.BlockSize)
            ice.Encrypt(plaintext.AsSpan(offset, IceKey.BlockSize), ciphertext.AsSpan(offset, IceKey.BlockSize));

        return new CSVCMsg_EncryptedData
        {
            Encrypted = ByteString.CopyFrom(ciphertext),
            KeyType = keyType,
        }.ToByteArray();
    }
}
