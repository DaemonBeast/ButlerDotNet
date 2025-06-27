using System.Diagnostics;
using ButlerDotNet.Schemas.Butlerd;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace ButlerDotNet;

public partial class ButlerClient
{
    internal readonly ILogger<ButlerClient> Logger;

    private bool _initializing;

    private readonly Process _butlerProcess;

    private readonly TaskCompletionSource _initializedTaskCompletionSource;

    public static ButlerClient CreateWithDefaults()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

        using var loggerFactory = new SerilogLoggerFactory(serilogLogger);
        var logger = loggerFactory.CreateLogger<ButlerClient>();

        return new ButlerClient(logger);
    }

    public ButlerClient(ILogger<ButlerClient> logger)
    {
        Logger = logger;

        _butlerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "butler",
                // TODO: Allow changing database path using configuration.
                Arguments = $"daemon --json --dbpath butler.db --destiny-pid {Environment.ProcessId} --keep-alive --log --verbose",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        _initializedTaskCompletionSource = new TaskCompletionSource();

        RegisterButlerdNotificationHandler<ListenNotificationHandler, ListenNotification>();
        RegisterButlerdNotificationHandler<LogNotificationHandler, LogNotification>();
    }

    public Task StartAsync()
    {
        if (_initializing) return _initializedTaskCompletionSource.Task;
        _initializing = true;

        StartButlerProcess();

        _ = Task.Run(HandleButlerdNotificationsAsync);

        return _initializedTaskCompletionSource.Task;
    }
}
