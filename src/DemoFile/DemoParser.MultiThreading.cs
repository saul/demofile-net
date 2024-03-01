using System.Diagnostics;
using System.Text;

namespace DemoFile
{
    public class MultiThreadedDemoParsingResult
    {
        public List<MultiThreadedDemoParserSection> Sections = new();

        internal MultiThreadedDemoParsingResult(params MultiThreadedDemoParserSection[] sections)
        {
            Sections.AddRange(sections);
        }
    }

    public class MultiThreadedDemoParserSection
    {
        public DemoParser DemoParser { get; set; }

        public StringBuilder StringBuilder { get; set; } = new StringBuilder();

        public object? UserData { get; set; }

        public MultiThreadedDemoParserSection(DemoParser demoParser)
        {
            DemoParser = demoParser;
        }
    }

    public partial class DemoParser
    {
        public static async ValueTask<MultiThreadedDemoParsingResult> ReadAllMultiThreadedAsync(
            Action<MultiThreadedDemoParserSection>? setupAction, MemoryStream stream, CancellationToken cancellationToken)
        {
            if (!stream.TryGetBuffer(out var arraySegment))
                throw new ArgumentException("Can't get buffer");

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

            int numParsers = 6 * 2;
            int numSections = (int)Math.Ceiling(parser.TickCount.Value / (double)FullPacketInterval);
            int numSectionsPerParser = (int)Math.Ceiling(numSections / (double)numParsers);
            //Console.WriteLine($"num parsers {numParsers}, num sections {numSections}, numSectionsPerParser {numSectionsPerParser}");

            var sw = Stopwatch.StartNew();

            // seek to end of demo to find all snapshot positions
            await parser.SeekToTickAsync(parser.TickCount, cancellationToken).ConfigureAwait(false);

            //Console.WriteLine($"Seek to end in {sw.ElapsedMilliseconds} ms");

            var tasks = new List<Task>();
            var threads = new List<Thread>();
            var sections = new List<MultiThreadedDemoParserSection>();

            for (int p = 0; p < numParsers; p++)
            {
                int startFullPacketPositionIndex = p * numSectionsPerParser;
                int endFullPacketPositionIndex = startFullPacketPositionIndex + numSectionsPerParser;

                //Console.WriteLine($"starting parser {p}, startFullPacketPositionIndex {startFullPacketPositionIndex}, endFullPacketPositionIndex {endFullPacketPositionIndex}");

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

                //var thread = new Thread(() => taskFunc().GetAwaiter().GetResult());
                //thread.Start();
                //threads.Add(thread);

                tasks.Add(Task.Run(taskFunc, cancellationToken));

                sections.Add(section);

                if (reachedLastTick)
                    break;
            }

            // start background parser for every section
            //for (int i = 0; i < parser._fullPacketPositions.Count; i++)
            //{
            //    var fullPacketPosition = parser._fullPacketPositions[i];

            //    var backgroundParser = new DemoParser();
            //    var backgroundParserStream = new MemoryStream(arraySegment.Array!);

            //    DemoTick endTick = i == parser._fullPacketPositions.Count - 1
            //        ? parser.TickCount
            //        : parser._fullPacketPositions[i + 1].Tick;

            //    var taskFunc = () => backgroundParser.ParseSectionInBackgroundAsync(
            //        backgroundParserStream,
            //        fullPacketPosition.Tick,
            //        endTick,
            //        parser._fullPacketPositions,
            //        setupAction,
            //        cancellationToken);

            //    //var thread = new Thread(() => taskFunc().GetAwaiter().GetResult());
            //    //thread.Start();
            //    //threads.Add(thread);

            //    tasks.Add(Task.Run(taskFunc, cancellationToken));
            //}

            // wait for all parsers to finish

            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var thread in threads)
                thread.Join();

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
