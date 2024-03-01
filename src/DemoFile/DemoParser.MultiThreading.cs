using System.Text;

namespace DemoFile
{
    /// <summary>
    /// Result of multi-threaded parsing.
    /// </summary>
    public class MultiThreadedDemoParsingResult
    {
        /// <summary>
        /// All section that were parsed. Every section is parsed on a different thread.
        /// </summary>
        public List<MultiThreadedDemoParserSection> Sections { get; } = new();

        public MultiThreadedDemoParsingResult(params MultiThreadedDemoParserSection[] sections)
        {
            Sections.AddRange(sections);
        }
    }

    public class MultiThreadedDemoParserSection
    {
        /// <summary>
        /// Parser that is parsing this section.
        /// </summary>
        public DemoParser DemoParser { get; }

        /// <summary>
        /// StringBuilder that you can use to output data. Provided for convenience.
        /// </summary>
        public StringBuilder StringBuilder { get; set; } = new StringBuilder();

        /// <summary>
        /// Optional user data stored for this section. This should be used when outputing data in a custom format.
        /// </summary>
        public object? UserData { get; set; }

        public MultiThreadedDemoParserSection(DemoParser demoParser)
        {
            DemoParser = demoParser;
        }
    }

    public partial class DemoParser
    {
        /// <summary>
        /// Read entire demo in a multi-threaded way. Demo is split-up into multiple sections, and each section is parsed
        /// on a different thread. When parsing is finished, you can access parsed sections and their data, without the need
        /// for sorting. Note that <paramref name="setupAction"/> will be called on a background thread, so your callbacks
        /// need to be thread-safe, but they can freely access the data of their own section.
        /// </summary>
        public static async ValueTask<MultiThreadedDemoParsingResult> ReadAllMultiThreadedAsync(
            Action<MultiThreadedDemoParserSection>? setupAction, MemoryStream stream, CancellationToken cancellationToken)
        {
            if (!stream.TryGetBuffer(out var arraySegment))
            {
                throw new ArgumentException("Can't get MemoryStream's internal buffer - this is a waste of performance. " +
                    "Make sure that you create MemoryStream with exposed buffer");
            }

            var parser = new DemoParser();

            await parser.StartReadingAsync(stream, cancellationToken).ConfigureAwait(false);

            // currently, no way to find snapshot positions in incomplete demos
            // => fallback to single-threaded parsing
            if (parser.TickCount.Value <= 0)
            {
                stream.Position = 0;
                parser = new DemoParser();
                var section = new MultiThreadedDemoParserSection(parser);
                setupAction?.Invoke(section);
                await parser.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
                return new MultiThreadedDemoParsingResult(section);
            }

            int numParsers = Environment.ProcessorCount;
            int numSections = (int)Math.Ceiling(parser.TickCount.Value / (double)FullPacketInterval);
            int numSectionsPerParser = (int)Math.Ceiling(numSections / (double)numParsers);
            
            // seek to end of demo to find all snapshot positions
            await parser.SeekToTickAsync(parser.TickCount, cancellationToken).ConfigureAwait(false);

            var tasks = new List<Task>();
            var sections = new List<MultiThreadedDemoParserSection>();

            for (int p = 0; p < numParsers; p++)
            {
                int startFullPacketPositionIndex = p * numSectionsPerParser;
                int endFullPacketPositionIndex = startFullPacketPositionIndex + numSectionsPerParser;

                bool reachedLastTick = endFullPacketPositionIndex >= parser._fullPacketPositions.Count;

                DemoTick startTick = parser._fullPacketPositions[startFullPacketPositionIndex].Tick;
                DemoTick endTick = reachedLastTick
                    ? parser.TickCount
                    : parser._fullPacketPositions[endFullPacketPositionIndex].Tick;

                var backgroundParser = new DemoParser();
                var backgroundParserStream = new MemoryStream(arraySegment.Array!);
                var section = new MultiThreadedDemoParserSection(backgroundParser);

                var taskFunc = () => backgroundParser.ParseSectionInBackgroundAsync(
                    section,
                    backgroundParserStream,
                    startTick,
                    endTick,
                    parser._fullPacketPositions,
                    setupAction,
                    cancellationToken);

                tasks.Add(Task.Run(taskFunc, cancellationToken));

                sections.Add(section);

                if (reachedLastTick)
                    break;
            }

            // wait for all parsers to finish

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return new MultiThreadedDemoParsingResult(sections.ToArray());
        }

        private async Task ParseSectionInBackgroundAsync(
            MultiThreadedDemoParserSection section,
            MemoryStream backgroundParserStream,
            DemoTick startTick,
            DemoTick endTick,
            IReadOnlyList<FullPacketRecord> fullPacketPositions,
            Action<MultiThreadedDemoParserSection>? setupAction,
            CancellationToken cancellationToken)
        {
            var backgroundParser = this;

            await backgroundParser.StartReadingAsync(backgroundParserStream, cancellationToken).ConfigureAwait(false);

            backgroundParser._fullPacketPositions.Clear();
            backgroundParser._fullPacketPositions.AddRange(fullPacketPositions);

            if (startTick != DemoTick.Zero)
                await backgroundParser.SeekToTickAsync(startTick, cancellationToken).ConfigureAwait(false);

            // only AFTER we seek to starting tick, allow the user to setup callbacks
            setupAction?.Invoke(section);

            while (true)
            {
                // peek
                (DemoTick nextTick, EDemoCommands command) = backgroundParser.PeekNext();

                if (nextTick >= endTick) // parser completed his section
                    break;

                bool hasNext = await backgroundParser.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                if (!hasNext) // end of file
                    break;
            }
        }

        /// <summary>
        /// Peek next command. Parser's state will not be modified and callbacks will not be invoked.
        /// </summary>
        public (DemoTick nextTick, EDemoCommands command) PeekNext()
        {
            var streamPositionBefore = _stream.Position;
            var commandStartPositionBefore = _commandStartPosition;
            var demoTickBefore = CurrentDemoTick;

            try
            {
                var cmd = ReadCommandHeader();
                var nextTick = CurrentDemoTick;
                return (nextTick, cmd.Command);
            }
            finally
            {
                _commandStartPosition = commandStartPositionBefore;
                CurrentDemoTick = demoTickBefore;
                _stream.Position = streamPositionBefore;
            }
        }
    }
}
