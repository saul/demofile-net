using System.Collections;
using System.ComponentModel;
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
        [EditorBrowsable(EditorBrowsableState.Advanced)] set
        {
            Debug.Assert(index < _values.Count);
            Version++;
            _array![index] = value;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public void EnsureSize(int length)
    {
        if (length > _values.Count)
        {
            //Debug.Assert(length == _values.Count + 1);
            Resize(length);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public void Resize(int length)
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
        Array.Resize(ref _array, newCapacity);
        _values = new ArraySegment<T?>(_array, 0, length);
    }
}
