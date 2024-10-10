using Concentus;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DemoFile.Example.VoiceExtraction
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

            var demo = new CsDemoParser();
            var demoFileReader = new DemoFileReader<CsDemoParser>(demo, new MemoryStream(File.ReadAllBytes(path)));

            var stopwatch = Stopwatch.StartNew();

            Dictionary<ulong, List<CMsgVoiceAudio>> voiceDataPerSteamId = new();

            demo.PacketEvents.SvcVoiceData += e =>
            {
                if (e.Audio == null)
                    return;

                // after 6th Feb 2024, CS uses Opus format, before that it was Steam format
                if (e.Audio.Format != VoiceDataFormat_t.VoicedataFormatOpus)
                    throw new ArgumentException($"Invalid voice format: {e.Audio.Format}");

                voiceDataPerSteamId.TryGetValue(e.Xuid, out var voiceData);
                voiceData ??= new();
                voiceData.Add(e.Audio);
                voiceDataPerSteamId[e.Xuid] = voiceData;
            };

            await demoFileReader.ReadAllAsync(CancellationToken.None);

            Console.WriteLine($"Extracting voice to directory: {Directory.GetCurrentDirectory()}\n");

            Console.WriteLine($"Total players with voice {voiceDataPerSteamId.Count}, " +
                $"total messages {voiceDataPerSteamId.Sum(_ => _.Value.Count)}, " +
                $"total compressed size {voiceDataPerSteamId.Sum(_ => _.Value.Sum(a => a.VoiceData.Length)) / 1024} KB\n");

            const int k_sampleRate = 48000;
            const int k_numChannels = 1;
            long totalSizeExtracted = 0;

            OpusCodecFactory.AttemptToUseNativeLibrary = false; // make sure that Managed-only code works
            using var decoder = OpusCodecFactory.CreateDecoder(k_sampleRate, k_numChannels);

            foreach (var item in voiceDataPerSteamId)
            {
                ulong steamId = item.Key;
                var player = demo.GetPlayerBySteamId(steamId);

                List<CMsgVoiceAudio> audioMessages = item.Value;
                int compressedSize = audioMessages.Sum(_ => _.VoiceData.Length);
                float[] pcmSamples = new float[compressedSize * 64]; // should be enough
                int numDecodedSamples = 0;
                foreach (var audioMessage in audioMessages)
                {
                    if (audioMessage.VoiceData.Length == 0) // necessary to check this, because otherwise Decoder can allocate big array for no reason
                        continue;
                    int numSamplesDecodedInMessage = decoder.Decode(audioMessage.VoiceData.Span, pcmSamples.AsSpan(numDecodedSamples), pcmSamples.Length - numDecodedSamples);
                    numDecodedSamples += numSamplesDecodedInMessage;
                }

                WriteWavFile($"{steamId}_demo_voice.wav", k_sampleRate, k_numChannels, pcmSamples.AsSpan(0, numDecodedSamples));

                int sizeExtracted = numDecodedSamples * 4;
                totalSizeExtracted += sizeExtracted;

                Console.WriteLine($"{player?.PlayerName ?? steamId.ToString()}:  {audioMessages.Count} messages,  {compressedSize / 1024} KB compressed,  {sizeExtracted / 1024} KB decompressed");
            }

            Console.WriteLine($"\nTotal voice extracted: {totalSizeExtracted / 1024.0:F} KB\n");

            Console.WriteLine($"Finished! elapsed {stopwatch.Elapsed.TotalSeconds:F} sec, ticks {demo.CurrentDemoTick.Value}");
        }

        static void WriteWavFile(string filePath, int sampleRate, int numChannels, ReadOnlySpan<float> samplesFloat32)
        {
            int numSamples = samplesFloat32.Length;
            int sampleSize = sizeof(int);

            int[] samplesInt32 = new int[numSamples];
            const int conversionScale = int.MaxValue - 1;
            for (int i = 0; i < numSamples; i++)
                samplesInt32[i] = (int)(samplesFloat32[i] * conversionScale);

            WriteWavFile(filePath, numSamples, sampleRate, numChannels, sampleSize, MemoryMarshal.AsBytes(samplesInt32.AsSpan()));
        }

        static void WriteWavFile(
            string filePath, int numSamples, int sampleRate, int numChannels, int sampleSize, ReadOnlySpan<byte> audioData)
        {
            var stream = new MemoryStream(44 + numSamples * sampleSize * numChannels);
            var wr = new BinaryWriter(stream);

            wr.Write(Encoding.ASCII.GetBytes("RIFF"));
            wr.Write(36 + numSamples * numChannels * sampleSize);
            wr.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            wr.Write((int)16);
            wr.Write((short)1); // Encoding
            wr.Write((short)numChannels); // Channels
            wr.Write((int)(sampleRate)); // Sample rate
            wr.Write((int)(sampleRate * sampleSize * numChannels)); // Average bytes per second
            wr.Write((short)(sampleSize * numChannels)); // block align
            wr.Write((short)(8 * sampleSize)); // bits per sample
            wr.Write(Encoding.ASCII.GetBytes("data")); // data chunk id
            wr.Write((int)(numSamples * sampleSize * numChannels)); // data size

            if (stream.Position != 44)
                throw new UnreachableException("Header not filled correctly");

            stream.Write(audioData);

            File.WriteAllBytes(filePath, stream.GetBuffer());
        }
    }
}
