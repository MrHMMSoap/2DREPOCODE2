using _2DREPOCODE.Enums;
using _2DREPOCODE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Handlers
{
    /// <summary>
    /// Handles player movement, sprinting, stamina management, and shrinking mechanics.
    /// Responsibility: Axel
    /// </summary>
    public class PlayerMovementHandler
    {
        // === Constants ===
        /// <summary>
        /// Base movement speed in tiles per second.
        /// </summary>
        private const float BASE_MOVE_SPEED = 5f;

        /// <summary>
        /// Sprint speed multiplier (applied to base speed).
        /// </summary>
        private const float SPRINT_MULTIPLIER = 1.5f;

        /// <summary>
        /// Movement speed when shrunk (slower for balance).
        /// </summary>
        private const float SHRUNK_SPEED_MULTIPLIER = 0.8f;

        /// <summary>
        /// Movement speed when dragging heavy items.
        /// </summary>
        private const float DRAGGING_SPEED_MULTIPLIER = 0.6f;

        // === Dependencies ===
        private MapHandler? mapHandler;

        /// <summary>
        /// Initializes the PlayerMovementHandler.
        /// </summary>
        public PlayerMovementHandler()
        {
            mapHandler = null;
        }

        /// <summary>
        /// Sets the MapHandler dependency for collision detection.
        /// </summary>
        public void SetMapHandler(MapHandler handler)
        {
            mapHandler = handler;
        }

        // === Main Movement Method ===

        /// <summary>
        /// Attempts to move the player in the specified direction.
        /// Handles collision detection, stamina drain, and noise generation.
        /// Returns true if movement was successful.
        /// </summary>
        /// <param name="player">The player to move.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="isSprinting">Whether the player is attempting to sprint.</param>
        /// <param name="deltaTime">Time since last update (for stamina calculations).</param>
        public bool MovePlayer(Player player, Direction direction, bool isSprinting, float deltaTime)
        {
            // Can't move if stunned or dead
            if (player.IsStunned || !player.IsAlive)
            {
                return false;
            }

            // Can't move if no direction specified
            if (direction == Direction.None)
            {
                return false;
            }

            // Calculate target position
            int targetX = player.X;
            int targetY = player.Y;

            switch (direction)
            {
                case Direction.North:
                    targetY--;
                    break;
                case Direction.East:
                    targetX++;
                    break;
                case Direction.South:
                    targetY++;
                    break;
                case Direction.West:
                    targetX--;
                    break;
            }

            // Check if movement is valid
            if (!CanMoveTo(player, targetX, targetY))
            {
                return false;
            }

            // Handle sprinting
            if (isSprinting && player.CanSprint)
            {
                // Drain stamina
                if (!player.DrainStamina(player.StaminaDrainRate * deltaTime))
                {
                    // Not enough stamina, fall back to walking
                    isSprinting = false;
                }
                else
                {
                    player.IsSprinting = true;
                    // Sprinting generates more noise (for Huntsman AI)
                    player.UpdateNoiseLevel(2);
                }
            }
            else
            {
                player.IsSprinting = false;
                // Normal walking generates some noise
                player.UpdateNoiseLevel(1);
            }

            // Calculate actual movement speed
            float moveSpeed = CalculateMoveSpeed(player, isSprinting);

            // In a grid-based system, we just update position
            // In Unity with smooth movement, you'd interpolate based on moveSpeed and deltaTime
            player.X = targetX;
            player.Y = targetY;
            player.Facing = direction;

            return true;
        }

        /// <summary>
        /// Updates player stamina regeneration when not moving.
        /// Should be called every frame.
        /// </summary>
        public void UpdateStamina(Player player, float deltaTime)
        {
            // Regenerate stamina when not sprinting
            if (!player.IsSprinting)
            {
                player.RegenerateStamina(deltaTime);
            }

            // Reset noise level when idle
            if (!player.IsSprinting)
            {
                player.UpdateNoiseLevel(0);
            }
        }

        // === Shrinking Mechanic ===

        /// <summary>
        /// Toggles the player's shrinking ability.
        /// When shrunk: slower movement, can access vents, can hide behind obstacles.
        /// </summary>
        public void ToggleShrink(Player player)
        {
            player.ToggleShrink();

            if (player.IsShrunken)
            {
                Console.WriteLine($"{player.Name} has shrunk down!");
            }
            else
            {
                Console.WriteLine($"{player.Name} has returned to normal size.");
            }
        }

        // === Helper Methods ===

        /// <summary>
        /// Checks if the player can move to the specified tile.
        /// Handles collision with walls, doors, vents (when not shrunk), etc.
        /// </summary>
        private bool CanMoveTo(Player player, int targetX, int targetY)
        {
            // If no MapHandler set, allow movement (for testing)
            if (mapHandler == null)
            {
                return true;
            }

            // Get the target tile
            MapTile? tile = mapHandler.GetTile(targetX, targetY);
            if (tile == null)
            {
                // Out of bounds
                return false;
            }

            // Check if tile is walkable
            if (!tile.IsWalkable)
            {
                return false;
            }

            // Special case: Vents can only be accessed when shrunk
            if (tile.Type == TileType.Vent && !player.IsShrunken)
            {
                return false;
            }

            // Special case: Doors must be open
            if (tile.Type == TileType.Door && !tile.IsDoorOpen)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the effective movement speed based on player state.
        /// Considers: sprinting, shrinking, carrying items.
        /// </summary>
        private float CalculateMoveSpeed(Player player, bool isSprinting)
        {
            float speed = BASE_MOVE_SPEED * player.MovementSpeed;

            // Apply sprint multiplier
            if (isSprinting)
            {
                speed *= SPRINT_MULTIPLIER * player.SprintSpeedMultiplier;
            }

            // Shrinking makes you slower
            if (player.IsShrunken)
            {
                speed *= SHRUNK_SPEED_MULTIPLIER;
            }

            // Holding items slows you down
            if (player.IsHoldingItem)
            {
                speed *= DRAGGING_SPEED_MULTIPLIER;

                // Heavy items slow you down more
                if (player.TetheredItem != null && player.TetheredItem.Mass > 10f)
                {
                    speed *= 0.7f; // Additional 30% slow for heavy items
                }
            }

            return speed;
        }

        /// <summary>
        /// Calculates the Manhattan distance between two points.
        /// Useful for pathfinding and distance checks.
        /// </summary>
        public int GetDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
        }

        /// <summary>
        /// Returns the direction from (x1,y1) to (x2,y2).
        /// Used for simple pathfinding.
        /// </summary>
        public Direction GetDirectionTo(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;

            // Prioritize horizontal movement
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? Direction.East : Direction.West;
            }
            else if (Math.Abs(dy) > 0)
            {
                return dy > 0 ? Direction.South : Direction.North;
            }

            return Direction.None;
        }

        /// <summary>
        /// Moves player directly to a position (for teleportation, respawning, etc.).
        /// Bypasses collision checks - use with caution!
        /// </summary>
        public void TeleportPlayer(Player player, int x, int y)
        {
            player.X = x;
            player.Y = y;
            Console.WriteLine($"{player.Name} teleported to ({x}, {y})");
        }

        /// <summary>
        /// Pushes the player in a direction (for knockback effects).
        /// </summary>
        public void KnockbackPlayer(Player player, Direction direction, int distance)
        {
            for (int i = 0; i < distance; i++)
            {
                int targetX = player.X;
                int targetY = player.Y;

                switch (direction)
                {
                    case Direction.North: targetY--; break;
                    case Direction.East: targetX++; break;
                    case Direction.South: targetY++; break;
                    case Direction.West: targetX--; break;
                }

                // Stop if hit a wall
                if (!CanMoveTo(player, targetX, targetY))
                {
                    break;
                }

                player.X = targetX;
                player.Y = targetY;
            }

            Console.WriteLine($"{player.Name} knocked back {distance} tiles!");
        }

        /// <summary>
        /// Applies a stun effect to the player.
        /// </summary>
        public void StunPlayer(Player player, float duration)
        {
            player.IsStunned = true;
            player.StunDuration = duration;
            player.IsSprinting = false;
            Console.WriteLine($"{player.Name} is stunned for {duration} seconds!");
        }

        /// <summary>
        /// Updates stun duration and removes stun when expired.
        /// Call this every frame.
        /// </summary>
        public void UpdateStun(Player player, float deltaTime)
        {
            if (player.IsStunned)
            {
                player.StunDuration -= deltaTime;
                if (player.StunDuration <= 0)
                {
                    player.IsStunned = false;
                    player.StunDuration = 0;
                    Console.WriteLine($"{player.Name} is no longer stunned.");
                }
            }
        }

        /// <summary>
        /// Gets all valid adjacent positions from a given position.
        /// Used for pathfinding and area checks.
        /// </summary>
        public List<(int x, int y)> GetAdjacentPositions(int x, int y)
        {
            List<(int x, int y)> adjacent = new List<(int x, int y)>
            {
                (x, y - 1), // North
                (x + 1, y), // East
                (x, y + 1), // South
                (x - 1, y)  // West
            };

            return adjacent;
        }
    }
}
