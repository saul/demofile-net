namespace DemoFile.Test.Integration;

/// <summary>
/// The varint after the class id and serial number in an entity-create header is the entity's spawn group handle
/// (see ENGINE_PARITY.md §10). In 14197.dem (cs_italy) the spawn groups are:
/// 1 = the map, 2 = terrorist team intro, 3 = team select, 4 = skybox, 5 = CT team intro, 6 = end of match,
/// 7/8 = team intro variants.
/// </summary>
[TestFixture]
public class SpawnGroupHandleTest
{
    private static readonly byte[] Gotv14197 = File.ReadAllBytes(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "14197.dem"));

    [Test]
    public async Task EntitiesCarryTheirSpawnGroupHandle()
    {
        var demo = new CsDemoParser();
        var loadedWorlds = new Dictionary<uint, string>();
        demo.PacketEvents.NetSpawnGroupLoad += msg => loadedWorlds[msg.Spawngrouphandle] = msg.Worldname;

        await DemoFileReader.Create(demo, new MemoryStream(Gotv14197)).ReadAllAsync(default);

        bool HasEntityIn(string className, string world) =>
            demo.Entities.Any(e => e.ServerClass.Name == className
                                   && loadedWorlds.GetValueOrDefault(e.SpawnGroupHandle) == world);

        // Not part of any spawn group
        Assert.That(demo.Entities.Where(e => e.ServerClass.Name is "CCSPlayerController" or "CCSTeam" or "CWorld")
            .Select(e => e.SpawnGroupHandle), Is.All.EqualTo(0u));

        // Map entities, skybox and UI prefabs (some classes also appear in the map itself)
        Assert.That(HasEntityIn("CPhysicsPropMultiplayer", "cs_italy"));
        Assert.That(HasEntityIn("CSkyCamera", "maps/prefabs/cs_italy_s2/cs_italy_s2_skybox"));
        Assert.That(HasEntityIn("CCSGO_TeamSelectTerroristPosition", "prefabs/misc/team_select"));
        Assert.That(HasEntityIn("CCSGO_EndOfMatchLineupStart", "prefabs/misc/end_of_match"));

        // Every entity's handle is either 0 or a loaded spawn group
        Assert.That(demo.Entities.Select(e => e.SpawnGroupHandle).Distinct(),
            Is.All.Matches<uint>(h => h == 0 || loadedWorlds.ContainsKey(h)));
    }
}
