using System.Diagnostics.CodeAnalysis;

namespace ButlerDotNet.Utilities;

public static class AddressUtils
{
    public static bool TryParseIPv4(
        string address,
        [NotNullWhen(true)] out string? hostname,
        [NotNullWhen(true)] out int? port)
    {
        hostname = null;
        port = null;

        // Not a big deal if there's surrounding whitespace. Just trim it.
        var trimmedAddress = address.Trim();

        // It is a big deal if there's whitespace inside the string.
        if (trimmedAddress.Any(char.IsWhiteSpace)) return false;

        var parts = trimmedAddress.Split(':');
        if (parts.Length != 2) return false;

        var unsafeHostname = parts[0];
        var unsafePortString = parts[1];

        // Just verifying.
        var unsafeHostnameParts = unsafeHostname.Split('.');
        if (unsafeHostnameParts.Length != 4 ||
            unsafeHostnameParts.Any(p => !int.TryParse(p, out var octet) || octet < 0 || octet > 255)) return false;

        if (!int.TryParse(unsafePortString, out var unsafePort) || unsafePort < 0 || unsafePort > 65535) return false;

        hostname = unsafeHostname;
        port = unsafePort;
        return true;
    }
}
