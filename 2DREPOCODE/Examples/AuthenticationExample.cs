using _2DREPOCODE.Handlers;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;

namespace _2DREPOCODE.Examples
{
    /// <summary>
    /// Example demonstrating how to use the authentication and save system.
    /// </summary>
    public class AuthenticationExample
    {
        public static void RunExample()
        {
            Console.WriteLine("=== Authentication System Example ===\n");

            // Initialize the authentication handler
            AuthHandler authHandler = new AuthHandler();
            authHandler.InitializeDatabase();

            // Initialize the SQL handler for game saves
            SQLHandler sqlHandler = new SQLHandler();
            sqlHandler.InitializeDatabase();

            // Example 1: Register a new user
            Console.WriteLine("--- Example 1: User Registration ---");
            bool registrationSuccess = authHandler.RegisterUser("john_doe", "securepass123");
            if (registrationSuccess)
            {
                Console.WriteLine("User registered successfully!\n");
            }

            // Example 2: Authenticate (login) a user
            Console.WriteLine("--- Example 2: User Authentication ---");
            User? loggedInUser = authHandler.AuthenticateUser("john_doe", "securepass123");
            if (loggedInUser != null)
            {
                Console.WriteLine($"User '{loggedInUser.Username}' logged in successfully!");
                Console.WriteLine($"User ID: {loggedInUser.UserId}");
                Console.WriteLine($"Account created: {loggedInUser.CreatedAt}\n");

                // Set the current user for the SQL handler
                sqlHandler.SetCurrentUser(loggedInUser);

                // Example 3: Save game data for the logged-in user
                Console.WriteLine("--- Example 3: Save Game Data ---");
                Dictionary<string, int> upgrades = new Dictionary<string, int>
                {
                    { "Health Upgrade", 2 },
                    { "Damage Upgrade", 1 }
                };

                sqlHandler.SaveGame(
                    playerHP: 85,
                    money: 500,
                    roundNumber: 3,
                    upgrades: upgrades
                );
                Console.WriteLine();

                // Example 4: Load game data
                Console.WriteLine("--- Example 4: Load Game Data ---");
                SaveData saveData = sqlHandler.LoadGame();
                Console.WriteLine($"Player HP: {saveData.PlayerHP}");
                Console.WriteLine($"Money: {saveData.Money}");
                Console.WriteLine($"Round: {saveData.RoundNumber}");
                Console.WriteLine("Upgrades:");
                foreach (var upgrade in saveData.Upgrades)
                {
                    Console.WriteLine($"  - {upgrade.Key}: Level {upgrade.Value}");
                }
                Console.WriteLine();

                // Example 5: Print formatted save data
                Console.WriteLine("--- Example 5: Print Save Data ---");
                sqlHandler.PrintSaveData();
                Console.WriteLine();

                // Example 6: Reset save to defaults
                Console.WriteLine("--- Example 6: Reset Save ---");
                sqlHandler.ResetSave();
                sqlHandler.PrintSaveData();
                Console.WriteLine();
            }

            // Example 7: Multiple users with separate saves
            Console.WriteLine("--- Example 7: Multiple Users ---");

            // Register and login second user
            authHandler.RegisterUser("jane_smith", "anotherpass456");
            User? user2 = authHandler.AuthenticateUser("jane_smith", "anotherpass456");

            if (user2 != null)
            {
                sqlHandler.SetCurrentUser(user2);

                Dictionary<string, int> user2Upgrades = new Dictionary<string, int>
                {
                    { "Speed Upgrade", 3 }
                };

                sqlHandler.SaveGame(
                    playerHP: 100,
                    money: 1000,
                    roundNumber: 10,
                    upgrades: user2Upgrades
                );

                Console.WriteLine($"Saved data for {user2.Username}:");
                sqlHandler.PrintSaveData();
            }

            // Example 8: Change password
            Console.WriteLine("\n--- Example 8: Change Password ---");
            bool passwordChanged = authHandler.ChangePassword("john_doe", "securepass123", "newsecurepass456");
            if (passwordChanged)
            {
                Console.WriteLine("Password changed successfully!");

                // Try to login with new password
                User? userWithNewPassword = authHandler.AuthenticateUser("john_doe", "newsecurepass456");
                if (userWithNewPassword != null)
                {
                    Console.WriteLine("Successfully logged in with new password!");
                }
            }

            // Example 9: Invalid login attempt
            Console.WriteLine("\n--- Example 9: Invalid Login Attempt ---");
            User? invalidUser = authHandler.AuthenticateUser("john_doe", "wrongpassword");
            if (invalidUser == null)
            {
                Console.WriteLine("Login failed as expected with wrong password.");
            }

            // Example 10: Check if user exists
            Console.WriteLine("\n--- Example 10: Check User Existence ---");
            bool userExists = authHandler.UserExists("john_doe");
            Console.WriteLine($"User 'john_doe' exists: {userExists}");

            bool nonExistentUser = authHandler.UserExists("nonexistent_user");
            Console.WriteLine($"User 'nonexistent_user' exists: {nonExistentUser}");

            Console.WriteLine("\n=== Example Complete ===");
        }
    }
}
