namespace DemoFile.Sdk;

public partial class CCSPlayerController
{
    public override string ToString() => $"{(IsActive ? "" : "[INACTIVE] ")}{PlayerName} ({Connected})";
}