using System.Diagnostics;

namespace DemoFile;

public partial class DemoParser
{
    /// <summary>
    /// Parse the entire demo in <paramref name="demoFileBytes"/> from start to end.
    /// The demo is divided into sections, with each section parsed in parallel.
    /// <paramref name="setupSection"/> is called for each section, and will be
    /// called concurrently.
    /// </summary>
    /// <param name="demoFileBytes">The contents of the demo file.</param>
    /// <param name="setupSection">Function to attach callbacks for each section.</param>
    /// <param name="cancellationToken">Cancellation token to interrupt parsing.</param>
    public static Task ReadAllParallelAsync(
        byte[] demoFileBytes,
        Action<DemoParser> setupSection,
        CancellationToken cancellationToken)
    {
        return ReadAllParallelAsync(demoFileBytes, demo =>
        {
            setupSection(demo);
            return 0;
        }, cancellationToken);
    }

    /// <summary>
    /// Parse the entire demo in <paramref name="demoFileBytes"/> from start to end.
    /// The demo is divided into sections, with each section parsed in parallel.
    /// <paramref name="setupSection"/> is called for each section, and the results
    /// are concatenated together to create the return value.
    /// </summary>
    /// <param name="demoFileBytes">The contents of the demo file.</param>
    /// <param name="setupSection">
    /// Function to attach callbacks for each section, and to build a result.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to interrupt parsing.</param>
    /// <typeparam name="TResult">Caller defined per-section result.</typeparam>
    /// <returns>Concatenated list of all return values of <paramref name="setupSection"/>.</returns>
    public static async Task<IReadOnlyList<TResult>> ReadAllParallelAsync<TResult>(
        byte[] demoFileBytes,
        Func<DemoParser, TResult> setupSection,
        CancellationToken cancellationToken)
    {
        var demo = new DemoParser();
        var stream = new MemoryStream(demoFileBytes);

        // Read all CDemoFullPackets
        TResult initialResult;
        using (new SeekScope(demo))
        {
            // Only section that is attached before `StartReadingAsync`, enabling
            // callbacks on e.g. DemoEvents.DemoFileInfo
            initialResult = setupSection(demo);
            await demo.StartReadingAsync(stream, cancellationToken).ConfigureAwait(false);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cmd = demo.ReadCommandHeader();

                if (cmd.Command == EDemoCommands.DemFullPacket)
                {
                    await demo.MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (cmd.Command == EDemoCommands.DemStop)
                {
                    break;
                }
                else
                {
                    // Skip over the data and start reading the next command
                    stream.Position += cmd.Size;
                }
            }
        }

        var maxParallelism = Environment.ProcessorCount;
        var numSections = demo.FullPackets.Count;
        var numSectionsPerParser = Math.Max(1, (numSections + maxParallelism - 1) / maxParallelism);
        var numParsers = (numSections + numSectionsPerParser - 1) / numSectionsPerParser;

        var tasks = new Task<TResult>[numParsers + 1];
        tasks[0] = Task.FromResult(initialResult);

        for (var parserIdx = 0; parserIdx < numParsers; parserIdx++)
        {
            var startFullPacketIdx = parserIdx * numSectionsPerParser;
            var endFullPacketIdx = startFullPacketIdx + numSectionsPerParser;

            var fullPacket = demo.FullPackets[startFullPacketIdx];
            var endPosition = endFullPacketIdx < demo.FullPackets.Count
                ? demo.FullPackets[endFullPacketIdx].StreamPosition
                : demoFileBytes.Length;

            var backgroundParser = new DemoParser();

            tasks[parserIdx + 1] = Task.Run(() => backgroundParser.ParseRangeAsync(demoFileBytes, fullPacket, endPosition, setupSection, cancellationToken));
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<TResult> ParseRangeAsync<TResult>(
        byte[] demoFileBytes,
        FullPacketRecord fullPacket,
        long endPosition,
        Func<DemoParser, TResult> setupAction,
        CancellationToken cancellationToken)
    {
        await StartReadingAsync(new MemoryStream(demoFileBytes), cancellationToken).ConfigureAwait(false);

        (CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
        RestoreStringTables(stringTables);
        _readFullPacketTick = CurrentDemoTick;

        var cmd = ReadCommandHeader();
        Debug.Assert(cmd.Command == EDemoCommands.DemFullPacket);
        await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false);

        // Caller sets up actions after the demo is restored to the full packet state
        var result = setupAction(this);

        while (_stream.Position < endPosition)
        {
            cmd = ReadCommandHeader();

            if (!await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                break;
            }
        }

        return result;
    }
}
