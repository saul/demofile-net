using System.Diagnostics;
using System.Text;

namespace DemoFile.Test;

[DebuggerDisplay("Count = {Count}")]
public class DemoSnapshot
{
    private readonly Dictionary<DemoTick, List<string>> _items = new();

    public int Count => _items.Count;

    public void Add(DemoTick tick, string details)
    {
        if (!_items.TryGetValue(tick, out var tickItems))
        {
            _items[tick] = new List<string> {details};
        }
        else if (!tickItems.Contains(details))
        {
            tickItems.Add(details);
        }
    }

    public void MergeFrom(DemoSnapshot other)
    {
        foreach (var (tick, items) in other._items)
        {
            foreach (var item in items)
            {
                Add(tick, item);
            }
        }
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        foreach (var (tick, items) in _items.OrderBy(kvp => kvp.Key))
        {
            foreach (var item in items)
            {
                result.Append($"[{tick}] {item}");

                if (item[^1] != '\n')
                {
                    result.AppendLine();
                }
            }
        }

        return result.ToString();
    }
}
