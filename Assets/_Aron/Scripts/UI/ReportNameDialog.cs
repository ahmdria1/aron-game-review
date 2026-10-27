// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Localization;
using Aron.Logic.Names;
using Aron.Naming;
using UnityEngine;

namespace Aron.UI
{
    /// <summary>
    /// "Report this name": opened from the results board or the lobby when a player taps another racer's name. The player picks a reason from a short
    /// list (no free text, no chat) and the report goes to the server, where three different reporters take the name away. Needs the online backend;
    /// offline it says so and does nothing.
    /// </summary>
    public class ReportNameDialog : MonoBehaviour
    {
        public static ReportNameDialog Open(Transform parent, string targetPlayerId, string shownName)
        {
            var go = new GameObject("ReportName");
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<ReportNameDialog>();
            d.Build(targetPlayerId, shownName);
            return d;
        }

        private void Build(string targetId, string shownName)
        {
            var canvas = UIFactory.CreateCanvas("ReportCanvas", 60);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.8f));
            var safe = UIFactory.SafeRoot(canvas);
            var g = UIFactory.VerticalGroup(safe, "Rows", 18f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 900f));
            var title = UIFactory.Label(g, "Title", Strings.Format("name_report_title", shownName), 48f, UIFactory.Sand);
            var status = UIFactory.Label(g, "Status", "", 38f, Color.white);
            foreach (var reason in ReportReasons.All)
            {
                string r = reason;
                UIFactory.CreateButton(g, r, Strings.Get("report_" + r), async () =>
                {
                    try
                    {
                        var res = await PlayerNames.Service.ReportAsync(targetId, r);
                        string key = res.Status == NameStatus.Ok ? "name_report_sent" : res.Status == NameStatus.RateLimited ? "name_err_rate_limited" : res.Status == NameStatus.Offline ? "name_report_offline" : "name_err_error";
                        ArabicTMP.SetText(status, Strings.Get(key));
                        if (res.Status == NameStatus.Ok) Destroy(gameObject, 1.2f);
                    }
                    catch (Exception) { ArabicTMP.SetText(status, Strings.Get("name_err_error")); }
                });
            }
            UIFactory.CreateButton(g, "Close", Strings.Get("back"), () => Destroy(gameObject));
        }
    }
}
