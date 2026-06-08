using System.Security.Cryptography;
using System.Text;
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Infrastructure.Security;

/// <summary>
/// SECONDARY ADAPTER — mints a token from a 256-bit CSPRNG secret prefixed
/// <c>outlet_pat_</c> (GitHub-style), and persists only its SHA-256 digest. The
/// plaintext is returned once and never stored.
/// </summary>
public sealed class Sha256TokenSecretFactory : ITokenSecretFactory
{
    private const string Prefix = "outlet_pat_";

    public GeneratedTokenSecret Create()
    {
        var id = PersonalAccessTokenId.From(Guid.NewGuid());

        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Prefix + Convert.ToHexStringLower(randomBytes);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        var hash = TokenHash.From(Convert.ToHexStringLower(digest));

        return new GeneratedTokenSecret(id, secret, hash);
    }
}
