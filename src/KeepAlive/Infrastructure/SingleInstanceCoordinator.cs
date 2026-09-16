using System.IO;
using System.IO.Pipes;

namespace KeepAlive.Infrastructure;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private const int NotificationAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMilliseconds(500);

    private readonly CancellationTokenSource _shutdown = new();
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private Task? _listenerTask;
    private bool _disposed;

    public SingleInstanceCoordinator(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        var safeId = string.Concat(applicationId.Select(character =>
            char.IsAsciiLetterOrDigit(character) ? character : '_'));

        _pipeName = $"{safeId}.Activation.v1";
        _mutex = new Mutex(
            initiallyOwned: true,
            name: $@"Local\{safeId}.SingleInstance.v1",
            createdNew: out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    public event EventHandler? ActivationRequested;

    public bool IsPrimaryInstance { get; }

    public void StartListening()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsPrimaryInstance)
        {
            throw new InvalidOperationException("Only the primary instance can listen for activation requests.");
        }

        _listenerTask ??= ListenAsync(_shutdown.Token);
    }

    public async Task<bool> NotifyPrimaryAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsPrimaryInstance)
        {
            return false;
        }

        for (var attempt = 0; attempt < NotificationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var client = new NamedPipeClientStream(
                    serverName: ".",
                    pipeName: _pipeName,
                    direction: PipeDirection.Out,
                    options: PipeOptions.Asynchronous);

                using var connectionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectionTimeout.CancelAfter(ConnectionTimeout);

                await client.ConnectAsync(connectionTimeout.Token);
                await client.WriteAsync(new byte[] { 1 }, cancellationToken);
                await client.FlushAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The primary instance may still be starting. Retry briefly.
            }
            catch (IOException)
            {
                // The activation pipe is not ready yet. Retry briefly.
            }

            await Task.Delay(RetryDelay, cancellationToken);
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();

        if (IsPrimaryInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The process is already shutting down and no longer owns the mutex.
            }
        }

        _mutex.Dispose();
        _shutdown.Dispose();
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    pipeName: _pipeName,
                    direction: PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);

                var command = new byte[1];
                var bytesRead = await server.ReadAsync(command, cancellationToken);
                if (bytesRead == 1 && command[0] == 1)
                {
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException) when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
