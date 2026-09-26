using System.Linq;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Crypto_Hockey.Services;

internal static class RewardSignatureHex
{
    public static bool TryNormalize(string? signature, out string normalizedSignatureHex)
    {
        normalizedSignatureHex = string.Empty;

        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var normalized = signature.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (normalized.Length % 2 != 0)
        {
            normalized = $"0{normalized}";
        }

        if (normalized.Length != 130 || !normalized.All(Uri.IsHexDigit))
        {
            return false;
        }

        normalizedSignatureHex = normalized;
        return true;
    }

    public static bool TryParseBytes(string? signature, out byte[] signatureBytes)
    {
        signatureBytes = [];
        if (!TryNormalize(signature, out var normalizedSignatureHex))
        {
            return false;
        }

        signatureBytes = normalizedSignatureHex.HexToByteArray();
        return true;
    }
}
