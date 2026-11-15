using DemoFile.Game.Cs;
using Google.Protobuf;

namespace DemoFile.Test.Integration;

/// <summary>
/// <c>CSVCMsg_PacketEntities.outofpvs_entity_updates</c> (engine2 ReadPacketEntities, see ENGINE_PARITY.md §7.3).
/// No local demo contains these yet (they need a POV recording from a server with <c>sv_outofpvsentityupdates</c>),
/// so these tests parse a real demo up to a point and then inject a synthetic packet.
/// </summary>
[TestFixture]
public class OutOfPvsEntityUpdatesTest
{
    private const int FreeEntityIndex = 16000;
    private const uint NewSerial = 77;

    private sealed record Setup(
        CsDemoParser Demo,
        CBasePlayerWeapon ExistingWeapon,
        int ServerClassBits,
        List<string> Events);

    private static async Task<Setup> ParseUntilWeaponsExist()
    {
        var demo = new CsDemoParser();
        var reader = DemoFileReader.Create(demo, new MemoryStream(GotvProtocol14197));
        await reader.StartReadingAsync(default);

        CBasePlayerWeapon? weapon = null;
        while (weapon == null && await reader.MoveNextAsync(default))
        {
            weapon = demo.Entities.OfType<CBasePlayerWeapon>().FirstOrDefault(w => w.IsActive);
        }

        Assert.That(weapon, Is.Not.Null);
        Assert.That(demo.Entities.Any(e => e.EntityIndex.Value == FreeEntityIndex), Is.False);

        var events = new List<string>();
        demo.EntityEvents.CBasePlayerWeapon.Create += e => events.Add($"create #{e.EntityIndex.Value}");
        demo.EntityEvents.CBasePlayerWeapon.PostUpdate += e => events.Add($"update #{e.EntityIndex.Value}");
        demo.EntityEvents.CBasePlayerWeapon.Delete += e => events.Add($"delete #{e.EntityIndex.Value}");

        var serverClassBits = (int)Math.Log2(demo.ServerInfo!.MaxClasses) + 1;
        return new Setup(demo, weapon!, serverClassBits, events);
    }

    /// <summary>
    /// An empty delta packet whose out-of-PVS stream updates <paramref name="existing"/> (no field changes)
    /// and creates a new entity of the same class at <see cref="FreeEntityIndex"/>.
    /// </summary>
    private static CSVCMsg_PacketEntities BuildPacket(Setup setup, IEnumerable<int> nonTransmitted)
    {
        var existing = (int)setup.ExistingWeapon.EntityIndex.Value;
        var classId = (uint)setup.ExistingWeapon.ServerClass.ServerClassId;

        var oopvs = new TestBitWriter()
            // delta on the existing weapon
            .UBitVar((uint)existing).Bit(false).NoFieldChanges()
            // create a new weapon in a free slot
            .UBitVar((uint)(FreeEntityIndex - existing - 1)).Bit(true)
            .Bits(classId, setup.ServerClassBits).Bits(NewSerial, 17).VarInt(0)
            .NoFieldChanges();

        var nonTransmittedWriter = new TestBitWriter();
        var previous = -1;
        var nonTransmittedCount = 0;
        foreach (var index in nonTransmitted.Order())
        {
            nonTransmittedWriter.UBitVar((uint)(index - previous - 1)).Bit(true);
            previous = index;
            nonTransmittedCount++;
        }

        return new CSVCMsg_PacketEntities
        {
            LegacyIsDelta = true,
            DeltaFrom = 1,
            UpdatedEntries = 0,
            EntityData = ByteString.Empty,
            OutofpvsEntityUpdates = new CSVCMsg_PacketEntities.Types.outofpvs_entity_updates_t
            {
                Count = 2,
                Data = ByteString.CopyFrom(oopvs.ToArray()),
            },
            NonTransmittedEntities = new CSVCMsg_PacketEntities.Types.non_transmitted_entities_t
            {
                HeaderCount = nonTransmittedCount,
                Data = ByteString.CopyFrom(nonTransmittedWriter.ToArray()),
            },
        };
    }

    [Test]
    public async Task CreateAndDelta_EntitiesExistOutsidePvs()
    {
        var setup = await ParseUntilWeaponsExist();
        var existingIndex = (int)setup.ExistingWeapon.EntityIndex.Value;

        setup.Demo.PacketEvents.SvcPacketEntities!(BuildPacket(setup, [existingIndex, FreeEntityIndex]));

        var created = setup.Demo.GetEntityByIndex<CBasePlayerWeapon>(new CEntityIndex(FreeEntityIndex));
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.ServerClass, Is.EqualTo(setup.ExistingWeapon.ServerClass));
        Assert.That(created.SerialNumber, Is.EqualTo(NewSerial));
        Assert.That(created.IsActive, Is.False, "out-of-PVS entities aren't in the PVS");

        var existing = setup.Demo.GetEntityByIndex<CBasePlayerWeapon>(new CEntityIndex((uint)existingIndex));
        Assert.That(existing, Is.SameAs(setup.ExistingWeapon));
        Assert.That(existing!.IsActive, Is.False);

        Assert.That(setup.Events, Is.EquivalentTo(new[] { $"create #{FreeEntityIndex}", $"update #{existingIndex}" }));
    }

    [Test]
    public async Task NotNonTransmitted_DeletedByReconcile()
    {
        // As in the engine: an out-of-PVS entity isn't part of the frame, so unless the server lists it
        // as non-transmitted it's deleted at the end of the packet.
        var setup = await ParseUntilWeaponsExist();
        var existingIndex = (int)setup.ExistingWeapon.EntityIndex.Value;

        setup.Demo.PacketEvents.SvcPacketEntities!(BuildPacket(setup, [FreeEntityIndex]));

        Assert.That(setup.Demo.GetEntityByIndex<CBasePlayerWeapon>(new CEntityIndex((uint)existingIndex)), Is.Null);
        Assert.That(setup.Demo.GetEntityByIndex<CBasePlayerWeapon>(new CEntityIndex(FreeEntityIndex)), Is.Not.Null);
        Assert.That(setup.Events, Does.Contain($"delete #{existingIndex}"));
    }
}
