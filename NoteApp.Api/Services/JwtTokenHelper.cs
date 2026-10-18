using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NoteApp.Api.Services;

public static class JwtTokenHelper
{
    public const string SigningKeyId = "NoteAppSigningKey";

    public static SymmetricSecurityKey CreateSecurityKey(string secret)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        {
            KeyId = SigningKeyId
        };
    }
}
