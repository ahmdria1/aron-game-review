// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Diagnostics;

namespace Aron.Logic.Diagnostics
{
    /// <summary>
    /// The only way game code writes to the log. <see cref="Info"/> and <see cref="Warn"/> are removed by the compiler (the call
    /// and its arguments) unless the build is the editor, a development build, or defines ARON_LOGS. A release build therefore
    /// carries none of the log text. <see cref="Error"/> always stays so it can reach the crash reporter.
    /// </summary>
    public static class Log
    {
        /// <summary>Set once at start-up by the Unity bridge (Debug.Log, plus breadcrumbs for the crash reporter).</summary>
        public static Action<string, string> Sink;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ARON_LOGS")]
        public static void Info(string message) { Sink?.Invoke("info", message); }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ARON_LOGS")]
        public static void Warn(string message) { Sink?.Invoke("warn", message); }

        public static void Error(string message) { Sink?.Invoke("error", message); }
    }
}
