using System.Collections;
using System.Diagnostics;
using System.Numerics;
using DemoFile.Sdk;

namespace DemoFile;

public readonly record struct GameTime_t(float Value) : IComparable<GameTime_t>
{
    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Value);

    public int CompareTo(GameTime_t other) => Value.CompareTo(other.Value);

    public override string ToString() => ToTimeSpan().ToString();
}

public readonly record struct GameTick_t(uint Value) : IComparable<GameTick_t>
{
    // CS2 is hardcoded as 64-tick
    public GameTime_t ToGameTime() => new(Value / 64.0f);

    public int CompareTo(GameTick_t other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Tick {Value} ({ToGameTime()})";
}

public readonly record struct CEntityIndex(uint Value);

public readonly record struct Vector(float X, float Y, float Z);

public readonly record struct NetworkedString(string Value)
{
    public static implicit operator string(NetworkedString @this) => @this.Value;
}

public class NetworkedVector<T> : IReadOnlyList<T> where T : new()
{
    private T[]? _values;
    private int _size;

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_values ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _size;

    public T this[int index]
    {
        get => _values![index];
        set => _values![index] = value;
    }

    public void EnsureSize(int length)
    {
        if (length > _size)
        {
            Resize(length);
        }
    }

    public void Resize(int length)
    {
        if (_values != null && length <= _values.Length)
        {
            _size = length;
            return;
        }

        var newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)length);
        var newValues = new T[newCapacity];

        // Copy the old values to the new array
        if (_values != null)
        {
            Debug.Assert(length > _values.Length, "guessed implementation! what to do on resize?");
            // todo: ensure we can only grow?
            new ReadOnlySpan<T>(_values).CopyTo(newValues);
        }

        _size = length;
        _values = newValues;
    }
}

public readonly record struct CHandle<T>(ulong Value) where T : CEntityInstance
{
    public CEntityIndex Index => new((uint) (Value & (DemoParser.MaxEdicts - 1)));
    public uint SerialNum => (uint)(Value >> DemoParser.MaxEdictBits);

    public static CHandle<T> FromIndexSerialNum(CEntityIndex index, uint serialNum) =>
        new(((ulong)index.Value) | (serialNum << DemoParser.MaxEdictBits));
}

public readonly record struct QAngle(float Pitch, float Yaw, float Roll);

public readonly record struct Vector2D(float X, float Y);

public readonly record struct Vector4D(float X, float Y, float Z, float W);

public readonly record struct Quaternion(float Value);

public readonly record struct CUtlStringToken(uint Value);

public readonly record struct CStrongHandle<T>(ulong Value);

public readonly record struct WorldGroupId_t(uint Value);

public readonly record struct AttachmentHandle_t(ulong Value);

public readonly record struct CTransform(float Value);

public readonly record struct CGameSceneNodeHandle(uint Value);

public readonly record struct HSequence(ulong Value);
