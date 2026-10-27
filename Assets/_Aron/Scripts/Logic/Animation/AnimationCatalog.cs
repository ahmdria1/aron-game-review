// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Animation
{
    /// <summary>One clip in the production list (Resources/Config/animation_clips.json, written with docs/animation_list.md by tools/gen_animation_list.py).</summary>
    [Serializable]
    public class AnimClipDef
    {
        public string id = "", actor = "", group = "", priority = "P1", source = "", kind = "", param = "", value = "", note = "";
        public bool loop;
        /// <summary>Length at 30 fps.</summary>
        public int frames = 30;

        public bool IsP0 => priority == "P0";
        public float Seconds => frames / 30f;
        /// <summary>The Animator state is named after the clip id.</summary>
        public string StateName => id;
    }

    [Serializable]
    public class AnimClipFile
    {
        public int fps = 30;
        public AnimClipDef[] clips = Array.Empty<AnimClipDef>();

        public static readonly string[] Actors = { "aron", "noura", "duhaim", "falcon", "oryx", "camel", "horse", "ostrich", "giraffe", "falcon_glider", "rider", "camel_racer", "robot_jockey", "oryx_wild" };
        public static readonly string[] Priorities = { "P0", "P1", "P2", "P3" };
        public static readonly string[] Sources = { "motion capture library", "motion capture reference", "keyframed", "hand-animated" };

        public IEnumerable<AnimClipDef> P0() { foreach (var c in clips) if (c.IsP0) yield return c; }
        public IEnumerable<AnimClipDef> For(string actor) { foreach (var c in clips) if (c.actor == actor) yield return c; }

        /// <summary>The parameters P0 clips need, each once.</summary>
        public List<AnimParam> P0Parameters()
        {
            var seen = new HashSet<string>(); var list = new List<AnimParam>();
            foreach (var c in P0())
                if (seen.Add(c.param)) foreach (var p in AnimParams.All) if (p.Name == c.param) list.Add(p);
            return list;
        }

        public static AnimParamType TypeOf(string kind)
        {
            switch (kind) { case "float": return AnimParamType.Float; case "int": return AnimParamType.Int; case "bool": return AnimParamType.Bool; default: return AnimParamType.Trigger; }
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (fps != 30) e.Add("clips are listed at 30 fps");
            if (clips.Length < 200 || clips.Length > 340) e.Add("the list is meant to hold about 200-340 clips, has " + clips.Length);
            var ids = new HashSet<string>();
            foreach (var c in clips)
            {
                if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) { e.Add("missing or duplicate clip id " + c.id); continue; }
                if (Array.IndexOf(Actors, c.actor) < 0) e.Add(c.id + ": unknown actor " + c.actor);
                if (!c.id.StartsWith(c.actor + "_", StringComparison.Ordinal)) e.Add(c.id + ": the id must start with the actor");
                if (Array.IndexOf(Priorities, c.priority) < 0) e.Add(c.id + ": priority " + c.priority);
                if (Array.IndexOf(Sources, c.source) < 0) e.Add(c.id + ": source " + c.source);
                if (c.frames < 4 || c.frames > 240) e.Add(c.id + ": frames " + c.frames + " is outside 4..240");
                if (c.loop && c.frames < 8) e.Add(c.id + ": a loop this short will flicker");
                bool found = false;
                foreach (var p in AnimParams.All)
                    if (p.Name == c.param) { found = true; if (p.Type != TypeOf(c.kind)) e.Add(c.id + ": " + c.param + " is a " + p.Type + " parameter, not " + c.kind); }
                if (!found) e.Add(c.id + ": unknown parameter " + c.param);
            }
            return e;
        }
    }
}
