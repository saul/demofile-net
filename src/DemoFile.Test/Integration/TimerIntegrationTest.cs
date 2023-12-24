namespace DemoFile.Test.Integration;

[TestFixture(true)]
[TestFixture(false)]
public class TimerIntegrationTest
{
    private readonly bool _readAll;

    public TimerIntegrationTest(bool readAll)
    {
        _readAll = readAll;
    }

    [Test]
    public async Task StartTimer_DemoTick()
    {
        // Arrange
        DemoTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.CreateTimer(
            new DemoTick(128),
            () =>
            {
                timerTick1 = demo.CurrentDemoTick;
                cts.Cancel();
            });

        // Act
        try
        {
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
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
        var disposable = demo.CreateTimer(
            new DemoTick(128),
            () =>
            {
                timerTick1 = demo.CurrentDemoTick;
            });

        demo.CreateTimer(
            new DemoTick(127),
            () =>
            {
                disposable.Dispose();
            });

        demo.CreateTimer(
            new DemoTick(129),
            () =>
            {
                cts.Cancel();
            });

        // Act
        try
        {
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
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
        demo.CreateTimer(
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
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
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
        GameTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.CreateTimer(
            new GameTick(120_000u),
            () =>
            {
                timerTick1 = demo.CurrentGameTick;
                cts.Cancel();
            });

        // Act
        try
        {
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new GameTick(120_000u)));
    }

    [Test]
    public async Task StartTimer_GameTick_Cancel()
    {
        // Arrange
        GameTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        var disposable = demo.CreateTimer(
            new GameTick(119_999u),
            () =>
            {
                timerTick1 = demo.CurrentGameTick;
            });

        demo.CreateTimer(
            new GameTick(119_998u),
            () =>
            {
                disposable.Dispose();
            });

        demo.CreateTimer(
            new GameTick(120_000u),
            () =>
            {
                cts.Cancel();
            });

        // Act
        try
        {
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(demo.CurrentGameTick, Is.EqualTo(new GameTick(120_000u)));
        Assert.That(timerTick1, Is.EqualTo(default(GameTick)));
    }

    [Test]
    public async Task StartTimer_GameTick_State()
    {
        // Arrange
        GameTick timerTick1 = default;

        var cts = new CancellationTokenSource();
        var demo = new DemoParser();
        demo.CreateTimer(
            new GameTick(120_000u),
            demo,
            demo =>
            {
                timerTick1 = demo.CurrentGameTick;
                cts.Cancel();
            });

        // Act
        try
        {
            if (_readAll)
            {
                await demo.ReadAllAsync(GotvCompetitiveProtocol13963, cts.Token);
            }
            else
            {
                await demo.StartReadingAsync(GotvCompetitiveProtocol13963, cts.Token);
                while (await demo.MoveNextAsync(cts.Token))
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        // Assert
        Assert.That(timerTick1, Is.EqualTo(new GameTick(120_000u)));
    }
}
