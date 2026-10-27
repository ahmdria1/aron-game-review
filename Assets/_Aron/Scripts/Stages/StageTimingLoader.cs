// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Regions;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>Loads Resources/Config/stage_timing.json (the 5..8 minute table, par and checkpoint spacing) before any stage is built.</summary>
    public static class StageTimingLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            var t = Resources.Load<TextAsset>("Config/stage_timing");
            if (t == null) { global::Aron.Logic.Diagnostics.Log.Warn("stage_timing.json missing, using built-in defaults"); return; }
            var f = JsonUtility.FromJson<StageTimingFile>(t.text);
            var problems = f.Validate();
            if (problems.Count > 0) { global::Aron.Logic.Diagnostics.Log.Warn("stage_timing.json: " + string.Join("; ", problems)); return; }
            StageTiming.Active = f;
        }
    }
}
