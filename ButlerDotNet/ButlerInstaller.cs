using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Version = SemanticVersioning.Version;

namespace ButlerDotNet;

public class ButlerInstaller
{
    // TODO: Add ability to verify installed files.
    // TODO: Clean up any extra files from old installs.

    public static OSPlatform[] SupportedPlatforms { get; } = [OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX];

    public string PlatformRuntimeId { get; }

    private readonly HttpClient _httpClient;

    private const string ButlerVersionFilePath = ".butler_version";

    public static bool TryGetCurrentPlatformRuntimeId(
        [NotNullWhen(true)] out string? runtimeId,
        [NotNullWhen(true)] out OSPlatform? platform,
        [NotNullWhen(true)] out bool? is64Bit)
    {
        platform = SupportedPlatforms.FirstOrDefault(RuntimeInformation.IsOSPlatform);
        is64Bit = Environment.Is64BitOperatingSystem;

        if (platform == default ||
            !is64Bit.Value && platform != OSPlatform.Windows) // If 32-bit, only Windows is allowed
        {
            runtimeId = null;
            return false;
        }

        var suffix = is64Bit.Value ? "amd64" : "386";

        string? prefix = null;
        if (platform == OSPlatform.Windows) prefix = "windows";
        if (platform == OSPlatform.Linux) prefix = "linux";
        if (platform == OSPlatform.OSX) prefix = "darwin";

        if (prefix == null) throw new Exception("Could not determine runtime ID for supported platform.");

        runtimeId = $"{prefix}-{suffix}";
        return true;
    }

    public static bool TryGetForCurrentPlatform([NotNullWhen(true)] out ButlerInstaller? installer)
    {
        if (!TryGetCurrentPlatformRuntimeId(out var runtimeId, out _, out _))
        {
            installer = null;
            return false;
        }

        installer = new ButlerInstaller(runtimeId);;
        return true;
    }

    private ButlerInstaller(string platformRuntimeId)
    {
        PlatformRuntimeId = platformRuntimeId;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                Path.Combine("https://broth.itch.zone", "butler", PlatformRuntimeId) + Path.DirectorySeparatorChar)
        };
    }

    public async Task<bool> TryEnsureLatestInstalledAsync(
        bool forceReinstall = false,
        bool ignoreCache = false,
        CancellationToken cancellationToken = default)
    {
        var latestVersion = await TryDetermineLatestVersionAsync(cancellationToken);
        if (latestVersion == null) return false;

        return await TryEnsureInstalledAsync(latestVersion, forceReinstall, ignoreCache, cancellationToken);
    }

    public ValueTask<bool> TryEnsureInstalledAsync(
        Version version,
        bool forceReinstall = false,
        bool ignoreCache = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceReinstall)
        {
            if (!TryDetermineCurrentVersion(out var currentVersion)) return ValueTask.FromResult(false);
            if (version == currentVersion) return ValueTask.FromResult(true);
        }

        return TryDownloadVersionAsync(version, ignoreCache, cancellationToken);
    }

    public bool TryDetermineCurrentVersion([NotNullWhen(true)] out Version? version)
    {
        if (!File.Exists(ButlerVersionFilePath))
        {
            version = null;
            return false;
        }

        var versionText = File.ReadAllText(ButlerVersionFilePath);
        return Version.TryParse(versionText, out version);
    }

    public async Task<Version?> TryDetermineLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        string versionString;
        try
        {
            versionString = await _httpClient.GetStringAsync("LATEST", cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null;
        }

        return Version.TryParse(versionString, out var version) ? version : null;
    }

    public async ValueTask<bool> TryDownloadVersionAsync(
        Version version,
        bool ignoreCache = false,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.GetTempPath();
        var tempButlerPath = Path.Combine(tempPath, nameof(ButlerDotNet), "installer", "butler", version.ToString());

        if (ignoreCache || !Directory.Exists(tempButlerPath))
        {
            if (!await TryDownloadVersionZipAsync(version, tempButlerPath, cancellationToken))
            {
                return false;
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        foreach (var file in Directory.EnumerateFiles(tempButlerPath))
        {
            File.Copy(file, Path.Combine(currentDirectory, Path.GetFileName(file)), true);
        }

        await File.WriteAllTextAsync(ButlerVersionFilePath, version.ToString(), cancellationToken);

        return true;
    }

    private async Task<bool> TryDownloadVersionZipAsync(
        Version version,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var downloadStream = await _httpClient.GetStreamAsync(
                Path.Combine(version.ToString(), "archive", "default"),
                cancellationToken);

            ZipFile.ExtractToDirectory(downloadStream, outputPath, true);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return false;
        }

        return true;
    }
}
