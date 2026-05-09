using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DemoFile;

/// <summary>
/// A list of ints representing a path through nested fields on server classes.
/// </summary>
internal struct FieldPath : IReadOnlyList<int>
{
    public static readonly FieldPath Default = new() { _size = 1, _storage = new InlineStorage { _element0 = -1 } };

    private InlineStorage _storage;
    private byte _size; // byte is sufficient for max size 7

    [InlineArray(7)]
    private struct InlineStorage
    {
        internal int _element0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int item)
    {
        if (_size >= 7)
            ThrowPathFull();

        _storage[_size++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop(int count)
    {
        if (count > _size)
            ThrowInvalidPop(count);

        _size -= (byte)count;
    }

    public int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            if ((uint)index >= (uint)_size)
                ThrowIndexOutOfRange(index);

            return Unsafe.Add(ref Unsafe.AsRef(in _storage._element0), index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)_size)
                ThrowIndexOutOfRange(index);

            Unsafe.Add(ref _storage._element0, index) = value;
        }
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<int> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _storage._element0), _size);

    public readonly override string ToString() => _size == 0
        ? "(empty)"
        : "/" + string.Join('/', this);

    public readonly IEnumerator<int> GetEnumerator() => new Enumerator(in this);
    
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(in this);

    public struct Enumerator : IEnumerator<int>
    {
        private readonly FieldPath _fieldPath;
        private int _index;
        internal Enumerator(in FieldPath fieldPath)
        {
            _index = -1;
            _fieldPath = fieldPath;
        }

        public int Current => _fieldPath[_index];

        object IEnumerator.Current => _fieldPath[_index];

        public void Dispose() { _index = _fieldPath.Count; }

        public bool MoveNext()
        {
            _index++;
            return _index < _fieldPath.Count;
        }

        public void Reset() => _index = -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowPathFull() =>
        throw new InvalidOperationException("FieldPath is full");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidPop(int count) =>
        throw new InvalidOperationException($"Cannot pop {count} items from a path of length {_size}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private readonly void ThrowIndexOutOfRange(int index) =>
        throw new ArgumentOutOfRangeException(nameof(index), $"Cannot access item at index {index}, must be < {_size}");
}
