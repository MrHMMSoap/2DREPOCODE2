using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles execution and validation of unit tests for the SQLHandler save/load functionality.
    /// </summary>
    public class UnitTestHandler
    {
        // Track the number of tests that passed
        private int passedTests = 0;
        // Track the number of tests that failed
        private int failedTests = 0;

        // Use a separate test database to avoid corrupting the main save file
        private readonly string testDatabaseName = "test_savefile.db";
        // Use a separate test user database
        private readonly string testUserDatabaseName = "test_users.db";

        /// <summary>
        /// Executes all unit tests and displays the results summary.
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("=== UNIT TESTS STARTED ===");
            Console.WriteLine();

            // Run all test cases
            TestUserAuthentication();
            TestDefaultSaveCreation();
            TestSaveAndLoadBasicData();
            TestSaveAndLoadUpgrades();
            TestResetSave();
            TestUpgradeOverwrite();
            TestMultipleUsers();

            // Display test results summary
            Console.WriteLine();
            Console.WriteLine("=== UNIT TESTS FINISHED ===");
            Console.WriteLine($"Passed: {passedTests}");
            Console.WriteLine($"Failed: {failedTests}");
        }

        /// <summary>
        /// Creates a fresh SQLHandler instance with a clean test database.
        /// Deletes any existing test database to ensure each test starts with a clean slate.
        /// </summary>
        /// <returns>A new SQLHandler instance connected to a fresh test database.</returns>
        private SQLHandler CreateFreshTestSQLHandler()
        {
            // Get the full path to the test database file
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, testDatabaseName);

            // Delete the existing test database if it exists to start fresh
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            // Create a new SQLHandler and initialize a clean database
            SQLHandler sqlHandler = new SQLHandler(testDatabaseName);
            sqlHandler.InitializeDatabase();

            return sqlHandler;
        }

        /// <summary>
        /// Creates a fresh AuthHandler instance with a clean test user database.
        /// </summary>
        /// <returns>A new AuthHandler instance connected to a fresh test database.</returns>
        private AuthHandler CreateFreshTestAuthHandler()
        {
            // Get the full path to the test user database file
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, testUserDatabaseName);

            // Delete the existing test user database if it exists to start fresh
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            // Create a new AuthHandler and initialize a clean database
            AuthHandler authHandler = new AuthHandler(testUserDatabaseName);
            authHandler.InitializeDatabase();

            return authHandler;
        }

        /// <summary>
        /// Creates a test user and returns the User object.
        /// </summary>
        /// <param name="authHandler">The AuthHandler to use.</param>
        /// <param name="username">The username to create.</param>
        /// <param name="password">The password for the user.</param>
        /// <returns>A User object for the created user.</returns>
        private User CreateTestUser(AuthHandler authHandler, string username, string password)
        {
            authHandler.RegisterUser(username, password);
            User? user = authHandler.AuthenticateUser(username, password);
            return user!;
        }

        /// <summary>
        /// Tests user registration and authentication functionality.
        /// </summary>
        private void TestUserAuthentication()
        {
            AuthHandler authHandler = CreateFreshTestAuthHandler();

            // Test user registration
            bool registerResult = authHandler.RegisterUser("testuser", "password123");
            AssertTrue(registerResult, "User registration should succeed");

            // Test duplicate username prevention
            bool duplicateResult = authHandler.RegisterUser("testuser", "different");
            AssertTrue(!duplicateResult, "Duplicate username should be rejected");

            // Test valid authentication
            User? user = authHandler.AuthenticateUser("testuser", "password123");
            AssertTrue(user != null, "Valid credentials should authenticate");
            AssertEqual("testuser", user?.Username ?? "", "Authenticated user should have correct username");

            // Test invalid authentication
            User? invalidUser = authHandler.AuthenticateUser("testuser", "wrongpassword");
            AssertTrue(invalidUser == null, "Invalid password should reject authentication");
        }

        /// <summary>
        /// Asserts that the expected value equals the actual value.
        /// Updates test counters and logs the result to the console.
        /// </summary>
        /// <typeparam name="T">The type of values being compared.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="testName">A descriptive name for the test assertion.</param>
        private void AssertEqual<T>(T expected, T actual, string testName)
        {
            // Compare the expected and actual values
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                passedTests++;
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                // Log detailed failure information
                failedTests++;
                Console.WriteLine($"[FAIL] {testName}");
                Console.WriteLine($"       Expected: {expected}");
                Console.WriteLine($"       Actual:   {actual}");
            }
        }

        private void AssertTrue(bool condition, string testName)
        {
            if (condition)
            {
                passedTests++;
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                failedTests++;
                Console.WriteLine($"[FAIL] {testName}");
            }
        }

        /// <summary>
        /// Tests that a newly created database contains the correct default save data values.
        /// Verifies initial HP, money, round number, and that no upgrades exist.
        /// </summary>
        private void TestDefaultSaveCreation()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create and set a test user
            User testUser = CreateTestUser(authHandler, "defaulttest", "password123");
            sqlHandler.SetCurrentUser(testUser);

            // Load the default save data
            SaveData saveData = sqlHandler.LoadGame();

            // Verify all default values are correct
            AssertEqual(100, saveData.PlayerHP, "Default save should start with 100 HP");
            AssertEqual(0, saveData.Money, "Default save should start with 0 money");
            AssertEqual(1, saveData.RoundNumber, "Default save should start on round 1");
            AssertEqual(0, saveData.Upgrades.Count, "Default save should have no upgrades");
        }

        /// <summary>
        /// Tests that basic save data (HP, money, round number) can be saved and loaded correctly.
        /// </summary>
        private void TestSaveAndLoadBasicData()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create and set a test user
            User testUser = CreateTestUser(authHandler, "basictest", "password123");
            sqlHandler.SetCurrentUser(testUser);

            // Create an empty upgrades dictionary
            Dictionary<string, int> upgrades = new Dictionary<string, int>();

            // Save game data with specific values
            sqlHandler.SaveGame(
                playerHP: 75,
                money: 1200,
                roundNumber: 4,
                upgrades: upgrades
            );

            // Load the saved data
            SaveData saveData = sqlHandler.LoadGame();

            // Verify all values were saved and loaded correctly
            AssertEqual(75, saveData.PlayerHP, "Saved HP should load correctly");
            AssertEqual(1200, saveData.Money, "Saved money should load correctly");
            AssertEqual(4, saveData.RoundNumber, "Saved round should load correctly");
        }

        /// <summary>
        /// Tests that player upgrades can be saved and loaded correctly.
        /// Verifies that upgrade names and their levels are preserved.
        /// </summary>
        private void TestSaveAndLoadUpgrades()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create and set a test user
            User testUser = CreateTestUser(authHandler, "upgradetest", "password123");
            sqlHandler.SetCurrentUser(testUser);

            // Create a dictionary with multiple upgrades at different levels
            Dictionary<string, int> upgrades = new Dictionary<string, int>
            {
                { "Health Upgrade", 2 },
                { "Speed Upgrade", 1 },
                { "Strength Upgrade", 3 }
            };

            sqlHandler.SaveGame(
                playerHP: 90,
                money: 500,
                roundNumber: 2,
                upgrades: upgrades
            );

            SaveData saveData = sqlHandler.LoadGame();

            AssertEqual(3, saveData.Upgrades.Count, "Saved upgrades count should load correctly");
            AssertEqual(2, saveData.Upgrades["Health Upgrade"], "Health Upgrade level should load correctly");
            AssertEqual(1, saveData.Upgrades["Speed Upgrade"], "Speed Upgrade level should load correctly");
            AssertEqual(3, saveData.Upgrades["Strength Upgrade"], "Strength Upgrade level should load correctly");
        }

        /// <summary>
        /// Tests that the ResetSave method correctly restores all values to their defaults.
        /// Verifies that HP, money, round number, and all upgrades are reset.
        /// </summary>
        private void TestResetSave()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create and set a test user
            User testUser = CreateTestUser(authHandler, "resettest", "password123");
            sqlHandler.SetCurrentUser(testUser);

            // Create some upgrades
            Dictionary<string, int> upgrades = new Dictionary<string, int>
            {
                { "Health Upgrade", 2 },
                { "Speed Upgrade", 1 }
            };

            sqlHandler.SaveGame(
                playerHP: 40,
                money: 999,
                roundNumber: 7,
                upgrades: upgrades
            );

            sqlHandler.ResetSave();

            SaveData saveData = sqlHandler.LoadGame();

            AssertEqual(100, saveData.PlayerHP, "Reset save should restore HP to 100");
            AssertEqual(0, saveData.Money, "Reset save should restore money to 0");
            AssertEqual(1, saveData.RoundNumber, "Reset save should restore round to 1");
            AssertEqual(0, saveData.Upgrades.Count, "Reset save should remove all upgrades");
        }

        /// <summary>
        /// Tests that saving an upgrade with the same name but different level correctly updates
        /// the existing upgrade rather than creating a duplicate entry.
        /// </summary>
        private void TestUpgradeOverwrite()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create and set a test user
            User testUser = CreateTestUser(authHandler, "overwritetest", "password123");
            sqlHandler.SetCurrentUser(testUser);

            // Save a game with Health Upgrade at level 1
            Dictionary<string, int> upgradesLevelOne = new Dictionary<string, int>
            {
                { "Health Upgrade", 1 }
            };

            sqlHandler.SaveGame(
                playerHP: 100,
                money: 300,
                roundNumber: 1,
                upgrades: upgradesLevelOne
            );

            // Save a game with the same upgrade but at level 3
            Dictionary<string, int> upgradesLevelThree = new Dictionary<string, int>
            {
                { "Health Upgrade", 3 }
            };

            sqlHandler.SaveGame(
                playerHP: 100,
                money: 100,
                roundNumber: 2,
                upgrades: upgradesLevelThree
            );

            SaveData saveData = sqlHandler.LoadGame();

            // Verify that the upgrade was updated, not duplicated
            AssertEqual(1, saveData.Upgrades.Count, "Duplicate upgrade should not create extra rows");
            AssertEqual(3, saveData.Upgrades["Health Upgrade"], "Upgrade level should be overwritten correctly");
        }

        /// <summary>
        /// Tests that multiple users can have separate save files without interfering with each other.
        /// </summary>
        private void TestMultipleUsers()
        {
            // Create a fresh database and auth handler
            AuthHandler authHandler = CreateFreshTestAuthHandler();
            SQLHandler sqlHandler = CreateFreshTestSQLHandler();

            // Create two test users
            User user1 = CreateTestUser(authHandler, "player1", "pass123");
            User user2 = CreateTestUser(authHandler, "player2", "pass456");

            // Save data for user 1
            sqlHandler.SetCurrentUser(user1);
            Dictionary<string, int> user1Upgrades = new Dictionary<string, int>
            {
                { "Health Upgrade", 2 }
            };
            sqlHandler.SaveGame(50, 100, 3, user1Upgrades);

            // Save data for user 2
            sqlHandler.SetCurrentUser(user2);
            Dictionary<string, int> user2Upgrades = new Dictionary<string, int>
            {
                { "Speed Upgrade", 1 }
            };
            sqlHandler.SaveGame(75, 200, 5, user2Upgrades);

            // Load and verify user 1's data
            sqlHandler.SetCurrentUser(user1);
            SaveData user1Data = sqlHandler.LoadGame();
            AssertEqual(50, user1Data.PlayerHP, "User 1 should have their own HP value");
            AssertEqual(100, user1Data.Money, "User 1 should have their own money value");
            AssertEqual(3, user1Data.RoundNumber, "User 1 should have their own round number");
            AssertTrue(user1Data.Upgrades.ContainsKey("Health Upgrade"), "User 1 should have Health Upgrade");
            AssertTrue(!user1Data.Upgrades.ContainsKey("Speed Upgrade"), "User 1 should not have User 2's upgrades");

            // Load and verify user 2's data
            sqlHandler.SetCurrentUser(user2);
            SaveData user2Data = sqlHandler.LoadGame();
            AssertEqual(75, user2Data.PlayerHP, "User 2 should have their own HP value");
            AssertEqual(200, user2Data.Money, "User 2 should have their own money value");
            AssertEqual(5, user2Data.RoundNumber, "User 2 should have their own round number");
            AssertTrue(user2Data.Upgrades.ContainsKey("Speed Upgrade"), "User 2 should have Speed Upgrade");
            AssertTrue(!user2Data.Upgrades.ContainsKey("Health Upgrade"), "User 2 should not have User 1's upgrades");
        }
    }
}
