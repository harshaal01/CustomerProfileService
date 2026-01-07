using MySql.Data.MySqlClient;
using CustomerProfileService.Domain.Entities;
using CustomerProfileService.Infrastructure.Query;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

public class AuthService : IAuthService
{
    private readonly SQLHelper _db;
    private readonly ILogger<AuthService> _logger;

    public AuthService(SQLHelper db, ILogger<AuthService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegisterAsync(User user)
    {
        try
        {
            // 🔹 Step 1: Validate input
            ValidateUser(user);

            // 🔹 Step 2: Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            // 🔹 Step 3: Insert user
            var rowsAffected = await _db.ExecuteNonQuery(
                QueryGenerator.RegisterUser(),
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Name", user.Name.Trim());
                    cmd.Parameters.AddWithValue("@Email", user.Email.Trim());
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                });

            // 🔹 Step 4: Business validation
            if (rowsAffected <= 0)
            {
                _logger.LogError(
                    "User registration failed. No rows affected. Email: {Email}",
                    user.Email
                );
                throw new Exception("User registration failed.");
            }

            _logger.LogInformation(
                "User registered successfully. Email: {Email}",
                user.Email
            );
        }
        catch (ValidationException)
        {
            _logger.LogWarning(
                "Validation failed while registering user. Email: {Email}",
                user.Email
            );
            throw;
        }
        catch (MySqlException ex)
        {
            if (ex.Number == 1062)
            {
                _logger.LogWarning(
                    "Duplicate email detected during registration. Email: {Email}",
                    user.Email
                );
                throw new ValidationException("Email already exists.");
            }

            _logger.LogError(ex, "Database error occurred during user registration.");
            throw new ApplicationException("Database error occurred.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in RegisterAsync");
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();
        }
    }


    public async Task<User> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Email and password are required.");

            // 🔹 Step 1: Fetch user from DB by email
            var user = await QuerySingleOrDefaultAsync(
                QueryGenerator.GetUserByEmail(),
                cmd => cmd.Parameters.AddWithValue("@Email", email.Trim())
            );

            if (user == null)
            {
                _logger.LogWarning("Login failed. User not found. Email: {Email}", email);
                throw new ValidationException("Invalid credentials.");
            }

            // 🔹 Step 2: Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed. Invalid password. Email: {Email}", email);
                throw new ValidationException("Invalid credentials.");
            }

            // 🔹 Step 3: Return user for token generation
            _logger.LogInformation("User logged in successfully. Email: {Email}", email);
            return user;
        }
        catch (ValidationException)
        {
            throw; // already handled
        }
        catch (MySqlException ex)
        {
            _logger.LogError(ex, "Database error occurred during login. Email: {Email}", email);
            throw new ApplicationException("Database error occurred.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in LoginAsync. Email: {Email}", email);
            throw;
        }
        finally
        {
            _db.CloseSqlConnection();
        }
    }



    private void ValidateUser(User user)
    {
        if (user == null)
            throw new ValidationException("User data is required.");

        if (string.IsNullOrWhiteSpace(user.Name))
            throw new ValidationException("Name is required.");

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ValidationException("Email is required.");

        if (!new EmailAddressAttribute().IsValid(user.Email))
            throw new ValidationException("Invalid email format.");

        if (string.IsNullOrWhiteSpace(user.Password))
            throw new ValidationException("Password is required.");

        if (user.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");
    }

    public async Task<User?> QuerySingleOrDefaultAsync(string query, Action<MySqlCommand> param)
    {
        try
        {
            using var reader = await _db.ExecuteQueryAsync(query, param);

            if (await reader.ReadAsync())
            {
                // Map the reader to User entity
                var user = new User
                {
                    Id = SQLHelper.GetIntValue(reader, "Id"),
                    Name = SQLHelper.GetStringValue(reader, "Name"),
                    Email = SQLHelper.GetStringValue(reader, "Email"),
                    Password = SQLHelper.GetStringValue(reader, "Password")
                };

                return user;
            }

            return null; // No record found
        }
        catch (Exception ex)
        {
            // Log exception here if needed
            throw new Exception("Error executing QuerySingleOrDefaultAsync.", ex);
        }
        finally
        {
            _db.CloseSqlConnection(); // ensure connection is closed
        }
    }

}
