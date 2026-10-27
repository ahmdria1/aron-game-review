// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Aron.RemoteConfig
{
    /// <summary>
    /// Reads the settings document from Unity Remote Config. Compiled only when both packages are installed
    /// (com.unity.remote-config and com.unity.services.authentication); without them the game runs on the file it ships with.
    /// The only identifier involved is the anonymous service player id; no advertising id and no personal data are sent.
    /// The whole document sits in one JSON key, "settings", whose layout is documented in docs/remote_config.md.
    /// </summary>
    public sealed class UgsSettingsSource : IRemoteSettingsSource
    {
        public const string Key = "settings";

        private struct UserAttributes { }
        private struct AppAttributes { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { if (GameSettingsService.Source == null) GameSettingsService.Source = new UgsSettingsSource(); }

        public IEnumerator Fetch(Action<string> done)
        {
            var task = FetchAsync();
            while (!task.IsCompleted) yield return null;
            done(task.IsFaulted || task.IsCanceled ? null : task.Result);
        }

        private static async Task<string> FetchAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await Unity.Services.RemoteConfig.RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
                string json = Unity.Services.RemoteConfig.RemoteConfigService.Instance.appConfig.GetJson(Key, "");
                return string.IsNullOrWhiteSpace(json) ? null : json;
            }
            catch (Exception) { return null; }   // offline, blocked or not configured: keep what the phone has
        }
    }
}
