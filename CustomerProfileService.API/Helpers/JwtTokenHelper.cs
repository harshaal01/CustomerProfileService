using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using CustomerProfileService.Domain.Entities;

namespace CustomerProfileService.API.Helpers
{
    public static class JwtTokenHelper
    {
        public static string GenerateToken(User user, IConfiguration config)
        {
            // 🔹 Step 1: Create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Id", user.Id.ToString()) // custom claim
            };

            // 🔹 Step 2: Create signing key
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["ConnectionStrings:AuthSecretKey"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 🔹 Step 3: Create JWT token
            var token = new JwtSecurityToken(
                issuer: config["CustomerProfile"],           // optional if you validate issuer
                audience: config["CustomerProfileUser"],    // optional if you validate audience
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // 🔹 Step 4: Return token string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
