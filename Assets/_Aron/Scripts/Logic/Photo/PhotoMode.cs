// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Photo
{
    /// <summary>Where the player is when they ask for Photo Mode.</summary>
    public struct PhotoContext
    {
        public bool inStage, paused, inHub;
        public bool onlineRace;
        public bool timeTrial, raceStarted, raceFinished;
        public bool cutscene;
    }

    /// <summary>The free camera: orbit around the shot's focus, pan, zoom, FOV and tilt, every value clamped.</summary>
    public struct PhotoCamera
    {
        public float yaw, pitch, distance, fov, tilt;
        public float panX, panY, panZ;
    }

    /// <summary>
    /// The walls the free camera keeps to: an orbit radius, a pitch range, a pan sphere around the focus and the
    /// stage's playable box (so it can never look behind the set). Collision is the runtime's sphere cast; it
    /// reports the free distance and <see cref="PhotoMode.PullIn"/> keeps the camera in front of the hit.
    /// </summary>
    public class PhotoBounds
    {
        public float minDistance = 1.2f, maxDistance = 9f;
        public float minPitch = -10f, maxPitch = 70f;
        public float maxPan = 3f;
        public float minFov = 20f, maxFov = 80f;
        public float maxTilt = 30f;
        /// <summary>The playable box in world space.</summary>
        public float minX = -1e6f, maxX = 1e6f, minY = -1e6f, maxY = 1e6f, minZ = -1e6f, maxZ = 1e6f;
    }

    public static class PhotoMode
    {
        public static readonly string[] Filters = { "none", "warm_desert", "cinematic", "mono", "vivid" };
        public static readonly string[] TimesOfDay = { "stage", "day", "golden", "sunset", "night" };
        /// <summary>Poses for Aron, Noura and the companions. Noura's are all modest; none shows more than the costume does.</summary>
        public static readonly string[] Poses =
        {
            "stand", "joy", "scream", "victory_dance", "eat_local", "wave", "salute_hand_heart", "agal_spin",
            "bag_twirl", "laugh", "surprise", "thinking", "sit_majlis", "point_landmark", "selfie_peace",
        };
        public static readonly string[] CompanionPoses = { "stand", "joy", "surprise", "affection", "sleep", "proud" };

        /// <summary>Default orbit: just behind and above the heroes.</summary>
        public static PhotoCamera Start() => new PhotoCamera { yaw = 0f, pitch = 15f, distance = 4f, fov = 50f };

        /// <summary>
        /// From the pause menu in a stage or anywhere in a hub. Never in an online race; in a time trial only before
        /// the start or after the finish (so the clock and ghosts stay fair). Not during cutscenes.
        /// </summary>
        public static bool Allowed(PhotoContext c)
        {
            if (c.onlineRace || c.cutscene) return false;
            if (c.timeTrial) return !c.raceStarted || c.raceFinished;
            if (c.inHub) return true;
            return c.inStage && c.paused;
        }

        public static PhotoCamera Clamp(PhotoCamera cam, PhotoBounds b)
        {
            cam.distance = Clamp(cam.distance, b.minDistance, b.maxDistance);
            cam.pitch = Clamp(cam.pitch, b.minPitch, b.maxPitch);
            cam.fov = Clamp(cam.fov, b.minFov, b.maxFov);
            cam.tilt = Clamp(cam.tilt, -b.maxTilt, b.maxTilt);
            cam.yaw = ((cam.yaw % 360f) + 360f) % 360f;
            float pan = (float)Math.Sqrt(cam.panX * cam.panX + cam.panY * cam.panY + cam.panZ * cam.panZ);
            if (pan > b.maxPan) { float k = b.maxPan / pan; cam.panX *= k; cam.panY *= k; cam.panZ *= k; }
            return cam;
        }

        /// <summary>Where the camera ends up in world space for a focus point, kept inside the playable box.</summary>
        public static void Position(PhotoCamera cam, float fx, float fy, float fz, PhotoBounds b, out float x, out float y, out float z)
        {
            double yaw = cam.yaw * Math.PI / 180.0, pitch = cam.pitch * Math.PI / 180.0;
            float cx = fx + cam.panX, cy = fy + cam.panY, cz = fz + cam.panZ;
            x = cx - (float)(Math.Sin(yaw) * Math.Cos(pitch)) * cam.distance;
            y = cy + (float)Math.Sin(pitch) * cam.distance;
            z = cz - (float)(Math.Cos(yaw) * Math.Cos(pitch)) * cam.distance;
            x = Clamp(x, b.minX, b.maxX);
            y = Clamp(y, b.minY, b.maxY);
            z = Clamp(z, b.minZ, b.maxZ);
        }

        /// <summary>Distance after a sphere cast found a wall <paramref name="freeDistance"/> away: stays a little in front of it.</summary>
        public static float PullIn(float wanted, float freeDistance, PhotoBounds b, float skin = 0.25f)
        {
            float d = Math.Min(wanted, freeDistance - skin);
            return Math.Max(b.minDistance * 0.5f, d);
        }

        /// <summary>Depth of field only on the High tier; it costs too much elsewhere.</summary>
        public static bool DepthOfField(QualityTierName tier) => tier == QualityTierName.High;

        /// <summary>The 2x supersampled capture is a High tier option; the rest capture at screen size.</summary>
        public static int CaptureScale(QualityTierName tier, bool supersample) => tier == QualityTierName.High && supersample ? 2 : 1;

        /// <summary>The country's frames and stickers: its frame, its signature dish, its animal and the heroes' emotes.</summary>
        public static List<string> Stickers(string country, string dishId, IEnumerable<string> animals)
        {
            var list = new List<string> { "frame_" + country, "frame_plain", "sticker_dish_" + dishId };
            if (animals != null) foreach (var a in animals) list.Add("sticker_animal_" + a);
            list.Add("sticker_yalla");
            list.Add("sticker_heart");
            return list;
        }

        /// <summary>
        /// Saved file name: the country and the moment only. No player name, id or location goes in the name or
        /// in metadata (the encoder writes no EXIF or GPS).
        /// </summary>
        public static string FileName(string country, DateTime utcNow) =>
            "aron_" + (string.IsNullOrEmpty(country) ? "trip" : country) + "_" + utcNow.ToUniversalTime().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".png";

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }

    /// <summary>Mirror of the runtime quality tiers, so the logic needs no engine types.</summary>
    public enum QualityTierName { Low, Medium, High }

    /// <summary>
    /// The small game logo in a corner of every exported photo. It is always on (marketing) and kept tasteful:
    /// about 14% of the width, a 3% margin, 85% opacity, bottom-right (bottom-left in Arabic and Urdu).
    /// </summary>
    public static class Watermark
    {
        public const float WidthShare = 0.14f, Margin = 0.03f, Opacity = 0.85f;
        public const bool AlwaysOn = true;

        public static void Rect(int width, int height, int logoW, int logoH, bool rtl, out int x, out int y, out int w, out int h)
        {
            w = Math.Max(16, (int)(width * WidthShare));
            h = Math.Max(1, logoW > 0 ? w * logoH / logoW : w / 3);
            int m = (int)(Math.Min(width, height) * Margin);
            x = rtl ? m : width - w - m;
            y = m;
        }

        /// <summary>
        /// Blends the logo (RGBA, row 0 at the bottom like Unity textures) over the photo pixels in place.
        /// Nearest-neighbour scaling is enough at this size.
        /// </summary>
        public static void Apply(byte[] rgba, int width, int height, byte[] logo, int logoW, int logoH, bool rtl)
        {
            if (rgba == null || logo == null || logoW <= 0 || logoH <= 0) return;
            Rect(width, height, logoW, logoH, rtl, out int x0, out int y0, out int w, out int h);
            for (int y = 0; y < h && y0 + y < height; y++)
                for (int x = 0; x < w && x0 + x < width; x++)
                {
                    int lx = x * logoW / w, ly = y * logoH / h;
                    int li = (ly * logoW + lx) * 4;
                    float a = logo[li + 3] / 255f * Opacity;
                    if (a <= 0f) continue;
                    int pi = ((y0 + y) * width + x0 + x) * 4;
                    for (int c = 0; c < 3; c++) rgba[pi + c] = (byte)(rgba[pi + c] * (1f - a) + logo[li + c] * a + 0.5f);
                    rgba[pi + 3] = 255;
                }
        }
    }

    public enum GalleryPermission { Unknown, Granted, Denied }

    /// <summary>Android MediaStore / iOS PHPhotoLibrary (add-only) behind one interface, mocked in tests.</summary>
    public interface IGallery
    {
        GalleryPermission Status { get; }
        /// <summary>Shows the OS prompt for add-only access; calls back with the answer.</summary>
        void Request(Action<GalleryPermission> done);
        /// <summary>Writes a PNG to the photos app. True on success.</summary>
        bool Save(byte[] png, string fileName);
    }

    public enum SaveResult { Saved, Denied, Failed }

    /// <summary>
    /// Save to gallery: the add-only permission is asked at the first save, never at launch. A denial is remembered
    /// (the button then offers the OS settings and the share sheet still works without any permission).
    /// </summary>
    public static class PhotoSaver
    {
        public static void Save(IGallery gallery, SaveData data, byte[] png, string fileName, Action<SaveResult> done)
        {
            void Write()
            {
                data.galleryPermission = 1;
                done(gallery.Save(png, fileName) ? SaveResult.Saved : SaveResult.Failed);
            }
            switch (gallery.Status)
            {
                case GalleryPermission.Granted: Write(); return;
                case GalleryPermission.Denied: data.galleryPermission = 2; done(SaveResult.Denied); return;
            }
            gallery.Request(answer =>
            {
                if (answer == GalleryPermission.Granted) Write();
                else { data.galleryPermission = 2; done(SaveResult.Denied); }
            });
        }
    }
}
