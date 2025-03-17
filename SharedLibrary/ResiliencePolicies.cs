using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Extensions.Http;
using RabbitMQ.Client.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace SharedLibrary
{
    public static class ResiliencePolicies
    {
        //HTTP Fault Isolation Policies (for API calls)
        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (exception, duration, context) =>
                    {
                        Console.WriteLine($"Circuit breaker OPEN for {duration.TotalSeconds} seconds.");
                    },
                    onReset: (context) =>
                    {
                        Console.WriteLine("Circuit breaker CLOSED.");
                    });

        private static readonly AsyncRetryPolicy<HttpResponseMessage> RetryPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        Console.WriteLine($" Retrying {retryCount} after {delay.TotalSeconds} seconds.");
                    });

        public static IAsyncPolicy<HttpResponseMessage> GetHttpResiliencePolicy()
        {
            return Policy.WrapAsync(CircuitBreakerPolicy, RetryPolicy);
        }
        
        private static readonly AsyncRetryPolicy DatabaseRetryPolicy =
            Policy
                .Handle<DbUpdateException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, delay, retryCount, context) =>
                    {
                        Console.WriteLine($" Retrying Database Operation (Attempt {retryCount}) after {delay.TotalSeconds}s due to {exception.Message}");
                    });

        private static readonly AsyncCircuitBreakerPolicy DatabaseCircuitBreakerPolicy =
            Policy
                .Handle<DbUpdateException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10),
                    onBreak: (exception, duration) =>
                    {
                        Console.WriteLine($"Database Circuit Breaker OPEN! Breaking for {duration.TotalSeconds}s due to: {exception.Message}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("✅ Database Circuit Breaker RESET.");
                    });

        public static IAsyncPolicy GetDatabaseResiliencePolicy()
        {
            return Policy.WrapAsync(DatabaseCircuitBreakerPolicy, DatabaseRetryPolicy);
        }

        //  RabbitMQ Resilience Policies
        private static readonly AsyncRetryPolicy RabbitMqRetryPolicy =
            Policy
                .Handle<BrokerUnreachableException>()
                .Or<IOException>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryAttempt))), 
                    (exception, timeSpan) =>
                    {
                        Console.WriteLine($"[RabbitMQ] Retrying connection after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });

        private static readonly AsyncCircuitBreakerPolicy RabbitMqCircuitBreakerPolicy =
            Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(60),
                    onBreak: (exception, duration) =>
                    {
                        Console.WriteLine($"[RabbitMQ] Circuit breaker OPEN! Skipping retries for {duration.TotalSeconds}s due to: {exception.Message}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("[RabbitMQ] Circuit breaker RESET. Resuming normal operations.");
                    });

        public static IAsyncPolicy GetRabbitMqResiliencePolicy()
        {
            return Policy.WrapAsync(RabbitMqCircuitBreakerPolicy, RabbitMqRetryPolicy);
        }
    }
}
