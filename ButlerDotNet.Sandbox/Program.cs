using System.Text.Json.Serialization;
using ButlerDotNet.Structs;
using ButlerDotNet.Utilities;
using ButlerDotNet.Utilities.Extensions;
using Serilog;
using Version = SemanticVersioning.Version;

namespace ButlerDotNet.Sandbox;

internal static class Program
{
    private static readonly Version ButlerVersion = new(15, 24, 0);

    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        if (!ButlerInstaller.TryGetForCurrentPlatform(out var installer))
        {
            Log.Error("Failed to initialize installer.");
            return;
        }

        Log.Information("Ensuring butler is installed...");

        if (!await installer.TryEnsureInstalledAsync(ButlerVersion))
        {
            Log.Error("Failed to ensure butler version {ButlerVersion} was installed.", ButlerVersion);
            // Log.Error("Failed to ensure latest butler version was installed.");
            return;
        }

        try
        {
            await RunAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Program terminated unexpectedly.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task RunAsync()
    {
        var client = ButlerClient.CreateWithDefaults();
        await client.StartAsync();

        const string tempDir = "temp/";
        const string archiveDir = "itch.io/";

        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(archiveDir);

        const long gameId = 257677; // Among Us

        var channelBuilds = new Dictionary<string, Build[]>();

        var session = await client.EstablishSessionAsync(b =>
        {
            b.HandleLogging();
            b.HandleProgress();

            b.OnRequest<InstallVersionSwitchPick, InstallVersionSwitchPick.Result>(
                "InstallVersionSwitchPick",
                h =>
                {
                    channelBuilds[h.Upload.ChannelName] = h.Builds;
                    return Task.FromResult(new InstallVersionSwitchPick.Result { Index = 0 });
                });
        });

        await Console.Out.WriteAsync("Username: ");
        var username = (await Console.In.ReadLineAsync())!;

        await Console.Out.WriteAsync("Password: ");
        var password = ConsoleUtilities.ReadPassword();

        var loginResponse = await session.Methods.Profile.LoginWithPassword(username, password);
        var profileId = loginResponse.Profile.Id;

        await session.SendRequest(
            "Fetch.GameRecords",
            new { profileId, source = "owned", fresh = true });

        var gameUploads = await session.Methods.Fetch.GameUploads(gameId, false, true);

        foreach (var upload in gameUploads.Uploads)
        {
            var stagingDir = Path.Join(tempDir, "staging", upload.ChannelName);
            Directory.CreateDirectory(stagingDir);

            var installDir = Path.Join(tempDir, "bootstrap", upload.ChannelName);
            Directory.CreateDirectory(installDir);

            var installId = $"bootstrap-{upload.ChannelName}";

            await session.SendRequest(
                "Install.Locations.Add",
                new { id = installId, path = installDir });

            var queueInstallResponse = await session.SendRequest<Schemas.Install.Queue.Result>(
                "Install.Queue",
                new
                {
                    reason = "install",
                    installLocationId = installId,
                    game = new { id = gameId },
                    upload,
                    ignoreInstallers = true,
                    stagingFolder = stagingDir
                });

            var performInstallResult = await session.SendRequest<Schemas.Install.Perform.Result>(
                "Install.Perform",
                new { id = installId, stagingFolder = queueInstallResponse.StagingFolder });

            await session.SendRequest(
                "Install.VersionSwitch.Queue",
                new { caveId = performInstallResult.CaveId });

            await session.SendRequest(
                "Uninstall.Perform",
                new { caveId = performInstallResult.CaveId, hard = true });
        }

        foreach (var (channel, builds) in channelBuilds)
        {
            var upload = gameUploads.Uploads.Single(u => u.ChannelName == channel);

            foreach (var build in builds)
            {
                var installId = $"archive-v{build.Version}-{build.Id}";
                var installDir = Path.Join(archiveDir, channel, build.Version.ToString());
                Directory.CreateDirectory(installDir);

                var queueInstallResponse = await session.SendRequest<Schemas.Install.Queue.Result>(
                    "Install.Queue",
                    new
                    {
                        reason = "install",
                        noCave = true,
                        installFolder = installDir,
                        game = new { id = gameId },
                        upload,
                        build,
                        ignoreInstallers = true,
                        stagingFolder = "staging"
                    });

                _ = await session.SendRequest<Schemas.Install.Perform.Result>(
                    "Install.Perform",
                    new { id = installId, stagingFolder = queueInstallResponse.StagingFolder });
            }
        }

        Log.Information("Archive summary:");
        foreach (var (channel, builds) in channelBuilds)
        {
            Log.Information(
                "\t{NumBuilds} builds for {Channel} (versions {OldestVersion}–{NewestVersion})",
                builds.Length,
                channel,
                builds.Min(b => b.Version),
                builds.Max(b => b.Version));
        }
    }

    public class InstallVersionSwitchPick
    {
        [JsonPropertyName("cave")] public required Cave Cave { get; init; }
        [JsonPropertyName("upload")] public required Upload Upload { get; init; }
        [JsonPropertyName("builds")] public required Build[] Builds { get; init; }

        public class Result
        {
            [JsonPropertyName("index")] public required int Index { get; init; }
        }
    }
}
