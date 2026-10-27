// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Animation
{
    /// <summary>One bored idle: the Animator trigger it fires (Bored_0..N) and what it shows.</summary>
    [Serializable]
    public class BoredClip
    {
        public string id;
        /// <summary>Index N of the Animator trigger "Bored_N".</summary>
        public int trigger;
        public string noteEn = "";
        /// <summary>Optional line Noura or a boss says with it (Arabic).</summary>
        public string lineAr = "";
        public float seconds = 3f;
    }

    [Serializable]
    public class BoredActor
    {
        /// <summary>aron, noura, falcon, oryx, camel or boss.</summary>
        public string actor;
        public BoredClip[] clips = Array.Empty<BoredClip>();
    }

    /// <summary>Resources/Config/bored_idles.json.</summary>
    [Serializable]
    public class BoredIdleFile
    {
        public float minWait = 6f, maxWait = 8f;
        public BoredActor[] actors = Array.Empty<BoredActor>();

        public static readonly string[] Actors = { "aron", "noura", "falcon", "oryx", "camel", "boss" };

        public BoredClip[] For(string actor)
        {
            foreach (var a in actors) if (a.actor == actor) return a.clips;
            return Array.Empty<BoredClip>();
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (minWait < 2f || maxWait < minWait || maxWait > 30f) e.Add("bored wait must be 2..30 s with min <= max");
            var seen = new HashSet<string>();
            foreach (var a in actors)
            {
                if (Array.IndexOf(Actors, a.actor) < 0) e.Add("unknown bored actor " + a.actor);
                if (!seen.Add(a.actor ?? "")) e.Add("bored actor listed twice: " + a.actor);
                if (a.clips.Length == 0) e.Add(a.actor + ": no bored idles");
                var triggers = new HashSet<int>();
                var ids = new HashSet<string>();
                foreach (var c in a.clips)
                {
                    if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) e.Add(a.actor + ": missing or duplicate clip id");
                    if (c.trigger < 0 || !triggers.Add(c.trigger)) e.Add(a.actor + " " + c.id + ": trigger index must be unique and >= 0");
                    if (c.seconds <= 0.3f || c.seconds > 10f) e.Add(a.actor + " " + c.id + ": 0.3..10 s");
                }
            }
            foreach (var a in Actors) if (!seen.Contains(a)) e.Add("no bored idles for " + a);
            return e;
        }

        public static string Trigger(int n) => "Bored_" + n;
    }

    /// <summary>
    /// The bored-idle clock for one character. Feed it time and input; after a randomized wait without input
    /// it picks a clip (never the same one twice in a row, skipping clips the rig doesn't have yet).
    /// Any input stops it at once and restarts the wait.
    /// </summary>
    public class BoredIdle
    {
        private readonly List<BoredClip> clips = new List<BoredClip>();
        private readonly float minWait, maxWait;
        private readonly Random rnd;
        private readonly Func<BoredClip, bool> available;
        private float idle, wait, playLeft;

        public BoredClip Playing { get; private set; }
        public BoredClip Last { get; private set; }
        public float Wait => wait;
        public bool IsPlaying => Playing != null;

        /// <param name="available">Whether the rig can play a clip (Animator has the trigger and a clip). Null means all.</param>
        public BoredIdle(IEnumerable<BoredClip> clips, float minWait, float maxWait, int seed, Func<BoredClip, bool> available = null)
        {
            if (clips != null) this.clips.AddRange(clips);
            this.minWait = Math.Max(0.5f, minWait);
            this.maxWait = Math.Max(this.minWait, maxWait);
            rnd = new Random(seed);
            this.available = available;
            NewWait();
        }

        /// <summary>Adds a clip (a costume's country idle) at run time.</summary>
        public void Add(BoredClip c) { if (c != null) clips.Add(c); }

        private void NewWait() { wait = minWait + (float)rnd.NextDouble() * (maxWait - minWait); idle = 0f; }

        /// <summary>Any input or movement: stop the clip now and wait again from zero.</summary>
        public void Interrupt()
        {
            Playing = null;
            playLeft = 0f;
            NewWait();
        }

        /// <summary>Advances time. Returns the clip to start this frame, or null.</summary>
        public BoredClip Tick(float dt, bool hadInput)
        {
            if (hadInput) { Interrupt(); return null; }
            if (Playing != null)
            {
                playLeft -= dt;
                if (playLeft <= 0f) { Playing = null; NewWait(); }
                return null;
            }
            idle += dt;
            if (idle < wait) return null;
            var pick = Pick();
            if (pick == null) { NewWait(); return null; }
            Playing = Last = pick;
            playLeft = pick.seconds;
            return pick;
        }

        /// <summary>A random playable clip, never the last one played unless it is the only playable clip.</summary>
        public BoredClip Pick()
        {
            var pool = new List<BoredClip>();
            foreach (var c in clips) if (available == null || available(c)) pool.Add(c);
            if (pool.Count > 1 && Last != null) pool.Remove(Last);
            if (pool.Count == 0) return null;
            return pool[rnd.Next(pool.Count)];
        }
    }
}
