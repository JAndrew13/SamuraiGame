using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Server.Models;
using SharedLibrary;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

// This was built following https://www.youtube.com/watch?v=4CAVmLTdr0M

public interface IAuthService
{
    (bool success, string content) Register(string username, string password);
    (bool success, string token) Login(string username, string password);

    void PingDb();
}

public class AuthService : IAuthService 
{
    private readonly Settings _settings;
    private readonly GameDbContext _context;

    public AuthService(Settings settings, GameDbContext context)
    {
        _settings = settings;
        _context = context;
    }

    /// <summary>
    /// Registers a new user with the provided username and password.
    /// </summary>
    /// <param name="username">The username of the new user.</param>
    /// <param name="password">The password of the new user.</param>
    /// <returns>A tuple indicating success and a content message.</returns>
    public (bool success, string content) Register(string username, string password)
    {
        if (_context.Users.Any(u => u.Username == username)) return (false, "Invalid username");

        var user = new User { Username = username, PasswordHash = password };
        user.ProvideSaltAndHash();

        _context.Users.Add(user);
        _context.SaveChanges();

        return (true, "");
    }

    /// <summary>
    /// Logs in a user with the provided username and password.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <returns>A tuple indicating success and a JWT token if successful.</returns>
    public (bool success, string token) Login(string username, string password)
    {
        var user = _context.Users.Include(u=>u.Heroes).SingleOrDefault(u => u.Username == username);
        if (user is null) return (false, "Invalid username");

        if (user.PasswordHash != AuthenticationHelpers.ComputeHash(password, user.Salt)) return (false, "Invalid password.");

        return (true, GenerateJwtToken(AssembleClaimsIdentity(user)));
    }

    /// <summary>
    /// Assembles a ClaimsIdentity for the provided user.
    /// </summary>
    /// <param name="user">The user for whom to assemble the ClaimsIdentity.</param>
    /// <returns>A ClaimsIdentity containing the user's claims.</returns>
    private ClaimsIdentity AssembleClaimsIdentity(User user)
    {
        var subject = new ClaimsIdentity(new[] {
            new Claim("Id", user.Id.ToString()),
            new Claim("Heroes",JsonConvert.SerializeObject( user.Heroes.Select(h=>h.Id)))

        });

        return subject;
    }

    /// <summary>
    /// Generates a JWT token for the provided ClaimsIdentity.
    /// </summary>
    /// <param name="subject">The ClaimsIdentity for which to generate the token.</param>
    /// <returns>A JWT token as a string.</returns>
    private string GenerateJwtToken(ClaimsIdentity subject)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_settings.BearerKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = subject,
            Expires = DateTime.Now.AddYears(10), // Update this and create refresh tokens!
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // Create a simple funtion that pings the database to see if the connection string is setup correctly and the sql server is running.
    // If an error is thrown, return it to the client.

    public void PingDb()
    {
        try
        {
            _context.Database.ExecuteSqlRaw("SELECT 1");
        }
        catch (Exception e)
        {
            throw new Exception("Database is not connected: " + e.Message);
        }
    }
}

public static class AuthenticationHelpers 
{
    /// <summary>
    /// Provides a salt and hash for the user's password.
    /// </summary>
    /// <param name="user">The user for whom to provide the salt and hash.</param>
    public static void ProvideSaltAndHash(this User user)
    {
        var salt = GenerateSalt();
        user.Salt = Convert.ToBase64String(salt);
        user.PasswordHash = ComputeHash(user.PasswordHash, user.Salt).ToString();
    }

    /// <summary>
    /// Generates a cryptographic salt.
    /// </summary>
    /// <returns>A byte array containing the generated salt.</returns>
    private static byte[] GenerateSalt()
    {
        var rng = RandomNumberGenerator.Create();
        var salt = new byte[24];
        rng.GetBytes(salt);
        return salt;
    }

    /// <summary>
    /// Computes a hash for the given password and salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="saltString">The salt to use for hashing.</param>
    /// <returns>A string containing the computed hash.</returns>
    public static string ComputeHash(string password, string saltString)
    {
        var salt = Convert.FromBase64String(saltString);

        using var hashGenerator = new Rfc2898DeriveBytes(password, salt);
        hashGenerator.IterationCount = 10101;
        var bytes = hashGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes);
    }
}


