using _2DREPOCODE.Enums;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles player stats, health, damage, upgrades, and special abilities.
    /// Manages the Semibot's core gameplay mechanics.
    /// Responsibility: Axel
    /// </summary>
    public class PlayerHandler
    {
        // === Constants ===
        /// <summary>
        /// Base maximum HP for a new player.
        /// </summary>
        private const int BASE_MAX_HP = 100;

        /// <summary>
        /// Base maximum stamina.
        /// </summary>
        private const float BASE_MAX_STAMINA = 100f;

        /// <summary>
        /// Base carrying strength.
        /// </summary>
        private const float BASE_CARRYING_STRENGTH = 1.0f;

        /// <summary>
        /// Respawn invincibility duration in seconds.
        /// </summary>
        private const float RESPAWN_INVINCIBILITY_TIME = 3f;

        // === Player Registry ===
        private Dictionary<int, Player> activePlayers;
        private int nextPlayerId;

        /// <summary>
        /// Initializes the PlayerHandler.
        /// </summary>
        public PlayerHandler()
        {
            activePlayers = new Dictionary<int, Player>();
            nextPlayerId = 1;
        }

        // === Player Creation & Management ===

        /// <summary>
        /// Creates a new player (Semibot) with default stats.
        /// </summary>
        /// <param name="playerName">Display name for the player.</param>
        /// <returns>The newly created player.</returns>
        public Player CreatePlayer(string playerName)
        {
            Player player = new Player(nextPlayerId, playerName, BASE_MAX_HP, BASE_MAX_STAMINA);
            activePlayers.Add(nextPlayerId, player);
            nextPlayerId++;

            Console.WriteLine($"Semibot '{playerName}' has been created! (ID: {player.PlayerId})");
            return player;
        }

        /// <summary>
        /// Creates a player with custom starting stats (for testing/balancing).
        /// </summary>
        public Player CreateCustomPlayer(string playerName, int maxHP, float maxStamina, float carryingStrength)
        {
            Player player = new Player(nextPlayerId, playerName, maxHP, maxStamina);
            player.CarryingStrength = carryingStrength;
            activePlayers.Add(nextPlayerId, player);
            nextPlayerId++;

            Console.WriteLine($"Custom Semibot '{playerName}' created! HP:{maxHP} Stamina:{maxStamina}");
            return player;
        }

        /// <summary>
        /// Retrieves a player by their ID.
        /// </summary>
        public Player? GetPlayer(int playerId)
        {
            if (activePlayers.TryGetValue(playerId, out Player? player))
            {
                return player;
            }
            return null;
        }

        /// <summary>
        /// Gets all active players.
        /// </summary>
        public List<Player> GetAllPlayers()
        {
            return new List<Player>(activePlayers.Values);
        }

        /// <summary>
        /// Removes a player from the active registry.
        /// </summary>
        public void RemovePlayer(int playerId)
        {
            if (activePlayers.Remove(playerId))
            {
                Console.WriteLine($"Player {playerId} has been removed.");
            }
        }

        // === Health Management ===

        /// <summary>
        /// Applies damage to a player.
        /// Handles death logic and returns true if player died.
        /// </summary>
        /// <param name="player">The player taking damage.</param>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="damageSource">Description of damage source (for logging).</param>
        public bool DamagePlayer(Player player, int damage, string damageSource = "Unknown")
        {
            if (!player.IsAlive)
            {
                return false; // Already dead
            }

            // Apply damage
            bool died = player.TakeDamage(damage);

            Console.WriteLine($"{player.Name} took {damage} damage from {damageSource}! HP: {player.CurrentHP}/{player.MaxHP}");

            if (died)
            {
                OnPlayerDeath(player);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Heals a player by the specified amount.
        /// </summary>
        public void HealPlayer(Player player, int healAmount)
        {
            int previousHP = player.CurrentHP;
            player.Heal(healAmount);
            int actualHealed = player.CurrentHP - previousHP;

            Console.WriteLine($"{player.Name} healed for {actualHealed} HP! HP: {player.CurrentHP}/{player.MaxHP}");
        }

        /// <summary>
        /// Fully restores a player's health and stamina.
        /// </summary>
        public void FullyRestorePlayer(Player player)
        {
            player.CurrentHP = player.MaxHP;
            player.CurrentStamina = player.MaxStamina;
            player.IsStunned = false;
            player.StunDuration = 0;

            Console.WriteLine($"{player.Name} has been fully restored!");
        }

        /// <summary>
        /// Handles player death logic.
        /// </summary>
        private void OnPlayerDeath(Player player)
        {
            Console.WriteLine($"☠ {player.Name} HAS DIED! ☠");

            // Drop held item
            if (player.IsHoldingItem && player.TetheredItem != null)
            {
                player.TetheredItem.Drop();
                player.TetheredItem = null;
                player.IsHoldingItem = false;
            }

            // Stop sprinting
            player.IsSprinting = false;

            // Reset shrink state
            player.IsShrunken = false;

            // In a roguelike, death might trigger game over or respawn logic
            // This will be handled by GameplayHandler
        }

        /// <summary>
        /// Respawns a player at a spawn point.
        /// </summary>
        public void RespawnPlayer(Player player, int spawnX, int spawnY)
        {
            player.CurrentHP = player.MaxHP;
            player.CurrentStamina = player.MaxStamina;
            player.X = spawnX;
            player.Y = spawnY;
            player.IsStunned = false;
            player.StunDuration = 0;

            Console.WriteLine($"{player.Name} has respawned at ({spawnX}, {spawnY})!");
        }

        // === Upgrade System (Service Station) ===

        /// <summary>
        /// Upgrades a player's max HP using SURPLUS currency.
        /// </summary>
        /// <param name="player">The player to upgrade.</param>
        /// <param name="hpIncrease">Amount of HP to add to max.</param>
        public void UpgradeMaxHP(Player player, int hpIncrease)
        {
            player.MaxHP += hpIncrease;
            player.CurrentHP += hpIncrease; // Also heal by that amount
            Console.WriteLine($"{player.Name}'s Max HP increased by {hpIncrease}! New Max HP: {player.MaxHP}");
        }

        /// <summary>
        /// Upgrades a player's max stamina.
        /// </summary>
        public void UpgradeMaxStamina(Player player, float staminaIncrease)
        {
            player.MaxStamina += staminaIncrease;
            player.CurrentStamina += staminaIncrease;
            Console.WriteLine($"{player.Name}'s Max Stamina increased by {staminaIncrease}! New Max: {player.MaxStamina}");
        }

        /// <summary>
        /// Upgrades a player's carrying strength (affects item dragging speed).
        /// </summary>
        public void UpgradeCarryingStrength(Player player, float strengthIncrease)
        {
            player.CarryingStrength += strengthIncrease;
            Console.WriteLine($"{player.Name}'s Carrying Strength increased by {strengthIncrease:F2}! New: {player.CarryingStrength:F2}");
        }

        /// <summary>
        /// Upgrades a player's movement speed.
        /// </summary>
        public void UpgradeMovementSpeed(Player player, float speedIncrease)
        {
            player.MovementSpeed += speedIncrease;
            Console.WriteLine($"{player.Name}'s Movement Speed increased by {speedIncrease:F2}! New: {player.MovementSpeed:F2}");
        }

        /// <summary>
        /// Upgrades a player's sprint speed multiplier.
        /// </summary>
        public void UpgradeSprintSpeed(Player player, float sprintIncrease)
        {
            player.SprintSpeedMultiplier += sprintIncrease;
            Console.WriteLine($"{player.Name}'s Sprint Multiplier increased by {sprintIncrease:F2}! New: {player.SprintSpeedMultiplier:F2}");
        }

        /// <summary>
        /// Upgrades stamina regeneration rate.
        /// </summary>
        public void UpgradeStaminaRegen(Player player, float regenIncrease)
        {
            player.StaminaRegenRate += regenIncrease;
            Console.WriteLine($"{player.Name}'s Stamina Regen increased by {regenIncrease:F2}/s! New: {player.StaminaRegenRate:F2}/s");
        }

        // === Equipment Management ===

        /// <summary>
        /// Equips a weapon to the player.
        /// </summary>
        public void EquipWeapon(Player player, string weaponName)
        {
            player.EquippedWeapon = weaponName;
            Console.WriteLine($"{player.Name} equipped: {weaponName}");
        }

        /// <summary>
        /// Unequips the player's current weapon.
        /// </summary>
        public void UnequipWeapon(Player player)
        {
            if (player.EquippedWeapon != null)
            {
                Console.WriteLine($"{player.Name} unequipped: {player.EquippedWeapon}");
                player.EquippedWeapon = null;
            }
        }

        // === Item Tethering (Grabber Tool) ===

        /// <summary>
        /// Attaches an item to the player's Grabber Tool (tethers it).
        /// </summary>
        public bool TetherItem(Player player, Item item)
        {
            if (player.IsHoldingItem)
            {
                Console.WriteLine($"{player.Name} is already holding an item!");
                return false;
            }

            if (item.IsBeingHeld || item.IsInCart)
            {
                Console.WriteLine($"Item {item.Name} is already held or in a cart!");
                return false;
            }

            player.IsHoldingItem = true;
            player.TetheredItem = item;
            item.PickUp(player.PlayerId);

            Console.WriteLine($"{player.Name} tethered {item.Name} (Mass: {item.Mass}, Value: {item.CurrentValue})");
            return true;
        }

        /// <summary>
        /// Detaches the item from the player's Grabber Tool.
        /// </summary>
        public void ReleaseItem(Player player)
        {
            if (!player.IsHoldingItem || player.TetheredItem == null)
            {
                return;
            }

            player.TetheredItem.Drop();
            Console.WriteLine($"{player.Name} released {player.TetheredItem.Name}");

            player.TetheredItem = null;
            player.IsHoldingItem = false;
        }

        // === Special Damage Types ===

        /// <summary>
        /// Applies damage over time from hazardous items.
        /// Call this every frame when player is holding a hazardous item.
        /// </summary>
        public void ApplyHazardDamage(Player player, float deltaTime)
        {
            if (player.TetheredItem != null && player.TetheredItem.Type == ItemType.Hazardous)
            {
                int damage = (int)(player.TetheredItem.HazardDamagePerSecond * deltaTime);
                if (damage > 0)
                {
                    DamagePlayer(player, damage, "Hazardous Item");
                }
            }
        }

        /// <summary>
        /// Applies environmental hazard damage (like stepping on broken glass).
        /// </summary>
        public void ApplyEnvironmentalDamage(Player player, MapTile tile)
        {
            if (tile.Type == TileType.Hazard && tile.HazardDamage > 0)
            {
                DamagePlayer(player, tile.HazardDamage, "Environmental Hazard");
            }
        }

        // === Player Status ===

        /// <summary>
        /// Gets a formatted status string for a player (for UI/debugging).
        /// </summary>
        public string GetPlayerStatus(Player player)
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine($"=== {player.Name} (ID: {player.PlayerId}) ===");
            status.AppendLine($"HP: {player.CurrentHP}/{player.MaxHP}");
            status.AppendLine($"Stamina: {player.CurrentStamina:F1}/{player.MaxStamina}");
            status.AppendLine($"Position: ({player.X}, {player.Y})");
            status.AppendLine($"Facing: {player.Facing}");
            status.AppendLine($"Status: {(player.IsAlive ? "Alive" : "Dead")}");

            if (player.IsShrunken)
                status.AppendLine("⚡ SHRUNK");
            if (player.IsSprinting)
                status.AppendLine("🏃 SPRINTING");
            if (player.IsStunned)
                status.AppendLine($"😵 STUNNED ({player.StunDuration:F1}s)");
            if (player.IsHoldingItem && player.TetheredItem != null)
                status.AppendLine($"📦 Holding: {player.TetheredItem.Name}");
            if (player.EquippedWeapon != null)
                status.AppendLine($"🔫 Weapon: {player.EquippedWeapon}");

            status.AppendLine($"\nStats:");
            status.AppendLine($"  Carrying Strength: {player.CarryingStrength:F2}");
            status.AppendLine($"  Move Speed: {player.MovementSpeed:F2}");
            status.AppendLine($"  Sprint Multiplier: {player.SprintSpeedMultiplier:F2}");
            status.AppendLine($"  Noise Level: {player.NoiseLevel}");

            return status.ToString();
        }

        /// <summary>
        /// Prints player status to console.
        /// </summary>
        public void PrintPlayerStatus(Player player)
        {
            Console.WriteLine(GetPlayerStatus(player));
        }

        /// <summary>
        /// Checks if all players are dead (for game over condition).
        /// </summary>
        public bool AreAllPlayersDead()
        {
            foreach (Player player in activePlayers.Values)
            {
                if (player.IsAlive)
                {
                    return false;
                }
            }
            return activePlayers.Count > 0; // Returns true only if there are players and all are dead
        }

        /// <summary>
        /// Gets the number of living players.
        /// </summary>
        public int GetLivingPlayerCount()
        {
            int count = 0;
            foreach (Player player in activePlayers.Values)
            {
                if (player.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Resets all players to their starting state (for new run).
        /// </summary>
        public void ResetAllPlayers()
        {
            foreach (Player player in activePlayers.Values)
            {
                player.CurrentHP = player.MaxHP;
                player.CurrentStamina = player.MaxStamina;
                player.IsShrunken = false;
                player.IsSprinting = false;
                player.IsStunned = false;
                player.StunDuration = 0;
                player.IsHoldingItem = false;
                player.TetheredItem = null;
                player.NoiseLevel = 0;
            }

            Console.WriteLine("All players have been reset!");
        }

        /// <summary>
        /// Clears all players (for returning to main menu).
        /// </summary>
        public void ClearAllPlayers()
        {
            activePlayers.Clear();
            nextPlayerId = 1;
            Console.WriteLine("All players cleared.");
        }
    }
}
