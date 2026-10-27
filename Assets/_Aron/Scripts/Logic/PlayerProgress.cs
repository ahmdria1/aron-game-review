// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    /// <summary>Lives and dates bookkeeping for a single level run.</summary>
    public class PlayerProgress
    {
        public int Lives { get; private set; }
        public int Dates { get; private set; }
        public int DatesPerExtraLife { get; }
        /// <summary>How far through the current hundred, for the counter.</summary>
        public int ToNextLife => DatesPerExtraLife - Dates;
        public bool IsGameOver => Lives <= 0;

        public PlayerProgress(int startLives, int datesPerExtraLife)
        {
            if (startLives < 1) throw new ArgumentOutOfRangeException(nameof(startLives));
            if (datesPerExtraLife < 1) throw new ArgumentOutOfRangeException(nameof(datesPerExtraLife));
            Lives = startLives;
            DatesPerExtraLife = datesPerExtraLife;
        }

        /// <returns>Number of extra lives earned.</returns>
        public int AddDates(int amount)
        {
            if (amount <= 0) return 0;
            Dates += amount;
            int earned = Dates / DatesPerExtraLife;
            Dates %= DatesPerExtraLife;
            Lives += earned;
            return earned;
        }

        public void AddLife() => Lives++;

        /// <returns>True when this was the last life.</returns>
        public bool LoseLife()
        {
            if (Lives > 0) Lives--;
            return IsGameOver;
        }
    }
}
