using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace DemoFile;

[DebuggerDisplay("Count = {Count}")]
public class NetworkedVector<T> : IReadOnlyList<T?>
    where T : new()
{
    public int Version { get; private set; }

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

    private T?[]? _array;
    private ArraySegment<T?> _values = ArraySegment<T?>.Empty;

    public IEnumerator<T> GetEnumerator() => new NetworkedVectorEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _values.Count;

    public T? this[int index]
    {
        get => _values[index];
        internal set
        {
            Debug.Assert(index < _values.Count);
            Version++;
            _array![index] = value;
        }
    }

    internal void EnsureSize(int length)
    {
        if (length > _values.Count)
        {
            // todo: this assertion shouldn't fail - bad baselines?
            //Debug.Assert(length == _values.Count + 1);
            Resize(length);
        }
    }

    internal void Resize(int length)
    {
        Version++;

        if (length == 0)
        {
            // Drop the reference to the values - let the GC clear it up
            _array = null;
            _values = ArraySegment<T?>.Empty;

            return;
        }

        // Can we accommodate this resize with our existing capacity?
        if (_array != null && length <= _array.Length)
        {
            // Vector is shrinking - clear removed values
            if (length < _values.Count)
            {
                ((Span<T?>)_array)[length.._values.Count].Clear();
            }

            _values = new ArraySegment<T?>(_array, 0, length);
            return;
        }

        // If we're here, it's to grow the backing array
        Debug.Assert(_array == null || length > _array.Length);

        var newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)length);
        var newArray = new T[newCapacity];

        // Copy the old values to the new, larger backing array
        if (_array != null)
        {
            ((ReadOnlySpan<T?>)_array).CopyTo(newArray);
        }

        _array = newArray;
        _values = new ArraySegment<T?>(newArray, 0, length);
    }
}
