// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    /// <summary>Boxes in a stage and how many of them got broken, for the end screen.</summary>
    public class BoxTally
    {
        public int Total { get; private set; }
        public int Broken { get; private set; }
        /// <summary>Golden (bisht) boxes broken this run; all of a stage's golden boxes is its hidden-collectible star.</summary>
        public int BishtBroken { get; private set; }
        public int Left => Math.Max(0, Total - Broken);
        public bool AllBroken => Total > 0 && Broken >= Total;

        public void SetTotal(int total)
        {
            Total = Math.Max(0, total);
            if (Broken > Total) Total = Broken;
        }

        public void Break(bool bisht = false)
        {
            Broken++;
            if (bisht) BishtBroken++;
            // A box nobody counted (dropped in by a script) still counts, rather than going past the total.
            if (Broken > Total) Total = Broken;
        }

        /// <summary>"Boxes: 37 / 42 — 5 left" with whatever template the language uses.</summary>
        public string Line(string template) => string.Format(template, Broken, Total, Left);
    }
}
