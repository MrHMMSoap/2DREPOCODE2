using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Enums
{
    /// <summary>
    /// Represents the four cardinal directions for 2D grid-based movement.
    /// Used by PlayerMovementHandler and MonsterMovementHandler for pathfinding.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Upward movement (decreases Y in grid coordinates).
        /// </summary>
        North = 0,

        /// <summary>
        /// Rightward movement (increases X in grid coordinates).
        /// </summary>
        East = 1,

        /// <summary>
        /// Downward movement (increases Y in grid coordinates).
        /// </summary>
        South = 2,

        /// <summary>
        /// Leftward movement (decreases X in grid coordinates).
        /// </summary>
        West = 3,

        /// <summary>
        /// No movement/stationary.
        /// </summary>
        None = 4
    }
}
