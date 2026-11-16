namespace DemoFile.Test.Integration;

/// <summary>
/// Entity deletions that aren't part of the entity records in <see cref="CSVCMsg_PacketEntities"/>
/// (engine2 ReadPacketEntities, see ENGINE_PARITY.md §7.2). Expected values come from a replica of the
/// engine's reader, which matches the parser's entity set after every packet on these demos.
/// </summary>
[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class EntityDeletionIntegrationTest
{
    private static readonly byte[] Pov14030 = File.ReadAllBytes(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "pov_test.dem"));

    private readonly ParseMode _mode;

    public EntityDeletionIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    private static DemoSnapshot RecordWeaponDeletions(CsDemoParser demo)
    {
        var snapshot = new DemoSnapshot();
        demo.EntityEvents.CBasePlayerWeapon.Delete += weapon =>
            snapshot.Add(demo.CurrentDemoTick, $"Deleted {weapon.ServerClass.Name} #{weapon.EntityIndex.Value}");
        return snapshot;
    }

    [Test]
    public async Task TrailingDeletionsList()
    {
        // Older demos: delta packets end with a list of deleted entity indices.
        // These two weapons are only ever deleted via that list.
        var result = await Parse(_mode, GotvProtocol14005, RecordWeaponDeletions);

        Assert.That(result, Does.Contain("Deleted CWeaponHKP2000 #249"));
        Assert.That(result, Does.Contain("Deleted CWeaponAWP #262"));
    }

    [Test]
    public async Task NonTransmittedReconcile()
    {
        // Newer POV demos: entities that leave the PVS and aren't listed as non-transmitted are deleted.
        var result = await Parse(_mode, Pov14030, RecordWeaponDeletions);

        Assert.That(result, Does.Contain("Deleted CWeaponHKP2000 #196"));
        Assert.That(result, Does.Contain("Deleted CWeaponAWP #235"));
    }
}

[TestFixture]
public class EntityDeletionLiveCountTest
{
    private static async Task<int> LiveEntitiesAtEnd(string demoFileName)
    {
        var demo = new CsDemoParser();
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", demoFileName));
        await DemoFileReader.Create(demo, new MemoryStream(bytes)).ReadAllAsync(default);
        return demo.Entities.Count();
    }

    // Live entity counts at the end of the demo according to the engine's semantics.
    // Before handling the deletion mechanisms we kept 2 / 32 extra ghost entities respectively.
    [TestCase("14005.dem", 211)]
    [TestCase("pov_test.dem", 746)]
    public async Task MatchesEngine(string demoFileName, int expected)
    {
        Assert.That(await LiveEntitiesAtEnd(demoFileName), Is.EqualTo(expected));
    }
}
