// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Emotions
{
    /// <summary>What a reaction asks for. Every field is optional; whatever the rig or the scene lacks is skipped.</summary>
    [Serializable]
    public class EmotionCue
    {
        /// <summary>The state: fear, scream, cry, recover, sadness, joy, surprise, anger, embarrassment, relief, affection, hunger, worry, cheer.</summary>
        public string state;
        /// <summary>Face blendshapes and weights (0..1), e.g. "Eyes_Wide=1".</summary>
        public string[] blendshapes = Array.Empty<string>();
        /// <summary>Bone poses used when the blendshapes are missing (animals mostly): "ears=back", "tail=wag", "head=down".</summary>
        public string[] bones = Array.Empty<string>();
        /// <summary>Animator trigger on the body layer.</summary>
        public string anim = "";
        /// <summary>A hero line id in Dialogue/HeroLines.json, or a species sound in Audio/Sfx.</summary>
        public string bark = "", sound = "";
        /// <summary>Comic effect: tears, sweat, blush, stars, exclaim, steam, hearts, music_notes.</summary>
        public string vfx = "";
        /// <summary>Key in the haptics table.</summary>
        public string haptic = "";
        public float seconds = 1f;
        /// <summary>Seconds before the same state can play again.</summary>
        public float cooldown = 3f;
        public int priority = 1;
    }

    /// <summary>One character's or species' reactions. Resources/Config/emotions.json.</summary>
    [Serializable]
    public class EmotionProfile
    {
        /// <summary>aron, noura, or an animal species id (oryx, falcon, camel, horse, giraffe...).</summary>
        public string who;
        public bool animal;
        public EmotionCue[] cues = Array.Empty<EmotionCue>();

        public EmotionCue Cue(string state)
        {
            foreach (var c in cues) if (c.state == state) return c;
            return null;
        }
    }

    [Serializable]
    public class EmotionFile
    {
        public const float MaxSequenceSeconds = 2.5f;
        public static readonly string[] HeroStates =
        {
            "scream", "cry", "recover", "sadness", "joy", "surprise", "anger", "embarrassment", "relief", "cheer", "worry",
        };
        public static readonly string[] AnimalStates = { "fear", "sadness", "joy", "anger", "surprise", "affection", "hunger" };
        public static readonly string[] Blendshapes =
        {
            "Brows_Up", "Brows_Down", "Eyes_Wide", "Eyes_Shut", "Mouth_Scream", "Mouth_Frown", "Mouth_Smile", "Mouth_Pout", "Cheeks_Blush", "Tears",
        };
        public static readonly string[] Vfx = { "", "tears", "sweat", "blush", "stars", "exclaim", "steam", "hearts", "music_notes" };

        public EmotionProfile[] profiles = Array.Empty<EmotionProfile>();

        public EmotionProfile For(string who)
        {
            foreach (var p in profiles) if (p.who == who) return p;
            return null;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            var seen = new HashSet<string>();
            foreach (var p in profiles)
            {
                if (string.IsNullOrEmpty(p.who) || !seen.Add(p.who)) e.Add("emotion profile missing or duplicate: " + p.who);
                foreach (var s in p.animal ? AnimalStates : HeroStates)
                    if (p.Cue(s) == null) e.Add(p.who + ": no " + s + " reaction");
                foreach (var c in p.cues)
                {
                    string w = p.who + " " + c.state;
                    if (c.seconds <= 0f || c.seconds > MaxSequenceSeconds) e.Add(w + ": 0..2.5 s, reactions never hold up play");
                    if (c.cooldown < 0f) e.Add(w + ": cooldown");
                    if (Array.IndexOf(Vfx, c.vfx ?? "") < 0) e.Add(w + ": unknown effect " + c.vfx);
                    foreach (var b in c.blendshapes)
                    {
                        int eq = b.IndexOf('=');
                        string name = eq > 0 ? b.Substring(0, eq) : b;
                        if (Array.IndexOf(Blendshapes, name) < 0) e.Add(w + ": unknown blendshape " + name);
                    }
                    if (p.animal && c.bones.Length == 0) e.Add(w + ": animals need a bone pose fallback");
                    if (p.animal && string.IsNullOrEmpty(c.sound)) e.Add(w + ": animals need a species sound");
                }
                if (!p.animal)
                {
                    var scream = p.Cue("scream"); var cry = p.Cue("cry"); var rec = p.Cue("recover");
                    if (scream != null && cry != null && rec != null && scream.seconds + cry.seconds + rec.seconds > MaxSequenceSeconds + 0.001f)
                        e.Add(p.who + ": scream, cry and recover together must stay under 2.5 s");
                }
            }
            return e;
        }
    }

    /// <summary>What to show this frame, already reduced to what the rig has.</summary>
    public class EmotionOutput
    {
        public EmotionCue cue;
        public readonly List<KeyValuePair<string, float>> blendshapes = new List<KeyValuePair<string, float>>();
        public readonly List<string> bones = new List<string>();
        public bool usedFallback;
    }

    /// <summary>
    /// Plays reactions for one character. A new reaction replaces the current one only at equal or higher
    /// priority, each state has a cooldown, and any input ends it at once. Fear runs as scream, then (if the
    /// character got hurt) a comic cry, then the recover beat, under 2.5 s in all. Can be switched off in settings.
    /// </summary>
    public class EmotionController
    {
        private readonly EmotionProfile profile;
        private readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
        private readonly Queue<EmotionCue> queue = new Queue<EmotionCue>();
        private float now, left;

        public bool Enabled = true;
        public EmotionCue Current { get; private set; }
        /// <summary>Raised when a cue starts (the runtime plays its anim, bark, effect and haptic).</summary>
        public event Action<EmotionCue> Started;

        public EmotionController(EmotionProfile profile) { this.profile = profile; }

        public bool Ready(string state) => !readyAt.TryGetValue(state, out var t) || now >= t;

        /// <summary>Asks for a reaction. False when off, unknown, cooling down or outranked.</summary>
        public bool Trigger(string state)
        {
            if (!Enabled || profile == null) return false;
            var cue = profile.Cue(state);
            if (cue == null || !Ready(state)) return false;
            if (Current != null && cue.priority < Current.priority) return false;
            queue.Clear();
            Begin(cue);
            return true;
        }

        /// <summary>The fear sequence: scream, then cry if hurt, then recover. Animals play their single fear reaction.</summary>
        public bool Fear(bool hurt)
        {
            if (profile != null && profile.animal) return Trigger("fear");
            if (!Trigger("scream")) return false;
            if (hurt) { var cry = profile.Cue("cry"); if (cry != null) queue.Enqueue(cry); }
            var rec = profile.Cue("recover");
            if (rec != null) queue.Enqueue(rec);
            return true;
        }

        private void Begin(EmotionCue cue)
        {
            Current = cue;
            left = cue.seconds;
            readyAt[cue.state] = now + cue.cooldown;
            Started?.Invoke(cue);
        }

        /// <summary>Any player input: the reaction stops so it never gets in the way.</summary>
        public void Skip() { queue.Clear(); Current = null; left = 0f; }

        public void Tick(float dt, bool hadInput)
        {
            now += dt;
            if (hadInput) { Skip(); return; }
            if (Current == null) return;
            left -= dt;
            if (left > 0f) return;
            Current = null;
            if (queue.Count > 0) Begin(queue.Dequeue());
        }

        /// <summary>
        /// Reduces a cue to what the rig can show: the blendshapes it has; when it has none of them, the bone
        /// poses instead; when it has neither, nothing (the bark, sound and effect still play).
        /// </summary>
        public static EmotionOutput Resolve(EmotionCue cue, Func<string, bool> hasBlendshape, Func<string, bool> hasBone)
        {
            var o = new EmotionOutput { cue = cue };
            if (cue == null) return o;
            foreach (var b in cue.blendshapes)
            {
                int eq = b.IndexOf('=');
                string name = eq > 0 ? b.Substring(0, eq) : b;
                float w = 1f;
                if (eq > 0) float.TryParse(b.Substring(eq + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w);
                if (hasBlendshape != null && hasBlendshape(name)) o.blendshapes.Add(new KeyValuePair<string, float>(name, Math.Max(0f, Math.Min(1f, w))));
            }
            if (o.blendshapes.Count == 0)
            {
                foreach (var b in cue.bones)
                {
                    int eq = b.IndexOf('=');
                    string bone = eq > 0 ? b.Substring(0, eq) : b;
                    if (hasBone != null && hasBone(bone)) o.bones.Add(b);
                }
                o.usedFallback = o.bones.Count > 0;
            }
            return o;
        }
    }
}
