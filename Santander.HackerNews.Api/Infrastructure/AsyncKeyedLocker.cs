using System.Collections.Concurrent;

namespace Santander.HackerNews.Api.Infrastructure;

/// <summary>
/// Provides async-safe mutual exclusion scoped by a string key.
/// This is used to prevent concurrent execution of the same operation,
/// such as cache rebuilds, while allowing unrelated keys to proceed in parallel.
/// </summary>
internal sealed class AsyncKeyedLocker
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Acquires an asynchronous lock for the specified key.
    /// Callers must dispose the returned handle to release the lock.
    /// </summary>
    /// <param name="key">The key that identifies the lock scope.</param>
    /// <param name="ct">A cancellation token used while waiting for the lock.</param>
    /// <returns>
    /// A disposable handle that releases the lock when disposed.
    /// </returns>
    public async Task<IDisposable> LockAsync(string key, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser(SemaphoreSlim sem) : IDisposable
    {
        private readonly SemaphoreSlim _sem = sem;

        public void Dispose()
        {
            _sem.Release();
        }
    }
}