using System.Collections.Generic;

namespace _2DREPOCODE.Models
{
    /// <summary>
    /// Represents game save data for a specific user.
    /// </summary>
    public class SaveData
    {
        public int UserId { get; set; }
        public int PlayerHP { get; set; }
        public int Money { get; set; }
        public int RoundNumber { get; set; }
        public Dictionary<string, int> Upgrades { get; set; }

        public SaveData()
        {
            UserId = 0;
            PlayerHP = 100;
            Money = 0;
            RoundNumber = 1;
            Upgrades = new Dictionary<string, int>();
        }
    }
}
