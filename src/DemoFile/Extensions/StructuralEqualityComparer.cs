namespace DemoFile.Extensions;

internal class StructuralEqualityComparer<T> : IEqualityComparer<IReadOnlyList<T>>
{
    public static readonly StructuralEqualityComparer<T> Instance = new();

    public bool Equals(IReadOnlyList<T>? left, IReadOnlyList<T>? right)
    {
        if (left == null && right == null)
            return true;

        if (left == null || right == null)
            return false;

        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!Equals(left[i], right[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode(IReadOnlyList<T> collection)
    {
        var hashCode = new HashCode();
        hashCode.Add(collection.Count);

        foreach (var value in collection)
        {
            hashCode.Add(value);
        }

        return hashCode.ToHashCode();
    }
}