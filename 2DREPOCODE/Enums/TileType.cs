using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Enums
{
    /// <summary>
    /// Represents the different types of tiles in the procedurally generated facility.
    /// Used by MapHandler for dungeon generation and collision detection.
    /// </summary>
    public enum TileType
    {
        /// <summary>
        /// Empty walkable floor tile.
        /// </summary>
        Floor = 0,

        /// <summary>
        /// Solid wall tile - blocks movement and line of sight.
        /// </summary>
        Wall = 1,

        /// <summary>
        /// Entrance/exit door tile - connects rooms.
        /// </summary>
        Door = 2,

        /// <summary>
        /// Extraction truck tile - where players deposit items.
        /// </summary>
        ExtractionPoint = 3,

        /// <summary>
        /// Spawn point for players at mission start.
        /// </summary>
        PlayerSpawn = 4,

        /// <summary>
        /// Shelf tile - where loot items spawn.
        /// </summary>
        Shelf = 5,

        /// <summary>
        /// Vent tile - small passages accessible only when shrunk.
        /// </summary>
        Vent = 6,

        /// <summary>
        /// Obstacle tile - small objects players can hide behind when shrunk.
        /// </summary>
        Obstacle = 7,

        /// <summary>
        /// Darkness tile - requires flashlight to see.
        /// </summary>
        DarkZone = 8,

        /// <summary>
        /// Hazard tile - deals damage when stepped on (broken glass, etc.).
        /// </summary>
        Hazard = 9
    }
}
