using System.Text;

namespace DemoFile;

/// <summary>
/// A key for decrypting <c>svc_EncryptedData</c> messages: the value of the server's <c>tv_encryptdata_key</c>
/// (or <c>tv_encryptdata_key_pub</c>), which the CS2 client knows as <c>cl_decryptdata_key</c>
/// (or <c>cl_decryptdata_key_pub</c>).
/// </summary>
/// <remarks>
/// The engine uses the key's 16 characters as a 128-bit ICE key. Keys handed out by the Game Coordinator are 64-bit
/// numbers (e.g. <c>WatchableMatchInfo.cl_decryptdata_key</c>, also found in a matchmaking demo's <c>.dem.info</c>
/// file), which the client formats as 16 upper-case hex digits; construct those with <see cref="DemoDecryptionKey(ulong)"/>.
/// </remarks>
public readonly struct DemoDecryptionKey : IEquatable<DemoDecryptionKey>
{
    /// <summary>Length of a key, in bytes.</summary>
    public const int Length = 16;

    private readonly string? _value;

    /// <summary>A key given as the 16-character string set in the server's <c>tv_encryptdata_key</c> convar.</summary>
    /// <exception cref="ArgumentException">The key isn't 16 bytes long (as UTF-8).</exception>
    public DemoDecryptionKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var byteCount = Encoding.UTF8.GetByteCount(key);
        if (byteCount != Length)
        {
            throw new ArgumentException(
                $"Decryption key must be {Length} bytes long, got {byteCount}. Keys from the Game Coordinator are 64-bit numbers: pass those as a ulong.",
                nameof(key));
        }

        _value = key;
    }

    /// <summary>A key given as a 64-bit number, as sent by the Game Coordinator (formatted as <c>%016llX</c>).</summary>
    public DemoDecryptionKey(ulong key) : this(key.ToString("X16"))
    {
    }

    public static implicit operator DemoDecryptionKey(ulong key) => new(key);

    /// <summary>The key as the engine sees it (the value of <c>cl_decryptdata_key</c>).</summary>
    public string Value => _value ?? throw new InvalidOperationException("Uninitialised decryption key");

    internal byte[] GetBytes() => Encoding.UTF8.GetBytes(Value);

    public bool Equals(DemoDecryptionKey other) => string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is DemoDecryptionKey other && Equals(other);

    public override int GetHashCode() => _value is null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

    public static bool operator ==(DemoDecryptionKey left, DemoDecryptionKey right) => left.Equals(right);

    public static bool operator !=(DemoDecryptionKey left, DemoDecryptionKey right) => !left.Equals(right);

    public override string ToString() => _value ?? "";
}
