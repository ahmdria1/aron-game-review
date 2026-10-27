// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Content
{
    [System.Serializable]
    public class ContentConfigFile
    {
        /// <summary>Folder that holds the built per-country bundles, e.g. https://cdn.example.com/aron/v1 (no trailing slash). Empty = no host yet.</summary>
        public string baseUrl = "";
    }

    /// <summary>
    /// The address the country content is downloaded from. Addressables resolves the remote load path through
    /// <see cref="BaseUrl"/> at run time, so the host can change without rebuilding the app: edit Resources/Config/content.json (or ship a new one).
    /// </summary>
    public static class ContentHosting
    {
        public const string ConfigPath = "Config/content";
        private static string baseUrl;

        /// <summary>Used by the content profile as <c>{Aron.Content.ContentHosting.BaseUrl}/[BuildTarget]</c>.</summary>
        public static string BaseUrl
        {
            get
            {
                if (baseUrl != null) return baseUrl;
                var t = Resources.Load<TextAsset>(ConfigPath);
                var f = t != null ? JsonUtility.FromJson<ContentConfigFile>(t.text) : null;
                baseUrl = Normalize(f != null ? f.baseUrl : "");
                return baseUrl;
            }
            set { baseUrl = Normalize(value); }
        }

        public static bool Configured => !string.IsNullOrEmpty(BaseUrl);

        public static string Normalize(string url) => string.IsNullOrWhiteSpace(url) ? "" : url.Trim().TrimEnd('/');

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => baseUrl = null;
    }
}
