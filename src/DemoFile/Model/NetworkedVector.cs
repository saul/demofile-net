using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace DemoFile;

[DebuggerDisplay("Count = {Count}")]
public class NetworkedVector<T> : IReadOnlyList<T?> where T : new()
{
    private struct NetworkedVectorEnumerator : IEnumerator<T>
    {
        private readonly NetworkedVector<T> _vector;
        private int _index;

        public NetworkedVectorEnumerator(NetworkedVector<T> vector)
        {
            _vector = vector;
            _index = -1;
        }

        public bool MoveNext()
        {
            for (;;)
            {
                _index += 1;
                if (_index >= _vector.Count)
                    return false;

                if (_vector[_index] != null)
                    return true;
            }
        }

        public void Reset() => _index = -1;

        public T Current => _vector[_index]!;

        object IEnumerator.Current => Current!;

        public void Dispose()
        {
        }
    }

    private T?[]? _values;
    private int _size;

    public IEnumerator<T> GetEnumerator() => new NetworkedVectorEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _size;

    public T? this[int index]
    {
        get => _values![index];
        internal set => _values![index] = value;
    }

    internal void EnsureSize(int length)
    {
        if (length > _size)
        {
            Resize(length);
        }
    }

    internal void Resize(int length)
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
            new ReadOnlySpan<T?>(_values).CopyTo(newValues);
        }

        _size = length;
        _values = newValues;
    }
}