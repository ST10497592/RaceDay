
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers;

// START: AUTHENTICATION CONTROLLER
/// <summary>Registration, login and logout.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        RaceDayDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // START: REGISTER
    /// <summary>Registers a new Participant or Organiser account.</summary>
    /// <remarks>
    /// Body: firstName, lastName, email, password, role (Participant or Organiser),
    /// optional phoneNumber and dateOfBirth. Organiser accounts also need the
    /// organiserCode configured on the server. Passwords are stored hashed.
    /// Returns 201 Created, 400 invalid data, 403 wrong organiser code, 409 email already registered.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = string.IsNullOrWhiteSpace(request.Role)
            ? "Participant"
            : request.Role.Trim();

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(
                nameof(request.DateOfBirth),
                "Date of birth must be in the past.");

            return ValidationProblem(ModelState);
        }

        // Organiser accounts can only be created with the server's access code.
        if (role == "Organiser")
        {
            var expectedCode = _configuration["Registration:OrganiserCode"];

            if (string.IsNullOrWhiteSpace(expectedCode) ||
                !CodesMatch(expectedCode, request.OrganiserCode ?? string.Empty))
            {
                return StatusCode(403, new
                {
                    message = "A valid organiser access code is required to register as an Organiser."
                });
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new
            {
                message = "This email address is already registered."
            });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = role,
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth?.Date,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = "Account created successfully.",
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role
        });
    }
    // END: REGISTER

    // START: LOGIN
    /// <summary>Authenticates a user and returns a JWT containing the user ID and role.</summary>
    /// <remarks>Body: email and password. Returns 200 with the token, or 401 for invalid credentials.</remarks>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password
            );

            await _context.SaveChangesAsync();
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key is missing.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );

        var expiryMinutes = int.TryParse(
            jwtSection["ExpiryMinutes"],
            out var minutes
        ) ? minutes : 60;

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo,
            user = new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.ProfilePictureUrl
            }
        });
    }
    // END: LOGIN

    // START: LOGOUT
    /// <summary>Ends the authenticated session.</summary>
    /// <remarks>
    /// JWTs are stateless, so the client ends the session by discarding the token
    /// (the MVC application also clears its server-side session).
    /// Returns 200, or 401 when no valid token is supplied.
    /// </remarks>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        return Ok(new
        {
            message = "Signed out successfully. Discard the token on the client."
        });
    }
    // END: LOGOUT

    private static bool CodesMatch(string expected, string supplied)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(supplied));
    }
}
// END: AUTHENTICATION CONTROLLER
