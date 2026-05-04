using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;
using _2DREPOCODE.Models;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles user authentication, registration, and password management.
    /// Uses SHA256 hashing for secure password storage.
    /// </summary>
    public class AuthHandler
    {
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the AuthHandler with the specified database file.
        /// </summary>
        /// <param name="databaseFileName">Name of the authentication database file.</param>
        public AuthHandler(string databaseFileName = "users.db")
        {
            string databasePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseFileName);
            connectionString = $"Data Source={databasePath}";
        }

        /// <summary>
        /// Creates the users table if it doesn't exist.
        /// This method is safe to call multiple times - it only creates the table if missing.
        /// </summary>
        public void InitializeDatabase()
        {
            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Define the Users table structure for storing user credentials
            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
            ";

            using SqliteCommand command = new SqliteCommand(createUsersTable, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Hashes a password using SHA256 algorithm.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password as a hexadecimal string.</returns>
        private string HashPassword(string password)
        {
            // Convert password to bytes and compute hash
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = SHA256.HashData(passwordBytes);

            // Convert hash bytes to hexadecimal string
            StringBuilder hexString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hexString.Append(b.ToString("x2"));
            }

            return hexString.ToString();
        }

        /// <summary>
        /// Registers a new user with the specified username and password.
        /// </summary>
        /// <param name="username">The desired username (must be unique).</param>
        /// <param name="password">The user's password (will be hashed before storage).</param>
        /// <returns>True if registration successful, false if username already exists or error occurred.</returns>
        public bool RegisterUser(string username, string password)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Username and password cannot be empty.");
                return false;
            }

            if (username.Length < 3)
            {
                Console.WriteLine("Username must be at least 3 characters long.");
                return false;
            }

            if (password.Length < 6)
            {
                Console.WriteLine("Password must be at least 6 characters long.");
                return false;
            }

            try
            {
                using SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Hash the password before storing
                string passwordHash = HashPassword(password);
                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Insert new user into database
                string insertUserQuery = @"
                    INSERT INTO Users (Username, PasswordHash, CreatedAt)
                    VALUES (@Username, @PasswordHash, @CreatedAt);
                ";

                using SqliteCommand command = new SqliteCommand(insertUserQuery, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);
                command.ExecuteNonQuery();

                Console.WriteLine($"User '{username}' registered successfully!");
                return true;
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                // Username already exists
                Console.WriteLine($"Username '{username}' is already taken.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Registration failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Authenticates a user with the specified username and password.
        /// </summary>
        /// <param name="username">The username to authenticate.</param>
        /// <param name="password">The password to verify.</param>
        /// <returns>A User object if authentication successful, null otherwise.</returns>
        public User? AuthenticateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Username and password cannot be empty.");
                return null;
            }

            try
            {
                using SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Hash the provided password to compare with stored hash
                string passwordHash = HashPassword(password);

                // Query for user with matching username and password hash
                string queryUser = @"
                    SELECT UserId, Username, PasswordHash, CreatedAt
                    FROM Users
                    WHERE Username = @Username AND PasswordHash = @PasswordHash;
                ";

                using SqliteCommand command = new SqliteCommand(queryUser, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                using SqliteDataReader reader = command.ExecuteReader();

                // If user found, return User object
                if (reader.Read())
                {
                    User user = new User(
                        userId: reader.GetInt32(0),
                        username: reader.GetString(1),
                        passwordHash: reader.GetString(2),
                        createdAt: DateTime.Parse(reader.GetString(3))
                    );

                    Console.WriteLine($"Welcome back, {username}!");
                    return user;
                }
                else
                {
                    Console.WriteLine("Invalid username or password.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Authentication failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Changes the password for an existing user.
        /// </summary>
        /// <param name="username">The username whose password to change.</param>
        /// <param name="oldPassword">The current password for verification.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if password changed successfully, false otherwise.</returns>
        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            // First, verify the old password
            User? user = AuthenticateUser(username, oldPassword);
            if (user == null)
            {
                return false;
            }

            // Validate new password
            if (newPassword.Length < 6)
            {
                Console.WriteLine("New password must be at least 6 characters long.");
                return false;
            }

            try
            {
                using SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                // Hash the new password
                string newPasswordHash = HashPassword(newPassword);

                // Update the password hash
                string updateQuery = @"
                    UPDATE Users
                    SET PasswordHash = @NewPasswordHash
                    WHERE UserId = @UserId;
                ";

                using SqliteCommand command = new SqliteCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                command.Parameters.AddWithValue("@UserId", user.UserId);
                command.ExecuteNonQuery();

                Console.WriteLine("Password changed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to change password: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if a username already exists in the database.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if username exists, false otherwise.</returns>
        public bool UserExists(string username)
        {
            try
            {
                using SqliteConnection connection = new SqliteConnection(connectionString);
                connection.Open();

                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username;";

                using SqliteCommand command = new SqliteCommand(checkQuery, connection);
                command.Parameters.AddWithValue("@Username", username);

                long count = (long)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking username: " + ex.Message);
                return false;
            }
        }
    }
}
