using System.Diagnostics;

namespace DemoFile;

internal readonly record struct NodePriority(int Weight, int Value) : IComparable<NodePriority>
{
    public int CompareTo(NodePriority other) => Weight == other.Weight
        ? other.Value.CompareTo(Value)
        : Weight.CompareTo(other.Weight);
}

internal record HuffmanNode<T>(T? Symbol, int Frequency, HuffmanNode<T>? Left, HuffmanNode<T>? Right)
{
    public override string ToString() => Symbol is { } symbol
        ? $"{symbol} ({Frequency})"
        : $"<node> ({Frequency})";

    public static HuffmanNode<T> Build(IEnumerable<KeyValuePair<T, int>> symbolFreqs)
    {
        var queue = new PriorityQueue<HuffmanNode<T>, NodePriority>(symbolFreqs
            .Select(kvp => KeyValuePair.Create(kvp.Key, Math.Max(1, kvp.Value)))
            .Select((kvp, i) => (new HuffmanNode<T>(kvp.Key, kvp.Value, null, null), new NodePriority(kvp.Value, i))));

        var i = queue.Count;
        while (queue.Count > 1)
        {
            var left = queue.Dequeue();
            var right = queue.Dequeue();
            var parent = new HuffmanNode<T>(default, left.Frequency + right.Frequency, left, right);
            var priority = new NodePriority(left.Frequency + right.Frequency, i++);
            queue.Enqueue(parent, priority);
        }

        return queue.Dequeue();
    }
}
