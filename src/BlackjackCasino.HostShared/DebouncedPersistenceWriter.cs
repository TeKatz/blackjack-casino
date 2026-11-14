namespace BlackjackCasino.Desktop;

/// <summary>
/// String-compatible facade retained for the existing profile/preferences persistence paths.
/// </summary>
internal sealed class DebouncedPersistenceWriter : IDisposable
{
    private readonly DebouncedPersistenceWriter<string> inner;

    public DebouncedPersistenceWriter(TimeSpan delay, Action<string> write)
    {
        inner = new DebouncedPersistenceWriter<string>(delay, write);
    }

    public void Schedule(string payload) => inner.Schedule(payload);

    public void Flush() => inner.Flush();

    public void Dispose() => inner.Dispose();
}

/// <summary>
/// Coalesces routine persistence requests and keeps slow persistence work off the caller thread.
/// Flush remains available for shutdown/read-after-save boundaries that require completion.
/// </summary>
internal sealed class DebouncedPersistenceWriter<TPayload> : IDisposable
    where TPayload : class
{
    private readonly object gate = new();
    private readonly TimeSpan delay;
    private readonly Action<TPayload> write;
    private readonly System.Threading.Timer timer;
    private TPayload? pendingPayload;
    private bool writeInProgress;
    private bool disposed;

    public DebouncedPersistenceWriter(TimeSpan delay, Action<TPayload> write)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }

        this.delay = delay;
        this.write = write ?? throw new ArgumentNullException(nameof(write));
        timer = new System.Threading.Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Schedule(TPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            pendingPayload = payload;
            timer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Flush()
    {
        FlushCore();
    }

    private void OnTimer(object? state)
    {
        TPayload? payload;

        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            if (writeInProgress)
            {
                if (pendingPayload is not null)
                {
                    timer.Change(delay, Timeout.InfiniteTimeSpan);
                }
                return;
            }

            payload = pendingPayload;
            pendingPayload = null;
            if (payload is null)
            {
                return;
            }

            writeInProgress = true;
        }

        try
        {
            write(payload);
        }
        catch
        {
            // Routine persistence is best-effort. Critical transaction boundaries bypass
            // this writer and keep their existing throwing/durable save path.
        }
        finally
        {
            lock (gate)
            {
                writeInProgress = false;
                Monitor.PulseAll(gate);
                if (!disposed && pendingPayload is not null)
                {
                    timer.Change(delay, Timeout.InfiniteTimeSpan);
                }
            }
        }
    }

    private void FlushCore()
    {
        while (true)
        {
            TPayload? payload;

            lock (gate)
            {
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                while (writeInProgress)
                {
                    Monitor.Wait(gate);
                }

                payload = pendingPayload;
                pendingPayload = null;
                if (payload is null)
                {
                    return;
                }

                writeInProgress = true;
            }

            try
            {
                write(payload);
            }
            catch
            {
                // Match the legacy routine-save contract: persistence failures must not
                // interrupt gameplay or application shutdown.
            }
            finally
            {
                lock (gate)
                {
                    writeInProgress = false;
                    Monitor.PulseAll(gate);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        FlushCore();
        timer.Dispose();
    }
}
