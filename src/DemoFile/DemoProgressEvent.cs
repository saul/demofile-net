namespace DemoFile;

/// <summary>
/// Fired after every command is read during <see cref="DemoFileReader{TGameParser}.ReadAllAsync()"/>.
/// </summary>
/// <param name="ProgressRatio">
/// A value between [0..1] indicating progress through the demo file.
/// If the demo was recorded incompletely, this will always be 0,
/// as the size of the demo is unknown.
/// </param>
public readonly record struct DemoProgressEvent(float ProgressRatio);
