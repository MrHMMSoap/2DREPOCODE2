using System;

namespace _2DREPOCODE.Models
{
    /// <summary>
    /// Represents a user account with authentication credentials.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }

        public User()
        {
            Username = string.Empty;
            PasswordHash = string.Empty;
            CreatedAt = DateTime.Now;
        }

        public User(int userId, string username, string passwordHash, DateTime createdAt)
        {
            UserId = userId;
            Username = username;
            PasswordHash = passwordHash;
            CreatedAt = createdAt;
        }
    }
}
