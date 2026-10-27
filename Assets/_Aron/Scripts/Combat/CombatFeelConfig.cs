// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Combat;
using UnityEngine;

namespace Aron.Combat
{
    /// <summary>Loads Resources/Config/combat_feel.json once (aim assist and hit-stop numbers).</summary>
    public static class CombatFeelConfig
    {
        private static CombatFeelFile file;

        public static CombatFeelFile File
        {
            get
            {
                if (file != null) return file;
                var t = Resources.Load<TextAsset>("Config/combat_feel");
                file = t != null ? JsonUtility.FromJson<CombatFeelFile>(t.text) : new CombatFeelFile();
                var problems = file.Validate();
                if (problems.Count > 0) { global::Aron.Logic.Diagnostics.Log.Warn("combat_feel.json: " + string.Join("; ", problems)); file = new CombatFeelFile(); }
                return file;
            }
        }
    }
}
