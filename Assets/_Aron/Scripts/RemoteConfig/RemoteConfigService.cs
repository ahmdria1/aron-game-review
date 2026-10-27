// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.Online;
using Aron.Logic.Performance;
using UnityEngine;
using UnityEngine.Networking;

namespace Aron.RemoteConfig
{
    /// <summary>
    /// Server-swappable shop signs. Order: disk cache -> bundled fallback (Resources) -> network fetch.
    /// A successful fetch is validated, cached and broadcast via <see cref="ConfigChanged"/>.
    /// </summary>
    public class RemoteConfigService : MonoBehaviour
    {
        public const string FallbackResource = "RemoteConfig/default_signs";
        public static RemoteConfigService Instance { get; private set; }
        public static event Action<ShopSignsConfig> ConfigChanged;

        [SerializeField] private string url = "";
        [SerializeField] private int timeoutSeconds = 8;

        public ShopSignsConfig Current { get; private set; }
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        private static string CachePath => Path.Combine(Application.persistentDataPath, "remote_signs.json");
        private static string EtagPath => Path.Combine(Application.persistentDataPath, "remote_signs_etag.json");
        private const string EtagKey = "signs";
        private EtagCache etags;

        public void SetUrl(string u) => url = u;

        private void Awake()
        {
            Instance = this;
            etags = new EtagCache(LoadEtags());
            // Sign pictures are rebuildable, so they are the first thing given up when the phone is short of memory.
            Caches.Registry.Register("sign-textures", 0, TrimTextures);
            Current = LoadCached() ?? LoadFallback() ?? new ShopSignsConfig { signs = new ShopSignEntry[0] };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Caches.Registry.Unregister("sign-textures");
        }

        private int TrimTextures()
        {
            int n = textures.Count;
            textures.Clear();                       // pictures still on a sign stay alive; the rest are unloaded
            Resources.UnloadUnusedAssets();
            return n;
        }

        private static EtagStore LoadEtags()
        {
            try { if (File.Exists(EtagPath)) return JsonUtility.FromJson<EtagStore>(File.ReadAllText(EtagPath)); }
            catch (Exception) { /* start with nothing cached */ }
            return new EtagStore();
        }

        private void Start()
        {
            ConfigChanged?.Invoke(Current);
            if (!string.IsNullOrEmpty(url) && GameSettingsService.IsOn(Aron.Logic.Config.Feature.ShopSigns)) StartCoroutine(Fetch());
        }

        public static ShopSignsConfig Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { var c = JsonUtility.FromJson<ShopSignsConfig>(json); return c != null && c.IsValid() ? c : null; }
            catch (Exception) { return null; }
        }

        private static ShopSignsConfig LoadCached()
        {
            try { return File.Exists(CachePath) ? Parse(File.ReadAllText(CachePath)) : null; }
            catch (Exception) { return null; }
        }

        private static ShopSignsConfig LoadFallback()
        {
            var ta = Resources.Load<TextAsset>(FallbackResource);
            return ta != null ? Parse(ta.text) : null;
        }

        private IEnumerator Fetch()
        {
            // Not on mobile data with Data saver on, and never while playing or in the background.
            if (!NetworkState.Allow(Traffic.Sync)) yield break;
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSeconds;
                // Ask "has it changed?" only when the last answer is still on disk.
                var inm = File.Exists(CachePath) ? etags.IfNoneMatch(EtagKey) : null;
                if (inm != null) req.SetRequestHeader("If-None-Match", inm);
                yield return req.SendWebRequest();
                NetworkState.Meter.Received(Traffic.Sync, (int)Math.Min(int.MaxValue, req.downloadedBytes + 400));
                if (req.responseCode == 304) yield break;     // unchanged: the cached copy is already current
                if (req.result != UnityWebRequest.Result.Success)
                {
                    global::Aron.Logic.Diagnostics.Log.Info("[RemoteConfig] Offline / failed, using cached/fallback: " + req.error);
                    yield break;
                }
                var cfg = Parse(req.downloadHandler.text);
                if (!ShopSignsConfig.ShouldReplace(Current, cfg)) { global::Aron.Logic.Diagnostics.Log.Warn("[RemoteConfig] Server config invalid or older, ignored."); yield break; }
                Current = cfg;
                try
                {
                    AtomicFile.WriteAllText(CachePath, req.downloadHandler.text);
                    var tag = req.GetResponseHeader("ETag");
                    if (!string.IsNullOrEmpty(tag)) { etags.OnResponse(EtagKey, 200, tag, "", DateTime.UtcNow.Ticks); AtomicFile.WriteAllText(EtagPath, JsonUtility.ToJson(etags.store)); }
                } catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn(e.Message); }
                ConfigChanged?.Invoke(cfg);
            }
        }

        /// <summary>Downloads (once) and returns a sign texture via callback; null on failure.</summary>
        public void GetTexture(string imageUrl, Action<Texture2D> done)
        {
            if (string.IsNullOrEmpty(imageUrl)) { done(null); return; }
            if (textures.TryGetValue(imageUrl, out var t)) { done(t); return; }
            if (!NetworkState.Allow(Traffic.Download)) { done(null); return; }
            StartCoroutine(FetchTexture(imageUrl, done));
        }

        private IEnumerator FetchTexture(string imageUrl, Action<Texture2D> done)
        {
            using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();
                Texture2D tex = req.result == UnityWebRequest.Result.Success ? DownloadHandlerTexture.GetContent(req) : null;
                if (tex != null) { textures[imageUrl] = tex; NetworkState.Meter.Received(Traffic.Download, (int)Math.Min(int.MaxValue, req.downloadedBytes)); }
                done(tex);
            }
        }
    }
}
