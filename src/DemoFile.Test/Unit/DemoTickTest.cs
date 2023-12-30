namespace DemoFile.Test.Unit;

[TestFixture]
public class DemoTickTest
{
    private static readonly TestCaseData[] ComparisonCases =
    {
        new(DemoTick.PreRecord, DemoTick.Zero),
        new(default(DemoTick), new DemoTick(1)),
        new(new DemoTick(0), new DemoTick(1)),
        new(new DemoTick(1), new DemoTick(2))
    };

    [TestCaseSource(nameof(ComparisonCases))]
    public void Comparison_LessThanGreaterThan_True(DemoTick left, DemoTick right)
    {
        Assert.That(left < right);
        Assert.That(left <= right);

        Assert.That(left > right, Is.False);
        Assert.That(left >= right, Is.False);

        Assert.That(left, Is.LessThan(right));
        Assert.That(left.CompareTo(right), Is.EqualTo(-1));

        Assert.That(left != right);
        Assert.That(left == right, Is.False);

        Assert.That(right > left);
        Assert.That(right, Is.GreaterThan(left));
        Assert.That(right.CompareTo(left), Is.EqualTo(1));
    }

    private static readonly TestCaseData[] EqualityCases =
    {
        new(DemoTick.PreRecord, DemoTick.PreRecord),
        new(DemoTick.Zero, new DemoTick(0)),
        new(default(DemoTick), new DemoTick(0)),
        new(new DemoTick(1), new DemoTick(1)),
        new(new DemoTick(2), new DemoTick(2))
    };

    [TestCaseSource(nameof(EqualityCases))]
    public void Equality_True(DemoTick left, DemoTick right)
    {
        Assert.That(left == right);
        Assert.That(left != right, Is.False);
        Assert.That(left.CompareTo(right), Is.EqualTo(0));

        Assert.That(right == left);
        Assert.That(right != left, Is.False);
        Assert.That(right.CompareTo(left), Is.EqualTo(0));
    }
}