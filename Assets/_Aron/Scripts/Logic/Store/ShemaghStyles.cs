// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>
    /// Aron's shemagh has two looks and no more: the royal wrap he wears at all times, and the face wrap
    /// (talathum) that goes on by itself when a boss fight starts. The thobe has eight colours (store slot "thobe_colour").
    /// </summary>
    public static class ShemaghStyles
    {
        public const string ThobeSlot = "thobe_colour";
        public const string Royal = "royal", FaceWrap = "talathum";

        public static readonly string[] ThobeColours = { "white", "cream", "light_grey", "sky_blue", "olive", "navy", "brown", "black" };

        public static string ThobeItemId(string colour) => "thobe_c_" + colour;

        /// <summary>The wrap covers the lower face (boss fights only).</summary>
        public static bool CoversFace(string look) => look == FaceWrap;
    }

    public enum IdleAction { None, Adjust }

    /// <summary>
    /// Which shemagh look Aron shows. Royal all the time; the face wrap from the start of a boss fight until it ends.
    /// After a win the wrap comes off with a smile (<see cref="SmileAfterWin"/>); after a loss, a quit or a reload it
    /// just comes off. When Aron stands still for a few seconds he adjusts his shemagh and agal, nothing else.
    /// There is nothing to buy: no look can be owned, rented or equipped.
    /// </summary>
    public class ShemaghDirector
    {
        public float IdleSeconds = 6f, RepeatEvery = 8f;

        private float idle, sinceAction;
        public bool Boss { get; private set; }
        /// <summary>True for one read after a boss win, so the hero can smile as the wrap comes off.</summary>
        public bool SmileAfterWin { get { bool s = smile; smile = false; return s; } }
        private bool smile;

        public string Current => Boss ? ShemaghStyles.FaceWrap : ShemaghStyles.Royal;

        public void BeginBoss() { Boss = true; ResetIdle(); }

        /// <summary>The fight is over. <paramref name="won"/> true means the hero smiles as the wrap comes off.</summary>
        public void EndBoss(bool won)
        {
            if (Boss && won) smile = true;
            Boss = false;
            ResetIdle();
        }

        /// <summary>Leaving the scene (quit, death, reload) puts the royal wrap back without a smile.</summary>
        public void ClearOverrides() { Boss = false; smile = false; ResetIdle(); }

        public void ResetIdle() { idle = 0f; sinceAction = 0f; adjusted = false; }
        private bool adjusted;

        /// <summary>Call every frame with whether the player is giving input. Adjust means: play the shemagh-and-agal adjusting animation.</summary>
        public IdleAction Tick(float dt, bool playerActive)
        {
            if (playerActive || Boss) { ResetIdle(); return IdleAction.None; }
            idle += dt;
            if (idle < IdleSeconds) return IdleAction.None;
            if (adjusted)
            {
                sinceAction += dt;
                if (sinceAction < RepeatEvery) return IdleAction.None;
            }
            adjusted = true; sinceAction = 0f;
            return IdleAction.Adjust;
        }
    }

    /// <summary>Names of store items in the player's language, with a fallback to the Arabic name in the catalog.</summary>
    public static class StoreItemNames
    {
        public static string Name(StoreItem item, Func<string, string> get, Func<string, object[], string> format, Func<string, bool> has)
        {
            if (item == null) return "";
            string id = item.id;
            if (item.IsVehicle && !id.StartsWith("mount_", StringComparison.Ordinal))
            {
                // boat_dhow, car_sports, bike_road, moto_dirt: the name is one UI string per language ("rboat_dhow").
                string k = "r" + id;
                if (has(k)) return get(k);
            }
            else if (id.StartsWith("mount_", StringComparison.Ordinal))
            {
                string k = "r" + id;
                if (has(k)) return get(k);
            }
            else if (id.StartsWith("breed_", StringComparison.Ordinal))
            {
                // breed_sufr: a camel breed, one string per language ("rbreed_sufr")
                string k = "r" + id;
                if (has(k)) return get(k);
            }
            else if (id.StartsWith("thobe_c_", StringComparison.Ordinal) && has(id)) return format("thobe_colour_fmt", new object[] { get(id) });
            else if (id.StartsWith("saddle_", StringComparison.Ordinal) || id.StartsWith("deco_", StringComparison.Ordinal))
            {
                // saddle_royal_camel, deco_tassels_falcon_glider
                var parts = id.Split(new[] { '_' }, 3);
                if (parts.Length == 3)
                {
                    string kind = (parts[0] == "saddle" ? "saddle_kind_" : "deco_kind_") + parts[1];
                    string mount = "rmount_" + parts[2];
                    if (has(kind) && has(mount)) return format("mount_item_fmt", new object[] { get(mount), get(kind) });
                }
            }
            return item.nameAr ?? id;
        }
    }
}
