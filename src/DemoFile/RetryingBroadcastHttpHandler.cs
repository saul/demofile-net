using System.Diagnostics;
using System.Net;

namespace DemoFile;

/// <summary>
/// Default HTTP handler for broadcasts. Limited automatic retries, with throttling on <c>/delta</c> requests to avoid being rate-limited by the server.
/// </summary>
public class RetryingBroadcastHttpHandler : DelegatingHandler
{
    private static long _lastDeltaRequestTimestamp;

    public RetryingBroadcastHttpHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    /// <summary>
    /// Maximum number of failed consecutive /delta HTTP requests before failing.
    /// </summary>
    public int MaxRetries { get; set; } = 10;

    /// <summary>
    /// Amount of time to wait between each retry of failed HTTP requests.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Throttle period between <c>/delta</c> HTTP requests.
    /// Even when the HTTP request is successful, there should be a pause between requests,
    /// otherwise you could get rate-limited by server. This is especially important during
    /// the initial startup of the <see cref="HttpBroadcastReader"/>, when 20-30 seconds of
    /// gameplay data is requested.
    /// </summary>
    public TimeSpan DeltaRequestInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isDeltaRequest = request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/delta", StringComparison.OrdinalIgnoreCase);
        if (!isDeltaRequest)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Throttle requests to the `/delta` endpoint
        await ThrottleDeltaRequest(cancellationToken).ConfigureAwait(false);

        var retryCount = 0;
        while (true)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                _lastDeltaRequestTimestamp = Stopwatch.GetTimestamp();
                return response;
            }

            response.Dispose();

            retryCount += 1;
            if (retryCount >= MaxRetries)
            {
                throw new HttpRequestException($"Stopping after {MaxRetries} consecutive '404 Not Found' errors");
            }

            await Task.Delay(RetryInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ThrottleDeltaRequest(CancellationToken cancellationToken)
    {
        var elapsedSinceLastDeltaRequest = Stopwatch.GetElapsedTime(_lastDeltaRequestTimestamp);
        var timeToSleep = DeltaRequestInterval - elapsedSinceLastDeltaRequest;

        if (timeToSleep <= TimeSpan.Zero)
            return;

        await Task.Delay(timeToSleep, cancellationToken).ConfigureAwait(false);
    }
}
