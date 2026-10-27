// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Content
{
    /// <summary>
    /// Where on-demand content comes from. The game talks to this and never to Addressables directly, so the rules
    /// (Wi-Fi only, never in a stage, fallback music) are tested with a fake and the package stays optional.
    /// Every method that takes time is a coroutine, so it runs on Unity's own loop and ends with a callback.
    /// </summary>
    public interface IContentBackend
    {
        /// <summary>False when no content host is configured: nothing can be downloaded and the base music plays instead.</summary>
        bool Available { get; }

        /// <summary>Bytes still to download for a group: 0 when it is already on the device, -1 when it could not be found out.</summary>
        IEnumerator SizeOf(string group, Action<long> result);

        /// <summary>Downloads a group into the on-device cache. <paramref name="done"/> gets null on success or a short reason.</summary>
        IEnumerator Fetch(string group, Action<float> progress, Action<string> done);

        /// <summary>Loads the music tracks of an already downloaded group into memory.</summary>
        IEnumerator LoadMusic(string group, IReadOnlyList<string> tracks, Action<string, AudioClip> loaded, Action<string> done);

        /// <summary>Stops the download in progress (a stage is starting, or mobile data was switched on without consent).</summary>
        void Cancel();
    }
}
