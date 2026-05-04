using System;
using System.Collections.Generic;
using System.Text;

namespace _2DREPOCODE.Enums
{
    /// <summary>
    /// Represents the different states of the game flow.
    /// Used by GameplayHandler to manage the core loop.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Main menu state - player can start game or quit.
        /// </summary>
        MainMenu = 0,

        /// <summary>
        /// Service Station state - between runs, buy upgrades with SURPLUS.
        /// </summary>
        ServiceStation = 1,

        /// <summary>
        /// Briefing state - shows quota and mission objective before deploying.
        /// </summary>
        Briefing = 2,

        /// <summary>
        /// Active gameplay - player is inside the facility scavenging.
        /// </summary>
        InMission = 3,

        /// <summary>
        /// Extraction phase - player is at the truck depositing items.
        /// </summary>
        Extracting = 4,

        /// <summary>
        /// Mission results - shows quota met/failed, SURPLUS earned.
        /// </summary>
        Results = 5,

        /// <summary>
        /// Game Over - player died or failed quota, run resets.
        /// </summary>
        GameOver = 6,

        /// <summary>
        /// Paused state - gameplay frozen, pause menu visible.
        /// </summary>
        Paused = 7
    }
}
