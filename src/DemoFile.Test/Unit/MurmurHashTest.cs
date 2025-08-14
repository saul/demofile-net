namespace DemoFile.Test.Unit;

[TestFixture]
public class MurmurHashTest
{
    [Test]
    public void MurmurHash64()
    {
        var hash = MurmurHash.MurmurHash64("weapons/models/defuser/defuser.vmdl"u8,
            MurmurHash.ResourceIdSeed);
        Assert.That(hash, Is.EqualTo(3242983900571730547u));
    }
}