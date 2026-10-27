// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Aron.Content
{
    /// <summary>
    /// The country groups through Unity Addressables: one remote group and one label per country (label = the group id, e.g. <c>country_egypt</c>),
    /// one address per track (<c>music/&lt;track&gt;</c>). Only compiled when com.unity.addressables is installed.
    /// The remote load path is <c>{Aron.Content.ContentHosting.BaseUrl}/[BuildTarget]</c>, so the host is a setting, not a rebuild.
    /// </summary>
    public sealed class AddressablesBackend : IContentBackend
    {
        public const string AddressPrefix = "music/";

        private bool cancelled;
        private readonly List<AsyncOperationHandle> held = new List<AsyncOperationHandle>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() => ContentService.BackendFactory = () => new AddressablesBackend();

        public bool Available => ContentHosting.Configured;

        public IEnumerator SizeOf(string group, Action<long> result)
        {
            if (!Available) { result(-1); yield break; }
            var init = Addressables.InitializeAsync(false);
            yield return init;
            if (init.Status != AsyncOperationStatus.Succeeded) { Addressables.Release(init); result(-1); yield break; }
            Addressables.Release(init);
            var h = Addressables.GetDownloadSizeAsync(group);
            yield return h;
            result(h.Status == AsyncOperationStatus.Succeeded ? h.Result : -1);
            Addressables.Release(h);
        }

        public IEnumerator Fetch(string group, Action<float> progress, Action<string> done)
        {
            cancelled = false;
            var h = Addressables.DownloadDependenciesAsync(group, false);
            while (!h.IsDone)
            {
                progress?.Invoke(h.GetDownloadStatus().Percent);
                yield return null;
            }
            bool ok = h.Status == AsyncOperationStatus.Succeeded;
            string why = ok ? null : "download failed";
            if (h.IsValid()) Addressables.Release(h);
            if (ok) progress?.Invoke(1f);
            done?.Invoke(why);
        }

        public IEnumerator LoadMusic(string group, IReadOnlyList<string> tracks, Action<string, AudioClip> loaded, Action<string> done)
        {
            string failure = null;
            foreach (var t in tracks)
            {
                var h = Addressables.LoadAssetAsync<AudioClip>(AddressPrefix + t);
                yield return h;
                if (h.Status == AsyncOperationStatus.Succeeded) { held.Add(h); loaded?.Invoke(t, h.Result); }
                else { failure = "could not load " + t; Addressables.Release(h); }
            }
            done?.Invoke(failure);
        }

        public void Cancel()
        {
            cancelled = true;   // the fetch loop sees it on its next frame and releases the handle itself
        }
    }
}
