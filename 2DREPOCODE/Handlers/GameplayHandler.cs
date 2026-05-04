using _2DREPOCODE.Enums;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Core gameplay loop handler. Manages game state transitions, mission flow,
    /// extraction logic, round progression, and quota tracking.
    /// Responsibility: Axel
    /// </summary>
    public class GameplayHandler
    {
        // === Game State ===
        /// <summary>
        /// Current game state (menu, mission, results, etc.).
        /// </summary>
        public GameState CurrentState { get; private set; }

        /// <summary>
        /// Current round number.
        /// </summary>
        public int CurrentRound { get; private set; }

        /// <summary>
        /// Current SURPLUS quota that must be extracted.
        /// </summary>
        public int CurrentQuota { get; private set; }

        /// <summary>
        /// Total SURPLUS extracted in the current mission.
        /// </summary>
        public int ExtractedValue { get; private set; }

        /// <summary>
        /// Mission time remaining (in seconds).
        /// </summary>
        public float MissionTimeRemaining { get; private set; }

        /// <summary>
        /// Default mission duration (5 minutes).
        /// </summary>
        private const float DEFAULT_MISSION_DURATION = 300f;

        // === Handlers ===
        private PlayerHandler playerHandler;
        private CartHandler cartHandler;
        private MapHandler mapHandler;
        private PlayerMovementHandler movementHandler;

        // === Extraction Tracking ===
        private List<Item> extractedItems;
        private bool quotaMet;

        /// <summary>
        /// Initializes the GameplayHandler with required dependencies.
        /// </summary>
        public GameplayHandler(PlayerHandler playerHandler, CartHandler cartHandler, 
                               MapHandler mapHandler, PlayerMovementHandler movementHandler)
        {
            this.playerHandler = playerHandler;
            this.cartHandler = cartHandler;
            this.mapHandler = mapHandler;
            this.movementHandler = movementHandler;

            CurrentState = GameState.MainMenu;
            CurrentRound = 1;
            CurrentQuota = 100; // Starting quota
            ExtractedValue = 0;
            quotaMet = false;
            extractedItems = new List<Item>();

            Console.WriteLine("GameplayHandler initialized!");
        }

        // === Game State Transitions ===

        /// <summary>
        /// Starts a new game from the main menu.
        /// </summary>
        public void StartNewGame()
        {
            Console.WriteLine("=== STARTING NEW GAME ===");

            CurrentRound = 1;
            CurrentQuota = 100;
            ExtractedValue = 0;
            quotaMet = false;

            // Reset all systems
            playerHandler.ResetAllPlayers();
            cartHandler.ResetAllCarts();

            // Transition to service station (upgrade/prep phase)
            TransitionTo(GameState.ServiceStation);
        }

        /// <summary>
        /// Starts a mission (enters the facility).
        /// </summary>
        public void StartMission()
        {
            Console.WriteLine($"=== STARTING MISSION: ROUND {CurrentRound} ===");
            Console.WriteLine($"QUOTA: Extract {CurrentQuota} SURPLUS to proceed.");

            // Generate new map
            mapHandler.GenerateMap();

            // Spawn players at spawn points
            List<(int x, int y)> spawnPoints = mapHandler.GetPlayerSpawnPoints();
            List<Player> players = playerHandler.GetAllPlayers();

            for (int i = 0; i < players.Count && i < spawnPoints.Count; i++)
            {
                players[i].X = spawnPoints[i].x;
                players[i].Y = spawnPoints[i].y;
                players[i].Facing = Direction.South;

                // Fully restore player for mission start
                playerHandler.FullyRestorePlayer(players[i]);

                Console.WriteLine($"{players[i].Name} spawned at ({players[i].X}, {players[i].Y})");
            }

            // Reset extraction tracking
            ExtractedValue = 0;
            extractedItems.Clear();
            quotaMet = false;

            // Set mission timer
            MissionTimeRemaining = DEFAULT_MISSION_DURATION;

            // Transition to in-mission state
            TransitionTo(GameState.InMission);
        }

        /// <summary>
        /// Updates the mission gameplay (called every frame/tick).
        /// </summary>
        public void UpdateMission(float deltaTime)
        {
            if (CurrentState != GameState.InMission)
            {
                return;
            }

            // Update mission timer
            MissionTimeRemaining -= deltaTime;

            // Update player stamina regeneration
            foreach (Player player in playerHandler.GetAllPlayers())
            {
                movementHandler.UpdateStamina(player, deltaTime);
            }

            // Update carts
            foreach (Cart cart in cartHandler.GetAllCarts())
            {
                cartHandler.UpdateCart(cart, deltaTime);
            }

            // Check if time ran out
            if (MissionTimeRemaining <= 0)
            {
                OnMissionTimeExpired();
            }

            // Check if all players are dead
            if (playerHandler.AreAllPlayersDead())
            {
                OnAllPlayersDead();
            }
        }

        /// <summary>
        /// Attempts to extract items at the extraction point.
        /// Called when a player brings items/cart to the extraction zone.
        /// </summary>
        public bool TryExtractItems(List<Item> items)
        {
            if (CurrentState != GameState.InMission)
            {
                Console.WriteLine("Cannot extract items outside of a mission!");
                return false;
            }

            int extractionValue = 0;

            foreach (Item item in items)
            {
                extractionValue += item.CurrentValue;
                item.Extract();
                extractedItems.Add(item);
                Console.WriteLine($"Extracted: {item.Name} ({item.CurrentValue} SURPLUS)");
            }

            ExtractedValue += extractionValue;

            Console.WriteLine($"💰 Total Extracted: {ExtractedValue}/{CurrentQuota} SURPLUS");

            // Check if quota is met
            if (ExtractedValue >= CurrentQuota && !quotaMet)
            {
                quotaMet = true;
                Console.WriteLine("✅ QUOTA MET! You can now evacuate safely.");
            }

            return true;
        }

        /// <summary>
        /// Attempts to extract all items from a cart at the extraction point.
        /// </summary>
        public bool TryExtractCart(Cart cart)
        {
            if (CurrentState != GameState.InMission)
            {
                Console.WriteLine("Cannot extract cart outside of a mission!");
                return false;
            }

            List<Item> cartItems = cartHandler.UnloadCart(cart);
            return TryExtractItems(cartItems);
        }

        /// <summary>
        /// Evacuates all players from the mission (returns to service station).
        /// </summary>
        public void Evacuate()
        {
            if (CurrentState != GameState.InMission)
            {
                Console.WriteLine("Cannot evacuate outside of a mission!");
                return;
            }

            Console.WriteLine("=== EVACUATING FACILITY ===");

            // Check if players are at extraction point
            (int extractX, int extractY) = mapHandler.GetExtractionPoint();
            List<Player> players = playerHandler.GetAllPlayers();

            int playersAtExtraction = 0;
            foreach (Player player in players)
            {
                if (player.IsAlive && player.X == extractX && player.Y == extractY)
                {
                    playersAtExtraction++;
                }
            }

            Console.WriteLine($"{playersAtExtraction}/{players.Count} players at extraction point.");

            // End mission and show results
            EndMission();
        }

        /// <summary>
        /// Ends the current mission and transitions to results.
        /// </summary>
        private void EndMission()
        {
            Console.WriteLine("=== MISSION COMPLETE ===");

            // Calculate performance
            bool success = quotaMet;
            int aliveCount = 0;

            foreach (Player player in playerHandler.GetAllPlayers())
            {
                if (player.IsAlive)
                {
                    aliveCount++;
                }
            }

            Console.WriteLine($"Quota Met: {(success ? "YES" : "NO")}");
            Console.WriteLine($"Extracted Value: {ExtractedValue}/{CurrentQuota} SURPLUS");
            Console.WriteLine($"Players Alive: {aliveCount}/{playerHandler.GetAllPlayers().Count}");
            Console.WriteLine($"Time Remaining: {MissionTimeRemaining:F1}s");

            // Transition to results
            TransitionTo(GameState.Results);
        }

        /// <summary>
        /// Handles mission failure due to time expiration.
        /// </summary>
        private void OnMissionTimeExpired()
        {
            Console.WriteLine("⏰ TIME'S UP! Mission failed!");
            EndMission();
        }

        /// <summary>
        /// Handles mission failure due to all players dying.
        /// </summary>
        private void OnAllPlayersDead()
        {
            Console.WriteLine("💀 ALL PLAYERS DEAD! Mission failed!");
            EndMission();
        }

        /// <summary>
        /// Shows the results screen and progresses to next round or game over.
        /// </summary>
        public void ShowResults()
        {
            if (CurrentState != GameState.Results)
            {
                return;
            }

            Console.WriteLine("=== MISSION RESULTS ===");
            Console.WriteLine($"Round {CurrentRound} Complete!");
            Console.WriteLine($"Extracted: {ExtractedValue} SURPLUS");
            Console.WriteLine($"Quota: {CurrentQuota} SURPLUS");

            if (quotaMet)
            {
                Console.WriteLine("✅ SUCCESS! Proceeding to next round...");
                AdvanceToNextRound();
            }
            else
            {
                Console.WriteLine("❌ FAILURE! Quota not met.");
                TransitionTo(GameState.GameOver);
            }
        }

        /// <summary>
        /// Advances to the next round with increased difficulty.
        /// </summary>
        private void AdvanceToNextRound()
        {
            CurrentRound++;

            // Increase quota (exponential scaling)
            CurrentQuota = (int)(100 * Math.Pow(1.5, CurrentRound - 1));

            Console.WriteLine($"=== ROUND {CurrentRound} ===");
            Console.WriteLine($"New Quota: {CurrentQuota} SURPLUS");

            // Return to service station for upgrades
            TransitionTo(GameState.ServiceStation);
        }

        /// <summary>
        /// Transitions to the service station (upgrade/prep phase).
        /// </summary>
        public void EnterServiceStation()
        {
            Console.WriteLine("=== SERVICE STATION ===");
            Console.WriteLine("Upgrade your equipment and prepare for the next mission.");
            Console.WriteLine($"Current SURPLUS: {ExtractedValue}");

            // Here you would integrate with upgrade shop, loadout selection, etc.

            TransitionTo(GameState.ServiceStation);
        }

        /// <summary>
        /// Returns to the main menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Console.WriteLine("Returning to Main Menu...");

            // Clear all game data
            playerHandler.ClearAllPlayers();
            cartHandler.ClearAllCarts();

            CurrentRound = 1;
            CurrentQuota = 100;
            ExtractedValue = 0;
            extractedItems.Clear();
            quotaMet = false;

            TransitionTo(GameState.MainMenu);
        }

        // === State Machine ===

        /// <summary>
        /// Transitions to a new game state.
        /// </summary>
        private void TransitionTo(GameState newState)
        {
            Console.WriteLine($"[State] {CurrentState} -> {newState}");
            CurrentState = newState;

            // Trigger state-specific logic
            OnStateEntered(newState);
        }

        /// <summary>
        /// Called when entering a new state.
        /// </summary>
        private void OnStateEntered(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    // Show main menu UI
                    break;

                case GameState.ServiceStation:
                    // Show upgrade shop / loadout selection
                    break;

                case GameState.Briefing:
                    // Show mission briefing
                    break;

                case GameState.InMission:
                    // Start mission timer, spawn enemies, etc.
                    break;

                case GameState.Results:
                    // Calculate rewards, show stats
                    ShowResults();
                    break;

                case GameState.GameOver:
                    Console.WriteLine("=== GAME OVER ===");
                    break;
            }
        }

        // === Utility Methods ===

        /// <summary>
        /// Checks if the quota has been met.
        /// </summary>
        public bool IsQuotaMet()
        {
            return quotaMet;
        }

        /// <summary>
        /// Gets the current mission progress as a percentage.
        /// </summary>
        public float GetMissionProgress()
        {
            if (CurrentQuota == 0) return 0f;
            return (float)ExtractedValue / CurrentQuota;
        }

        /// <summary>
        /// Gets the formatted time remaining string.
        /// </summary>
        public string GetTimeRemainingFormatted()
        {
            int minutes = (int)(MissionTimeRemaining / 60);
            int seconds = (int)(MissionTimeRemaining % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Gets a formatted status summary of the current gameplay state.
        /// </summary>
        public string GetGameplayStatus()
        {
            return $"=== GAMEPLAY STATUS ===\n" +
                   $"State: {CurrentState}\n" +
                   $"Round: {CurrentRound}\n" +
                   $"Quota: {ExtractedValue}/{CurrentQuota} SURPLUS ({GetMissionProgress() * 100:F1}%)\n" +
                   $"Time Remaining: {GetTimeRemainingFormatted()}\n" +
                   $"Players Alive: {playerHandler.GetLivingPlayerCount()}/{playerHandler.GetAllPlayers().Count}\n" +
                   $"Active Carts: {cartHandler.GetAllCarts().Count}\n" +
                   $"Extracted Items: {extractedItems.Count}";
        }

        /// <summary>
        /// Prints the current gameplay status to console.
        /// </summary>
        public void PrintGameplayStatus()
        {
            Console.WriteLine(GetGameplayStatus());
        }
    }
}
