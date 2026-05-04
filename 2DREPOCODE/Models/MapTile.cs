using _2DREPOCODE.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Models
{
    /// <summary>
    /// Represents a single tile in the procedurally generated facility map.
    /// Used by MapHandler for dungeon generation and collision detection.
    /// </summary>
    public class MapTile
    {
        // === Position ===
        /// <summary>
        /// X coordinate on the grid.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y coordinate on the grid.
        /// </summary>
        public int Y { get; set; }

        // === Tile Properties ===
        /// <summary>
        /// The type of this tile (floor, wall, door, etc.).
        /// Determines collision, visibility, and gameplay behavior.
        /// </summary>
        public TileType Type { get; set; }

        /// <summary>
        /// Whether this tile blocks movement.
        /// Walls and closed doors block movement; floors and open doors allow it.
        /// </summary>
        public bool IsWalkable { get; set; }

        /// <summary>
        /// Whether this tile blocks line of sight (for enemy AI and visibility).
        /// Walls block sight; floors and obstacles may not.
        /// </summary>
        public bool BlocksLineOfSight { get; set; }

        // === Door Properties (if applicable) ===
        /// <summary>
        /// If this is a door tile, whether it's currently open.
        /// </summary>
        public bool IsDoorOpen { get; set; }

        /// <summary>
        /// If this is a door tile, whether it requires a key or is locked.
        /// </summary>
        public bool IsDoorLocked { get; set; }

        // === Lighting & Visibility ===
        /// <summary>
        /// Light level of this tile (0.0 = complete darkness, 1.0 = fully lit).
        /// Used for 2D lighting system and flashlight mechanics.
        /// </summary>
        public float LightLevel { get; set; }

        /// <summary>
        /// Whether this tile has been explored by the player (for fog of war).
        /// </summary>
        public bool IsExplored { get; set; }

        /// <summary>
        /// Whether this tile is currently visible to the player.
        /// </summary>
        public bool IsVisible { get; set; }

        // === Special Tiles ===
        /// <summary>
        /// If this is a shelf tile, whether it has been looted already.
        /// </summary>
        public bool HasBeenLooted { get; set; }

        /// <summary>
        /// If this is a hazard tile, the damage dealt per step.
        /// </summary>
        public int HazardDamage { get; set; }

        // === Sprite/Rendering Info ===
        /// <summary>
        /// The sprite index or texture ID for rendering in Unity.
        /// Used to display the correct pixel art for this tile type.
        /// </summary>
        public int SpriteIndex { get; set; }

        /// <summary>
        /// Rotation of the sprite in degrees (for variety in procedural generation).
        /// </summary>
        public int SpriteRotation { get; set; }

        // === Constructor ===
        /// <summary>
        /// Creates a basic floor tile at the origin.
        /// </summary>
        public MapTile()
        {
            X = 0;
            Y = 0;
            Type = TileType.Floor;
            IsWalkable = true;
            BlocksLineOfSight = false;
            IsDoorOpen = false;
            IsDoorLocked = false;
            LightLevel = 0.5f; // Dimly lit by default
            IsExplored = false;
            IsVisible = false;
            HasBeenLooted = false;
            HazardDamage = 0;
            SpriteIndex = 0;
            SpriteRotation = 0;
        }

        /// <summary>
        /// Creates a tile with specific position and type.
        /// </summary>
        public MapTile(int x, int y, TileType type)
        {
            X = x;
            Y = y;
            Type = type;

            // Set properties based on tile type
            switch (type)
            {
                case TileType.Floor:
                    IsWalkable = true;
                    BlocksLineOfSight = false;
                    LightLevel = 0.5f;
                    break;

                case TileType.Wall:
                    IsWalkable = false;
                    BlocksLineOfSight = true;
                    LightLevel = 0.3f;
                    break;

                case TileType.Door:
                    IsWalkable = false; // Until opened
                    BlocksLineOfSight = true; // Until opened
                    IsDoorOpen = false;
                    IsDoorLocked = false;
                    LightLevel = 0.4f;
                    break;

                case TileType.ExtractionPoint:
                    IsWalkable = true;
                    BlocksLineOfSight = false;
                    LightLevel = 1.0f; // Well-lit extraction zone
                    break;

                case TileType.PlayerSpawn:
                    IsWalkable = true;
                    BlocksLineOfSight = false;
                    LightLevel = 0.8f;
                    break;

                case TileType.Shelf:
                    IsWalkable = true;
                    BlocksLineOfSight = false;
                    HasBeenLooted = false;
                    LightLevel = 0.5f;
                    break;

                case TileType.Vent:
                    IsWalkable = true; // Only when player is shrunken
                    BlocksLineOfSight = false;
                    LightLevel = 0.2f; // Dark inside vents
                    break;

                case TileType.Obstacle:
                    IsWalkable = true;
                    BlocksLineOfSight = false; // Can hide behind when shrunk
                    LightLevel = 0.5f;
                    break;

                case TileType.DarkZone:
                    IsWalkable = true;
                    BlocksLineOfSight = false;
                    LightLevel = 0.0f; // Complete darkness, requires flashlight
                    break;

                case TileType.Hazard:
                    IsWalkable = true; // Can walk but takes damage
                    BlocksLineOfSight = false;
                    HazardDamage = 5; // Damage per step
                    LightLevel = 0.4f;
                    break;
            }

            IsExplored = false;
            IsVisible = false;
            SpriteIndex = (int)type; // Default: use enum value as sprite index
            SpriteRotation = 0;
        }

        // === Helper Methods ===

        /// <summary>
        /// Opens a door tile (if applicable).
        /// Makes it walkable and transparent to line of sight.
        /// </summary>
        public void OpenDoor()
        {
            if (Type == TileType.Door && !IsDoorLocked)
            {
                IsDoorOpen = true;
                IsWalkable = true;
                BlocksLineOfSight = false;
            }
        }

        /// <summary>
        /// Closes a door tile (if applicable).
        /// Makes it block movement and line of sight.
        /// </summary>
        public void CloseDoor()
        {
            if (Type == TileType.Door)
            {
                IsDoorOpen = false;
                IsWalkable = false;
                BlocksLineOfSight = true;
            }
        }

        /// <summary>
        /// Marks this shelf as looted.
        /// </summary>
        public void LootShelf()
        {
            if (Type == TileType.Shelf)
            {
                HasBeenLooted = true;
            }
        }

        /// <summary>
        /// Updates the light level of this tile.
        /// Used for dynamic lighting and flashlight effects.
        /// </summary>
        public void SetLightLevel(float level)
        {
            LightLevel = Math.Clamp(level, 0.0f, 1.0f);
        }

        /// <summary>
        /// Marks this tile as explored (for fog of war).
        /// </summary>
        public void Explore()
        {
            IsExplored = true;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"MapTile({X},{Y}) Type:{Type} Walkable:{IsWalkable} Light:{LightLevel:F2}";
        }
    }
}
