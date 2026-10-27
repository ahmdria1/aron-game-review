// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Diagnostics;
using UnityEngine;

namespace Aron.Diagnostics
{
    /// <summary>Connects <see cref="Log"/> to the Unity console and, in release builds, silences everything below errors.</summary>
    public static class LogBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Log.Sink = (level, message) =>
            {
                if (level == "error") Debug.LogError(message);
                else if (level == "warn") Debug.LogWarning(message);
                else Debug.Log(message);
            };
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // Engine and package messages too: players' phones never need console text.
            Debug.unityLogger.filterLogType = LogType.Error;
#endif
        }
    }
}
