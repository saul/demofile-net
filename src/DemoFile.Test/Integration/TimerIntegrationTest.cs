namespace DemoFile.Test.Integration;

[TestFixture]
public class TimerIntegrationTest
{
    [Test]
    public async Task StartTimer_DemoTick()
    {
        // Arrange
        DemoTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.StartTimer(
            new DemoTick(128),
            () =>
            {
                timerTick1 = demo.CurrentDemoTick;
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new DemoTick(128)));
    }

    [Test]
    public async Task StartTimer_DemoTick_Cancel()
    {
        // Arrange
        DemoTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        var disposable = demo.StartTimer(
            new DemoTick(128),
            () =>
            {
                timerTick1 = demo.CurrentDemoTick;
            });

        demo.StartTimer(
            new DemoTick(127),
            () =>
            {
                disposable.Dispose();
            });

        demo.StartTimer(
            new DemoTick(129),
            () =>
            {
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(demo.CurrentDemoTick, Is.EqualTo(new DemoTick(129)));
        Assert.That(timerTick1, Is.EqualTo(default(DemoTick)));
    }

    [Test]
    public async Task StartTimer_DemoTick_State()
    {
        // Arrange
        DemoTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.StartTimer(
            new DemoTick(128),
            demo,
            demo =>
            {
                timerTick1 = demo.CurrentDemoTick;
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new DemoTick(128)));
    }

    [Test]
    public async Task StartTimer_GameTick()
    {
        // Arrange
        GameTick_t timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.StartTimer(
            new GameTick_t(17100u),
            () =>
            {
                timerTick1 = demo.CurrentGameTick;
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new GameTick_t(17100u)));
    }

    [Test]
    public async Task StartTimer_GameTick_Cancel()
    {
        // Arrange
        GameTick_t timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        var disposable = demo.StartTimer(
            new GameTick_t(17099u),
            () =>
            {
                timerTick1 = demo.CurrentGameTick;
            });

        demo.StartTimer(
            new GameTick_t(17098u),
            () =>
            {
                disposable.Dispose();
            });

        demo.StartTimer(
            new GameTick_t(17100u),
            () =>
            {
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(demo.CurrentGameTick, Is.EqualTo(new GameTick_t(17100u)));
        Assert.That(timerTick1, Is.EqualTo(default(GameTick_t)));
    }

    [Test]
    public async Task StartTimer_GameTick_State()
    {
        // Arrange
        GameTick_t timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.StartTimer(
            new GameTick_t(17100u),
            demo,
            demo =>
            {
                timerTick1 = demo.CurrentGameTick;
                cts.Cancel();
            });

        // Act
        try
        {
            await demo.Start(SpaceVsForwardM1Stream, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new GameTick_t(17100u)));
    }
}
