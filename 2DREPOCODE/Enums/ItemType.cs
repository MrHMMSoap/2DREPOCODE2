using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Enums
{
    /// <summary>
    /// Represents the different types of items that can be found and extracted.
    /// Used by ItemHandler to determine physics properties and value calculations.
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// Standard loot - moderate value, moderate weight.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Fragile item - high value but shatters on high-velocity collision.
        /// Examples: Plates, crystals, glass objects.
        /// </summary>
        Fragile = 1,

        /// <summary>
        /// Heavy item - very high value but requires multiple players to move efficiently.
        /// Examples: Safes, statues, machinery.
        /// </summary>
        Heavy = 2,

        /// <summary>
        /// Lightweight item - low value, easy to transport.
        /// Examples: Papers, small trinkets.
        /// </summary>
        Lightweight = 3,

        /// <summary>
        /// Hazardous item - high value but causes damage over time when held.
        /// Examples: Radioactive materials, cursed artifacts.
        /// </summary>
        Hazardous = 4,

        /// <summary>
        /// Bizarre artifact - extremely high value, may have special properties.
        /// Examples: Alien technology, anomalous objects.
        /// </summary>
        Artifact = 5
    }
}
