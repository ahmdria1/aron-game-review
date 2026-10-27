// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using Aron.Logic.Access;
using Aron.Save;
using UnityEngine;

namespace Aron.Fx
{
    /// <summary>Keeps the runtime switches that follow the assist setting in step with the save: when it is saved or loaded, the landing shadow boost follows it.</summary>
    public static class AssistRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SaveSystem.Saved -= OnSaved;
            SaveSystem.Saved += OnSaved;
            Sync(SaveSystem.Data);
        }

        private static void OnSaved(SaveData d) => Sync(d);

        public static void Sync(SaveData d)
        {
            DropShadow.AssistBoost = AssistRules.ShadowBoost(d != null ? d.access : null);
        }
    }
}
