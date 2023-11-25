using System.Runtime.CompilerServices;

namespace DemoFile.Test;

public static class Snapshot
{
    private static readonly DirectoryInfo SnapshotDir = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots"));
    private static readonly DirectoryInfo SnapshotSourceDir = FindSnapshotSourceDir();

    private static DirectoryInfo FindSnapshotSourceDir()
    {
        var currentDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

        while (currentDir != null)
        {
            var searchDir = new DirectoryInfo(Path.Combine(currentDir.FullName, "Snapshots"));
            if (searchDir.Exists)
            {
                return searchDir;
            }

            currentDir = currentDir.Parent;
        }

        throw new Exception("Could not find 'Snapshots' source directory");
    }

    public static void Assert(string contents, [CallerMemberName] string? name = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var fileName = name + ".txt";
        var path = Path.Combine(SnapshotDir.FullName, fileName);

        if (File.Exists(path))
        {
            Console.WriteLine($"Comparing snapshot: {path}");
            var expected = File.ReadAllText(path).ReplaceLineEndings();
            NUnit.Framework.Assert.That(contents, Is.EqualTo(expected), "Snapshot mismatch");
            return;
        }

        var outputPath = Path.Combine(SnapshotSourceDir.FullName, fileName);
        Console.WriteLine($"Writing snapshot: {outputPath}");
        File.WriteAllText(outputPath, contents.ReplaceLineEndings());
    }
}
