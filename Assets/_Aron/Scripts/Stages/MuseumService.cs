// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Core;
using Aron.Audio;
using Aron.Feedback;
using Aron.Fx;
using Aron.Localization;
using Aron.Logic.Culture;
using Aron.Logic.Passport;
using Aron.Save;
using Aron.UI;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// The museum's runtime side: loads the artifact list, and after a stage is finished hands out the country's artifact (every golden
    /// box in one run) and the passport stamp (the country's last boss). Both play the passport's stamp sound and haptic.
    /// </summary>
    public static class MuseumService
    {
        private static ArtifactFile file;
        public static ArtifactFile Artifacts
        {
            get
            {
                if (file != null) return file;
                var t = Resources.Load<TextAsset>("Museum/artifacts");
                try { file = t != null ? JsonUtility.FromJson<ArtifactFile>(t.text) : null; } catch (Exception) { file = null; }
                return file ?? (file = new ArtifactFile());
            }
        }

        /// <summary>The artifact found in the run that just ended, for the result screen (null when none).</summary>
        public static ArtifactDef LastFound { get; private set; }
        public static bool LastStamped { get; private set; }

        /// <summary>Call once after StageSession.RecordFinish, before the save.</summary>
        public static void OnStageFinished(string stageId, bool hiddenStar)
        {
            LastFound = null; LastStamped = false;
            var data = SaveSystem.Data;
            var catalog = StageSession.Catalog;
            var now = DateTime.UtcNow;
            LastStamped = PassportBook.OnStageFinished(catalog, data, stageId, now);
            LastFound = ArtifactBook.OnHiddenStar(catalog, data, Artifacts, stageId, hiddenStar, now);
            if (LastFound == null && !LastStamped) return;
            Sfx.Play(ArtifactBook.Sfx);
            Haptics.OnCue(ArtifactBook.Haptic);
            var p = Player.PlayerController.Active;
            if (p != null) FxPlayer.Play(ArtifactBook.Fx, p.transform.position + Vector3.up * 1.5f);
        }

        public static string NameOf(ArtifactDef a) => a.name.Get(Strings.Language);
        public static string FactOf(ArtifactDef a) => a.fact.Get(Strings.Language);
    }
}
