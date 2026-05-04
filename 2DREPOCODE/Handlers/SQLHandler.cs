using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using _2DREPOCODE.Models;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles all database operations for saving and loading game data using SQLite.
    /// Manages user-specific player stats (HP, money, round) and upgrade information.
    /// </summary>
    public class SQLHandler
    {
        // Full path to the SQLite database file
        private readonly string databasePath;
        // Connection string used to connect to the database
        private readonly string connectionString;
        // Currently logged in user (null if no user is logged in)
        private User? currentUser;

        /// <summary>
        /// Initializes a new instance of the SQLHandler with the specified database file.
        /// </summary>
        /// <param name="databaseFileName">Name of the database file (default: "savefile.db").</param>
        public SQLHandler(string databaseFileName = "savefile.db")
        {
            // Build the full path to the database file in the application directory
            databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseFileName);
            // Create the SQLite connection string
            connectionString = $"Data Source={databasePath}";
            currentUser = null;
        }

        /// <summary>
        /// Sets the current user for this SQLHandler instance.
        /// All save/load operations will be associated with this user.
        /// </summary>
        /// <param name="user">The user to set as current.</param>
        public void SetCurrentUser(User user)
        {
            currentUser = user;
        }

        /// <summary>
        /// Gets the currently logged in user.
        /// </summary>
        /// <returns>The current user, or null if no user is logged in.</returns>
        public User? GetCurrentUser()
        {
            return currentUser;
        }

        /// <summary>
        /// Clears the current user (logs out).
        /// </summary>
        public void ClearCurrentUser()
        {
            currentUser = null;
        }

        /// <summary>
        /// Creates the database tables if they don't exist and initializes default save data.
        /// This method is safe to call multiple times - it only creates tables if missing.
        /// </summary>
        public void InitializeDatabase()
        {
            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Define the SaveData table structure for storing player stats
            // Now includes UserId to link saves to specific users
            string createSaveDataTable = @"
                CREATE TABLE IF NOT EXISTS SaveData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL UNIQUE,
                    PlayerHP INTEGER NOT NULL,
                    Money INTEGER NOT NULL,
                    RoundNumber INTEGER NOT NULL
                );
            ";

            // Define the PlayerUpgrades table structure for storing upgrade levels
            // Now includes UserId to link upgrades to specific users
            // Composite UNIQUE constraint prevents duplicate upgrades per user
            string createUpgradeTable = @"
                CREATE TABLE IF NOT EXISTS PlayerUpgrades (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    UpgradeName TEXT NOT NULL,
                    UpgradeLevel INTEGER NOT NULL,
                    UNIQUE(UserId, UpgradeName)
                );
            ";

            // Execute the table creation queries
            using SqliteCommand saveDataCommand = new SqliteCommand(createSaveDataTable, connection);
            saveDataCommand.ExecuteNonQuery();

            using SqliteCommand upgradeCommand = new SqliteCommand(createUpgradeTable, connection);
            upgradeCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates a default save for the specified user if one doesn't exist.
        /// Default values: HP=100, Money=0, Round=1
        /// </summary>
        /// <param name="connection">An open SQLite connection to use.</param>
        /// <param name="userId">The ID of the user to create a default save for.</param>
        private void CreateDefaultSaveIfMissing(SqliteConnection connection, int userId)
        {
            // Check if a save for this user already exists
            string checkQuery = "SELECT COUNT(*) FROM SaveData WHERE UserId = @UserId;";

            using SqliteCommand checkCommand = new SqliteCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@UserId", userId);
            long saveCount = (long)checkCommand.ExecuteScalar();

            // Only create default save if none exists for this user
            if (saveCount == 0)
            {
                // Insert the default save data for this user
                string insertDefaultSave = @"
                    INSERT INTO SaveData (UserId, PlayerHP, Money, RoundNumber)
                    VALUES (@UserId, 100, 0, 1);
                ";

                using SqliteCommand insertCommand = new SqliteCommand(insertDefaultSave, connection);
                insertCommand.Parameters.AddWithValue("@UserId", userId);
                insertCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves the current game state to the database for the logged-in user.
        /// Uses a transaction to ensure all data is saved atomically.
        /// </summary>
        /// <param name="playerHP">Current player health points.</param>
        /// <param name="money">Current amount of money.</param>
        /// <param name="roundNumber">Current round number.</param>
        /// <param name="upgrades">Dictionary of upgrade names and their levels.</param>
        public void SaveGame(int playerHP, int money, int roundNumber, Dictionary<string, int> upgrades)
        {
            // Check if a user is logged in
            if (currentUser == null)
            {
                Console.WriteLine("Cannot save game: No user logged in.");
                return;
            }

            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Use a transaction to ensure all data is saved together or rolled back on error
            using SqliteTransaction transaction = connection.BeginTransaction();

            try
            {
                // Ensure the user has a save record, create if missing
                CreateDefaultSaveIfMissing(connection, currentUser.UserId);

                // Update the save data for the current user
                string updateSaveQuery = @"
                    UPDATE SaveData
                    SET PlayerHP = @PlayerHP,
                        Money = @Money,
                        RoundNumber = @RoundNumber
                    WHERE UserId = @UserId;
                ";

                using SqliteCommand saveCommand = new SqliteCommand(updateSaveQuery, connection, transaction);
                // Use parameters to prevent SQL injection
                saveCommand.Parameters.AddWithValue("@PlayerHP", playerHP);
                saveCommand.Parameters.AddWithValue("@Money", money);
                saveCommand.Parameters.AddWithValue("@RoundNumber", roundNumber);
                saveCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
                saveCommand.ExecuteNonQuery();

                // Save each upgrade - insert new or update existing for this user
                foreach (KeyValuePair<string, int> upgrade in upgrades)
                {
                    // UPSERT query: insert if new, update if exists (based on UNIQUE UserId + UpgradeName)
                    string saveUpgradeQuery = @"
                        INSERT INTO PlayerUpgrades (UserId, UpgradeName, UpgradeLevel)
                        VALUES (@UserId, @UpgradeName, @UpgradeLevel)
                        ON CONFLICT(UserId, UpgradeName)
                        DO UPDATE SET UpgradeLevel = @UpgradeLevel;
                    ";

                    using SqliteCommand upgradeCommand = new SqliteCommand(saveUpgradeQuery, connection, transaction);
                    upgradeCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
                    upgradeCommand.Parameters.AddWithValue("@UpgradeName", upgrade.Key);
                    upgradeCommand.Parameters.AddWithValue("@UpgradeLevel", upgrade.Value);
                    upgradeCommand.ExecuteNonQuery();
                }

                // Commit all changes if everything succeeded
                transaction.Commit();

                Console.WriteLine($"Game saved successfully for user '{currentUser.Username}'.");
            }
            catch (Exception error)
            {
                // Roll back all changes if any error occurred
                transaction.Rollback();
                Console.WriteLine("Could not save game.");
                Console.WriteLine(error.Message);
            }
        }

        /// <summary>
        /// Loads the saved game data from the database for the logged-in user.
        /// Returns a SaveData object containing player stats and all upgrades.
        /// </summary>
        /// <returns>A SaveData object populated with the saved game state, or default values if no save exists.</returns>
        public SaveData LoadGame()
        {
            SaveData saveData = new SaveData();

            // Check if a user is logged in
            if (currentUser == null)
            {
                Console.WriteLine("Cannot load game: No user logged in. Returning default save data.");
                return saveData;
            }

            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Ensure the user has a save record
            CreateDefaultSaveIfMissing(connection, currentUser.UserId);

            // Load the save data for the current user
            string loadSaveQuery = @"
                SELECT PlayerHP, Money, RoundNumber
                FROM SaveData
                WHERE UserId = @UserId;
            ";

            using SqliteCommand saveCommand = new SqliteCommand(loadSaveQuery, connection);
            saveCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
            using SqliteDataReader saveReader = saveCommand.ExecuteReader();

            // Read the save data if it exists
            if (saveReader.Read())
            {
                // GetInt32(index) retrieves the value at the specified column index
                saveData.UserId = currentUser.UserId;
                saveData.PlayerHP = saveReader.GetInt32(0);
                saveData.Money = saveReader.GetInt32(1);
                saveData.RoundNumber = saveReader.GetInt32(2);
            }

            saveReader.Close();

            // Load all player upgrades for the current user
            string loadUpgradesQuery = @"
                SELECT UpgradeName, UpgradeLevel
                FROM PlayerUpgrades
                WHERE UserId = @UserId
                ORDER BY UpgradeName;
            ";

            using SqliteCommand upgradeCommand = new SqliteCommand(loadUpgradesQuery, connection);
            upgradeCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
            using SqliteDataReader upgradeReader = upgradeCommand.ExecuteReader();

            // Read all upgrade rows and add them to the dictionary
            while (upgradeReader.Read())
            {
                string upgradeName = upgradeReader.GetString(0);
                int upgradeLevel = upgradeReader.GetInt32(1);

                saveData.Upgrades[upgradeName] = upgradeLevel;
            }

            return saveData;
        }

        /// <summary>
        /// Loads and displays the current save data to the console for the logged-in user.
        /// Shows player stats and all purchased upgrades.
        /// </summary>
        public void PrintSaveData()
        {
            // Check if a user is logged in
            if (currentUser == null)
            {
                Console.WriteLine("Cannot print save data: No user logged in.");
                return;
            }

            // Load the current save data
            SaveData saveData = LoadGame();

            // Display the main save information
            Console.WriteLine("=== SAVE FILE ===");
            Console.WriteLine($"User: {currentUser.Username}");
            Console.WriteLine($"Player HP: {saveData.PlayerHP}");
            Console.WriteLine($"Money: {saveData.Money}");
            Console.WriteLine($"Round: {saveData.RoundNumber}");

            Console.WriteLine();
            Console.WriteLine("=== UPGRADES ===");

            // Check if any upgrades exist
            if (saveData.Upgrades.Count == 0)
            {
                Console.WriteLine("No upgrades saved.");
                return;
            }

            // Display each upgrade and its level
            foreach (KeyValuePair<string, int> upgrade in saveData.Upgrades)
            {
                Console.WriteLine($"{upgrade.Key}: Level {upgrade.Value}");
            }
        }

        /// <summary>
        /// Resets the save file to default values and removes all upgrades for the logged-in user.
        /// Uses a transaction to ensure complete reset or no changes on error.
        /// </summary>
        public void ResetSave()
        {
            // Check if a user is logged in
            if (currentUser == null)
            {
                Console.WriteLine("Cannot reset save: No user logged in.");
                return;
            }

            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            // Use a transaction to ensure both operations succeed together
            using SqliteTransaction transaction = connection.BeginTransaction();

            try
            {
                // Reset the save data to default values for the current user
                string resetSaveQuery = @"
                    UPDATE SaveData
                    SET PlayerHP = 100,
                        Money = 0,
                        RoundNumber = 1
                    WHERE UserId = @UserId;
                ";

                // Delete all player upgrades for the current user
                string deleteUpgradesQuery = "DELETE FROM PlayerUpgrades WHERE UserId = @UserId;";

                // Execute the reset query
                using SqliteCommand resetCommand = new SqliteCommand(resetSaveQuery, connection, transaction);
                resetCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
                resetCommand.ExecuteNonQuery();

                // Execute the delete upgrades query
                using SqliteCommand deleteCommand = new SqliteCommand(deleteUpgradesQuery, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@UserId", currentUser.UserId);
                deleteCommand.ExecuteNonQuery();

                // Commit both changes together
                transaction.Commit();

                Console.WriteLine($"Save file has been reset for user '{currentUser.Username}'.");
            }
            catch (Exception error)
            {
                // Roll back all changes if any error occurred
                transaction.Rollback();
                Console.WriteLine("Could not reset save file.");
                Console.WriteLine(error.Message);
            }
        }
    }
}
