namespace DemoFile.Test;

public class FieldTypeTest
{
    public static readonly TestCaseData[] ParseCases = new TestCaseData[]
    {
        new("char", new FieldType("char", null, false, 0)),
        new("Outer< Inner* >[12]", new FieldType("Outer", new FieldType("Inner", null, true, 0), false, 12)),
        new("Outer< Inner*[4] >[12]", new FieldType("Outer", new FieldType("Inner", null, true, 4), false, 12)),
        new("Outer< Inner[1] >[12]", new FieldType("Outer", new FieldType("Inner", null, false, 1), false, 12)),
        new("Out3< Out2< Out1< char > > >",
            new FieldType("Out3",
                new FieldType("Out2", new FieldType("Out1", new FieldType("char", null, false, 0), false, 0), false, 0),
                false, 0)
        )
    };

    [TestCaseSource(nameof(ParseCases))]
    public void Parse(string typeName, object expected)
    {
        var fieldType = FieldType.Parse(typeName);
        Assert.That(fieldType, Is.EqualTo(expected));
        Assert.That(fieldType.ToString(), Is.EqualTo(typeName));
    }
}
