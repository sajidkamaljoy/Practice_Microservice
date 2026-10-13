using JwtConfiguration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JwtConfiguration
{
    public class JwtTokenHandler
    {
        private readonly JwtSettings _settings;

        public JwtTokenHandler(IConfiguration configuration)
        {
            _settings = JwtSettings.FromConfiguration(configuration);
        }

        public AuthenticationResponse? GenerateJwtToken(AuthenticationRequest request)
        {
            if(string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return null;
            }

            var user = _settings.Users.FirstOrDefault(u =>
                string.Equals(u.Username, request.Username, StringComparison.Ordinal) &&
                SecureEquals(u.Password, request.Password));
            if(user == null)
            {
                return null;
            }

            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(_settings.ValidityMinutes);
            var claimsIdentity = new ClaimsIdentity(new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, request.Username),
                new(/*ClaimTypes.Role*/"Role", user.Role)
            });

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_settings.KeyBytes),
                SecurityAlgorithms.HmacSha256Signature
            );

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = tokenExpiryTimeStamp,
                SigningCredentials = signingCredentials
            };

            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);
            var token = jwtSecurityTokenHandler.WriteToken(jwtToken);

            return new AuthenticationResponse
            {
                Username = user.Username,
                JwtToken = token,
                ExpiresIn = (int)tokenExpiryTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
            };
        }

        // Constant-time comparison so response timing does not reveal how much of a password matched.
        private static bool SecureEquals(string expected, string actual) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
    }
}
