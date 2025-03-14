using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Fallback;
using Polly.Timeout;
using Polly.Retry;

namespace SharedLibrary
{
    public static class ResiliencePolicies
    {
        
        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 2, 
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (exception, duration, context) =>
                    {
                        Console.WriteLine($" Circuit breaker OPEN for {duration.TotalSeconds} seconds.");
                    },
                    onReset: (context) =>
                    {
                        Console.WriteLine("Circuit breaker CLOSED.");
                    });

        
        private static readonly AsyncFallbackPolicy<HttpResponseMessage> FallbackPolicy =
            Policy<HttpResponseMessage>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackAction: (context, cancellationToken) => 
                        Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Content = new StringContent(" Fallback response: Service unavailable.")
                        }),
                    onFallbackAsync: (outcome, context) =>
                    {
                        Console.WriteLine("🔄 Fallback policy triggered.");
                        return Task.CompletedTask;
                    });
        
        
        private static readonly AsyncRetryPolicy<HttpResponseMessage> RetryPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        Console.WriteLine($"🔁 Retrying {retryCount} after {delay.TotalSeconds} seconds.");
                    });

       
        private static readonly AsyncTimeoutPolicy<HttpResponseMessage> TimeoutPolicy =
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));

    
        public static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy()
        {
            return Policy.WrapAsync(FallbackPolicy, CircuitBreakerPolicy, RetryPolicy, TimeoutPolicy);
        }
    }
}
