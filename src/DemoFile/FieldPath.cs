using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DemoFile;

/// <summary>
/// A list of ints representing a path through nested fields on server classes.
/// </summary>
internal struct FieldPath : IReadOnlyList<int>
{
    public static readonly FieldPath Default = new() {-1};
    
    private int _path0;
    private int _path1;
    private int _path2;
    private int _path3;
    private int _path4;
    private int _path5;
    private int _path6;
    private int _size;

    public void Add(int item)
    {
        switch (_size)
        {
            case 0: _path0 = item; break;
            case 1: _path1 = item; break;
            case 2: _path2 = item; break;
            case 3: _path3 = item; break;
            case 4: _path4 = item; break;
            case 5: _path5 = item; break;
            case 6: _path6 = item; break;
            default: throw new InvalidOperationException("FieldPath is full");
        }

        _size += 1;
    }

    public void Pop(int count)
    {
        if (count > _size)
        {
            throw new InvalidOperationException($"Cannot pop {count} items from a path of length {_size}");
        }

        _size -= count;
    }

    public int this[int index]
    {
        readonly get => index >= 0 && index < _size
            ? index switch
            {
                0 => _path0,
                1 => _path1,
                2 => _path2,
                3 => _path3,
                4 => _path4,
                5 => _path5,
                6 => _path6,
                _ => throw new UnreachableException()
            }
            : throw new ArgumentOutOfRangeException(nameof(index), $"Cannot get item at index {index}, must be < {_size}");
        set
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Cannot set item at index {index}, must be < {_size}");
            
            switch (index)
            {
                case 0: _path0 = value; break;
                case 1: _path1 = value; break;
                case 2: _path2 = value; break;
                case 3: _path3 = value; break;
                case 4: _path4 = value; break;
                case 5: _path5 = value; break;
                case 6: _path6 = value; break;
                default: throw new UnreachableException();
            };
        }
    }

    public readonly int Count => _size;
    
    public override string ToString() => _size == 0
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

    public ReadOnlySpan<int> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref _path0, _size);
}
