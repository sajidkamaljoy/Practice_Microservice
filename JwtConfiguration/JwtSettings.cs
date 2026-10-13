using System.Text;
using JwtConfiguration.Models;
using Microsoft.Extensions.Configuration;

namespace JwtConfiguration
{
    /// <summary>
    /// JWT settings read from configuration (environment variables, user-secrets,
    /// or a secret store). Nothing here is hard-coded in source.
    ///
    /// Expected configuration:
    ///   Jwt:Key                    signing key, at least 32 characters; must be identical in every service
    ///   Jwt:ValidityMinutes        optional, defaults to 20
    ///   Jwt:Users:0:Username       demo accounts used by AuthWebApi only
    ///   Jwt:Users:0:Password
    ///   Jwt:Users:0:Role
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";
        private const int MinKeyBytes = 32;
        private const int DefaultValidityMinutes = 20;

        public string Key { get; private init; } = string.Empty;
        public int ValidityMinutes { get; private init; } = DefaultValidityMinutes;
        public IReadOnlyList<UserAccount> Users { get; private init; } = Array.Empty<UserAccount>();

        public byte[] KeyBytes => Encoding.UTF8.GetBytes(Key);

        public static JwtSettings FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);

            var key = section["Key"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < MinKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Configuration value '{SectionName}:Key' is missing or shorter than {MinKeyBytes} characters. " +
                    "Set it with user-secrets or the environment variable Jwt__Key. See the README.");
            }

            var validityMinutes = DefaultValidityMinutes;
            if (int.TryParse(section["ValidityMinutes"], out var configured) && configured > 0)
            {
                validityMinutes = configured;
            }

            var users = new List<UserAccount>();
            foreach (var child in section.GetSection("Users").GetChildren())
            {
                var username = child["Username"];
                var password = child["Password"];
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    continue;
                }

                var role = child["Role"];
                users.Add(new UserAccount
                {
                    Username = username,
                    Password = password,
                    Role = string.IsNullOrEmpty(role) ? "User" : role
                });
            }

            return new JwtSettings
            {
                Key = key,
                ValidityMinutes = validityMinutes,
                Users = users
            };
        }
    }
}
