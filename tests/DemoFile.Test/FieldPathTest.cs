namespace DemoFile.Test;

[TestFixture]
public class FieldPathTest
{
    [Test]
    public void Empty()
    {
        var fieldPath = new FieldPath();
        
        Assert.That(fieldPath.Count, Is.EqualTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var _ = fieldPath[0];
        });
        Assert.That(fieldPath.ToString(), Is.EqualTo("(empty)"));
    }
    
    [Test]
    public void Add_1()
    {
        var fieldPath = new FieldPath {123};

        Assert.That(fieldPath.Count, Is.EqualTo(1));
        Assert.That(fieldPath[0], Is.EqualTo(123));
        Assert.That(fieldPath.ToString(), Is.EqualTo("/123"));
    }
    
    [Test]
    public void Add_7()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        Assert.That(fieldPath.Count, Is.EqualTo(7));
        Assert.That(fieldPath[0], Is.EqualTo(123));
        Assert.That(fieldPath[1], Is.EqualTo(234));
        Assert.That(fieldPath[2], Is.EqualTo(345));
        Assert.That(fieldPath[3], Is.EqualTo(456));
        Assert.That(fieldPath[4], Is.EqualTo(567));
        Assert.That(fieldPath[5], Is.EqualTo(678));
        Assert.That(fieldPath[6], Is.EqualTo(789));
        Assert.That(fieldPath.ToString(), Is.EqualTo("/123/234/345/456/567/678/789"));
    }
    
    [Test]
    public void Add_7_ThenSetAll()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        fieldPath[1] += 2;
        fieldPath[4] = 101010;
        fieldPath[2] = 5566;
        fieldPath[5] = 942;
        fieldPath[6] = 1305;
        fieldPath[0] -= 100;
        fieldPath[3] = 2605;

        Assert.That(fieldPath.Count, Is.EqualTo(7));
        Assert.That(fieldPath[0], Is.EqualTo(23));
        Assert.That(fieldPath[1], Is.EqualTo(236));
        Assert.That(fieldPath[2], Is.EqualTo(5566));
        Assert.That(fieldPath[3], Is.EqualTo(2605));
        Assert.That(fieldPath[4], Is.EqualTo(101010));
        Assert.That(fieldPath[5], Is.EqualTo(942));
        Assert.That(fieldPath[6], Is.EqualTo(1305));
    }
    
    [Test]
    public void Add_7_ThenSet()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        fieldPath[1] += 2;
        fieldPath[4] = 101010;
        fieldPath.Pop(2);

        Assert.That(fieldPath.Count, Is.EqualTo(5));
        Assert.That(fieldPath[0], Is.EqualTo(123));
        Assert.That(fieldPath[1], Is.EqualTo(236));
        Assert.That(fieldPath[2], Is.EqualTo(345));
        Assert.That(fieldPath[3], Is.EqualTo(456));
        Assert.That(fieldPath[4], Is.EqualTo(101010));
        Assert.That(fieldPath.ToString(), Is.EqualTo("/123/236/345/456/101010"));
    }
    
    [Test]
    public void Mutate()
    {
        var fieldPath = new FieldPath
        {
            123,
            234
        };

        fieldPath[^2] += 5;
        fieldPath[^1] += 2;

        Assert.That(fieldPath.Count, Is.EqualTo(2));
        Assert.That(fieldPath[0], Is.EqualTo(128));
        Assert.That(fieldPath[1], Is.EqualTo(236));
    }
    
    [Test]
    public void CannotMutateDefault()
    {
        var fieldPath = FieldPath.Default;
        fieldPath.Add(5);

        Assert.That(FieldPath.Default.Count, Is.EqualTo(1));
        Assert.That(FieldPath.Default[0], Is.EqualTo(-1));
    }
    
    [Test]
    public void Add_ThenPopAllButOne()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        fieldPath.Pop(6);
        fieldPath.Add(1706);
        
        Assert.That(fieldPath.Count, Is.EqualTo(2));
        Assert.That(fieldPath[0], Is.EqualTo(123));
        Assert.That(fieldPath[1], Is.EqualTo(1706));
        Assert.That(fieldPath.ToString(), Is.EqualTo("/123/1706"));
    }
    
    [Test]
    public void Add_ThenPopAll()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        fieldPath.Pop(7);
        
        Assert.That(fieldPath.Count, Is.EqualTo(0));
        Assert.That(fieldPath.ToString(), Is.EqualTo("(empty)"));
    }
    
    [Test]
    public void Enumerable_3()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345
        };

        var fieldPathItems = fieldPath.ToArray();
        Assert.That(fieldPathItems, Has.Length.EqualTo(3));
        Assert.That(fieldPathItems[0], Is.EqualTo(123));
        Assert.That(fieldPathItems[1], Is.EqualTo(234));
        Assert.That(fieldPathItems[2], Is.EqualTo(345));
    }
    
    [Test]
    public void Enumerable_7()
    {
        var fieldPath = new FieldPath
        {
            123,
            234,
            345,
            456,
            567,
            678,
            789
        };

        var fieldPathItems = fieldPath.ToArray();
        Assert.That(fieldPathItems, Has.Length.EqualTo(7));
        Assert.That(fieldPathItems[0], Is.EqualTo(123));
        Assert.That(fieldPathItems[1], Is.EqualTo(234));
        Assert.That(fieldPathItems[2], Is.EqualTo(345));
        Assert.That(fieldPathItems[3], Is.EqualTo(456));
        Assert.That(fieldPathItems[4], Is.EqualTo(567));
        Assert.That(fieldPathItems[5], Is.EqualTo(678));
        Assert.That(fieldPathItems[6], Is.EqualTo(789));
    }
}