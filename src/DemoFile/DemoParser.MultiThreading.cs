namespace DemoFile;

public partial class DemoParser
{
    public static Task ReadAllParallelAsync(
        byte[] demoFileBytes,
        Action<DemoParser> setupSection,
        int maxParallelism,
        CancellationToken cancellationToken)
    {
        return ReadAllParallelAsync(demoFileBytes, demo =>
        {
            setupSection(demo);
            return 0;
        }, maxParallelism, cancellationToken);
    }

    public static async Task<IReadOnlyList<TResult>> ReadAllParallelAsync<TResult>(
        byte[] demoFileBytes,
        Func<DemoParser, TResult> setupSection,
        int maxParallelism,
        CancellationToken cancellationToken)
    {
        var demo = new DemoParser();
        var stream = new MemoryStream(demoFileBytes);

        // Read all CDemoFullPackets
        using (new SeekCookie(demo))
        {
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

        var numSections = demo.FullPackets.Count;
        var numSectionsPerParser = Math.Max(1, (numSections + maxParallelism - 1) / maxParallelism);
        var numParsers = (numSections + numSectionsPerParser - 1) / numSectionsPerParser;

        var tasks = new Task<TResult>[numParsers];
        for (var parserIdx = 0; parserIdx < numParsers; parserIdx++)
        {
            var startFullPacketIdx = parserIdx * numSectionsPerParser;
            var endFullPacketIdx = startFullPacketIdx + numSectionsPerParser;

            var fullPacket = demo.FullPackets[startFullPacketIdx];
            var endTick = endFullPacketIdx < demo.FullPackets.Count
                ? demo.FullPackets[endFullPacketIdx].Tick
                : demo.CurrentDemoTick;

            var backgroundParser = new DemoParser();

            tasks[parserIdx] = Task.Run(() => backgroundParser.ParseRangeAsync(demoFileBytes, fullPacket, endTick, setupSection, cancellationToken));
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<TResult> ParseRangeAsync<TResult>(
        byte[] demoFileBytes,
        FullPacketRecord fullPacket,
        DemoTick endTick,
        Func<DemoParser, TResult> setupAction,
        CancellationToken cancellationToken)
    {
        await StartReadingAsync(new MemoryStream(demoFileBytes), cancellationToken).ConfigureAwait(false);

        (CurrentDemoTick, _stream.Position, var stringTables) = fullPacket;
        RestoreStringTables(stringTables);
        _readFullPacketTick = CurrentDemoTick;

        // only AFTER we seek to starting tick, allow the user to setup callbacks
        var result = setupAction(this);

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
