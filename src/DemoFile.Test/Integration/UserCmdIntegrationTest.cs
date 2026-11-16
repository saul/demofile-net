using DemoFile.Game.Cs;

namespace DemoFile.Test.Integration;

/// <summary>
/// User commands (<c>svc_UserCmds</c>) are delta-encoded from CS2 patch 14197.
/// </summary>
[TestFixtureSource(typeof(GlobalUtil), nameof(ParseModes))]
public class UserCmdIntegrationTest
{
    private static readonly byte[] GotvProtocol14197 = File.ReadAllBytes(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "14197.dem"));

    private readonly ParseMode _mode;

    public UserCmdIntegrationTest(ParseMode mode)
    {
        _mode = mode;
    }

    [Test]
    public async Task DeltaEncodedCommands_Decode()
    {
        var commands = 0;
        var deltas = 0;
        var incomplete = 0;
        var problems = new List<string>();

        await Parse(_mode, GotvProtocol14197, demo =>
        {
            demo.UserCmdEvents.UserCmd += cmd =>
            {
                Interlocked.Increment(ref commands);
                if (cmd.IsDelta) Interlocked.Increment(ref deltas);
                if (!cmd.IsComplete) Interlocked.Increment(ref incomplete);

                var b = cmd.Command.Base;
                lock (problems)
                {
                    // The decoded command must agree with the unencoded fields of the outer message
                    if (b is { HasClientTick: true } && b.ClientTick != (int)cmd.ClientTick.Value)
                        problems.Add($"cmd {cmd.CommandNumber}: client tick {b.ClientTick} != {cmd.ClientTick.Value}");
                    if (cmd.IsComplete && b?.Viewangles == null)
                        problems.Add($"cmd {cmd.CommandNumber}: complete command without view angles");
                }
            };
            return new DemoSnapshot();
        });

        Assert.That(problems, Is.Empty);
        Assert.That(commands, Is.GreaterThan(0));

        if (_mode != ParseMode.ReadAllParallel)
        {
            // Read sequentially from the start: the first command is sent in full, the rest are deltas
            Assert.That(commands, Is.EqualTo(353));
            Assert.That(deltas, Is.EqualTo(352));
            Assert.That(incomplete, Is.Zero);
        }
    }

    [Test]
    public async Task FullCommands_BeforeDeltaEncoding()
    {
        // Patches 14030 to 14160 send every command in full in svc_UserCmds
        var stats = await CountCommands(GotvProtocol14090);

        Assert.That(stats.Commands, Is.GreaterThan(0));
        Assert.That(stats.Deltas, Is.Zero);
        Assert.That(stats.Incomplete, Is.Zero);
        Assert.That(stats.WithViewAngles, Is.EqualTo(stats.Commands));
    }

    [Test]
    public async Task PovDemo_DemoUserCmd()
    {
        // POV demos carry the recording player's own commands as DEM_UserCmd
        var pov = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "pov_test.dem"));
        var stats = await CountCommands(pov);

        Assert.That(stats.Commands, Is.GreaterThan(0));
        Assert.That(stats.Deltas, Is.Zero);
        Assert.That(stats.Incomplete, Is.Zero);
        Assert.That(stats.Slots, Is.EquivalentTo(new[] { 0u }));
        if (_mode != ParseMode.ReadAllParallel)
            Assert.That(stats.Commands, Is.EqualTo(2306));
    }

    private record CommandStats(int Commands, int Deltas, int Incomplete, int WithViewAngles, HashSet<uint> Slots);

    private async Task<CommandStats> CountCommands(byte[] demoBytes)
    {
        int commands = 0, deltas = 0, incomplete = 0, withViewAngles = 0;
        var slots = new HashSet<uint>();

        await Parse(_mode, demoBytes, demo =>
        {
            demo.UserCmdEvents.UserCmd += cmd =>
            {
                Interlocked.Increment(ref commands);
                if (cmd.IsDelta) Interlocked.Increment(ref deltas);
                if (!cmd.IsComplete) Interlocked.Increment(ref incomplete);
                if (cmd.Command.Base?.Viewangles != null) Interlocked.Increment(ref withViewAngles);
                lock (slots) slots.Add(cmd.PlayerSlot.Value);
            };
            return new DemoSnapshot();
        });

        return new CommandStats(commands, deltas, incomplete, withViewAngles, slots);
    }
}
