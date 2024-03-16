namespace DemoFile;

public partial class DemoParser
{
    public static Task ReadAllParallelAsync(
        byte[] demoFileBytes,
        Action<DemoParser> setupSection,
        CancellationToken cancellationToken)
    {
        return ReadAllParallelAsync(
            demoFileBytes,
            demo =>
            {
                setupSection(demo);
                return 0;
            },
            cancellationToken);
    }

    public static async Task<IReadOnlyList<TResult>> ReadAllParallelAsync<TResult>(
        byte[] demoFileBytes,
        Func<DemoParser, TResult> setupSection,
        CancellationToken cancellationToken)
    {
        var demo = new DemoParser();
        var stream = new MemoryStream(demoFileBytes);

        // Setup a section _before_ StartReadingAsync to ensure the consumer
        // has chance to hit events like DemoEvents.DemoFileInfo
        var initialResult = setupSection(demo);

        // Read all CDemoFullPackets
        await demo.StartReadingAsync(stream, cancellationToken).ConfigureAwait(false);

        // hack!!
        demo._readFullPacketTick = new DemoTick(Int32.MaxValue);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cmd = demo.ReadCommandHeader();

            if (cmd.Command == EDemoCommands.DemFullPacket)
            {
                await demo.MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false);
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

        var maxParallelism = Environment.ProcessorCount;
        var numSections = demo._fullPacketPositions.Count;
        var numSectionsPerParser = (numSections + maxParallelism - 1) / maxParallelism;
        var numParsers = (numSections + numSectionsPerParser - 1) / numSectionsPerParser;

        var tasks = new Task<TResult>[numParsers + 1];
        tasks[0] = Task.FromResult(initialResult);

        for (var parserIdx = 0; parserIdx < numParsers; parserIdx++)
        {
            var startFullPacketIdx = parserIdx * numSectionsPerParser;
            var endFullPacketIdx = startFullPacketIdx + numSectionsPerParser;

            var fullPacket = demo._fullPacketPositions[startFullPacketIdx];
            var endTick = endFullPacketIdx < demo._fullPacketPositions.Count
                ? demo._fullPacketPositions[endFullPacketIdx].Tick
                : demo.CurrentDemoTick;

            var backgroundParser = new DemoParser();

            tasks[parserIdx + 1] = Task.Run(() => backgroundParser.ParseRangeAsync(demoFileBytes, fullPacket, endTick, setupSection, cancellationToken));
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<TResult> ParseRangeAsync<TResult>(
        byte[] demoFileBytes,
        FullPacketRecord fullPacket,
        DemoTick endTick,
        Func<DemoParser, TResult> setupSection,
        CancellationToken cancellationToken)
    {
        await StartReadingAsync(new MemoryStream(demoFileBytes), cancellationToken).ConfigureAwait(false);

        // Jump to the full packet
        (CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
        _readFullPacketTick = CurrentDemoTick;
        RestoreStringTables(stringTables);

        // After we seek to starting tick, allow the user to setup callbacks
        var result = setupSection(this);

        // Advance ticks until we get to the target tick
        while (CurrentDemoTick < endTick)
        {
            var cmd = ReadCommandHeader();

            if (CurrentDemoTick == endTick)
            {
                break;
            }

            if (!await MoveNextCoreAsync(cmd.Command, cmd.IsCompressed, cmd.Size, cancellationToken).ConfigureAwait(false))
            {
                throw new EndOfStreamException($"Reached EOF at tick {CurrentDemoTick} while reading [{fullPacket.Tick}..{endTick})");
            }
        }

        return result;
    }
}
