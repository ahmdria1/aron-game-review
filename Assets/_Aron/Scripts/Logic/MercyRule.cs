// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    /// <summary>
    /// Dying over and over at the same checkpoint: from the third time on the hero comes back already wearing
    /// one golden bisht. Reaching a new checkpoint starts the count again.
    /// </summary>
    public class MercyRule
    {
        public const int DeathsBeforeHelp = 3;
        public int Deaths { get; private set; }
        /// <summary>Deaths before the bisht comes; 0 means never (hard).</summary>
        public int Threshold { get; set; } = DeathsBeforeHelp;
        private string checkpoint = "";

        public void Reached(string checkpointId)
        {
            checkpointId = checkpointId ?? "";
            if (checkpointId == checkpoint) return;
            checkpoint = checkpointId;
            Deaths = 0;
        }

        /// <returns>True when this respawn should come with a bisht on.</returns>
        public bool Died(string checkpointId)
        {
            Reached(checkpointId);
            Deaths++;
            return Threshold > 0 && Deaths >= Threshold;
        }
    }
}
