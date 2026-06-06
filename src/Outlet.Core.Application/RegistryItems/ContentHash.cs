using System.Security.Cryptography;
using System.Text;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Stable content hash of a written file, recorded in the lockfile so that
/// <c>diff</c>/<c>update</c> can tell an untouched copy from a locally edited one.
/// </summary>
public static class ContentHash
{
    public static string Of(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
