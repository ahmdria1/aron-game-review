// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Sky
{
    /// <summary>The six parts of the day the sky and lighting follow.</summary>
    public enum DayPhase { Fajr, Morning, Noon, Asr, Maghrib, Night }

    /// <summary>Where something is in the sky, in degrees. Azimuth is from north, going east.</summary>
    public struct SkyPosition
    {
        public double Altitude, Azimuth;
        public SkyPosition(double alt, double az) { Altitude = alt; Azimuth = az; }
    }

    /// <summary>The moon as seen on a given night.</summary>
    public struct MoonPhase
    {
        /// <summary>0 at new moon, 1 at full moon.</summary>
        public double Illumination;
        /// <summary>Degrees the moon is east of the sun along the sky, 0..360. Under 180 means waxing.</summary>
        public double Elongation;
        public bool Waxing => Elongation < 180.0;
        /// <summary>Days since the new moon, 0..29.5.</summary>
        public double AgeDays => Elongation / 360.0 * Astronomy.SynodicMonth;
        /// <summary>new, waxing_crescent, first_quarter, waxing_gibbous, full, waning_gibbous, last_quarter, waning_crescent.</summary>
        public string Name
        {
            get
            {
                double e = Elongation;
                if (e < 12 || e >= 348) return "new";
                if (e < 80) return "waxing_crescent";
                if (e < 100) return "first_quarter";
                if (e < 168) return "waxing_gibbous";
                if (e < 192) return "full";
                if (e < 260) return "waning_gibbous";
                if (e < 280) return "last_quarter";
                return "waning_crescent";
            }
        }
    }

    /// <summary>
    /// Low-precision sun and moon positions (after Meeus, "Astronomical Algorithms", chapters 25, 47 and 48),
    /// good to well under a degree, which is plenty for a sky dome. Everything takes UTC.
    /// </summary>
    public static class Astronomy
    {
        public const double SynodicMonth = 29.530588853;
        private const double Deg = Math.PI / 180.0;
        private static readonly DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public static double JulianDay(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local) utc = utc.ToUniversalTime();
            return 2451545.0 + (utc - J2000).TotalDays;
        }

        private static double Norm(double d) { d %= 360.0; return d < 0 ? d + 360.0 : d; }
        private static double Sin(double d) => Math.Sin(d * Deg);
        private static double Cos(double d) => Math.Cos(d * Deg);

        /// <summary>Ecliptic longitude of the sun, degrees.</summary>
        public static double SunLongitude(double jd)
        {
            double t = (jd - 2451545.0) / 36525.0;
            double l0 = 280.46646 + 36000.76983 * t;
            double m = 357.52911 + 35999.05029 * t;
            double c = (1.914602 - 0.004817 * t) * Sin(m) + 0.019993 * Sin(2 * m) + 0.000289 * Sin(3 * m);
            return Norm(l0 + c);
        }

        /// <summary>Ecliptic longitude and latitude of the moon, degrees (the biggest terms of Meeus 47).</summary>
        public static void MoonEcliptic(double jd, out double lon, out double lat)
        {
            double t = (jd - 2451545.0) / 36525.0;
            double l = 218.3164477 + 481267.88123421 * t;
            double d = 297.8501921 + 445267.1114034 * t;
            double m = 357.5291092 + 35999.0502909 * t;
            double mp = 134.9633964 + 477198.8675055 * t;
            double f = 93.2720950 + 483202.0175233 * t;
            lon = Norm(l + 6.288774 * Sin(mp) + 1.274027 * Sin(2 * d - mp) + 0.658314 * Sin(2 * d) + 0.213618 * Sin(2 * mp)
                - 0.185116 * Sin(m) - 0.114332 * Sin(2 * f) + 0.058793 * Sin(2 * d - 2 * mp) + 0.057066 * Sin(2 * d - m - mp)
                + 0.053322 * Sin(2 * d + mp) + 0.045758 * Sin(2 * d - m) - 0.040923 * Sin(m - mp) - 0.034720 * Sin(d)
                - 0.030383 * Sin(m + mp));
            lat = 5.128122 * Sin(f) + 0.280602 * Sin(mp + f) + 0.277693 * Sin(mp - f) + 0.173237 * Sin(2 * d - f);
        }

        /// <summary>Phase of the moon at an instant (Meeus 48.4 for the phase angle).</summary>
        public static MoonPhase Moon(DateTime utc)
        {
            double jd = JulianDay(utc);
            double t = (jd - 2451545.0) / 36525.0;
            double d = Norm(297.8501921 + 445267.1114034 * t);
            double m = Norm(357.5291092 + 35999.0502909 * t);
            double mp = Norm(134.9633964 + 477198.8675055 * t);
            double i = 180.0 - d - 6.289 * Sin(mp) + 2.100 * Sin(m) - 1.274 * Sin(2 * d - mp) - 0.658 * Sin(2 * d)
                - 0.214 * Sin(2 * mp) - 0.110 * Sin(d);
            MoonEcliptic(jd, out double ml, out _);
            return new MoonPhase { Illumination = (1.0 + Cos(i)) / 2.0, Elongation = Norm(ml - SunLongitude(jd)) };
        }

        private static void Equatorial(double lon, double lat, out double ra, out double dec)
        {
            const double eps = 23.4393;
            ra = Norm(Math.Atan2(Sin(lon) * Cos(eps) - Math.Tan(lat * Deg) * Sin(eps), Cos(lon)) / Deg);
            dec = Math.Asin(Sin(lat) * Cos(eps) + Cos(lat) * Sin(eps) * Sin(lon)) / Deg;
        }

        private static double SiderealDegrees(double jd, double longitude) => Norm(280.46061837 + 360.98564736629 * (jd - 2451545.0) + longitude);

        private static SkyPosition Horizon(double ra, double dec, double jd, double latitude, double longitude, out double hourAngle)
        {
            hourAngle = Norm(SiderealDegrees(jd, longitude) - ra);
            double alt = Math.Asin(Sin(latitude) * Sin(dec) + Cos(latitude) * Cos(dec) * Cos(hourAngle)) / Deg;
            double az = Math.Atan2(-Sin(hourAngle), Math.Tan(dec * Deg) * Cos(latitude) - Sin(latitude) * Cos(hourAngle)) / Deg;
            return new SkyPosition(alt, Norm(az));
        }

        public static SkyPosition Sun(DateTime utc, double latitude, double longitude)
        {
            double jd = JulianDay(utc);
            Equatorial(SunLongitude(jd), 0, out double ra, out double dec);
            return Horizon(ra, dec, jd, latitude, longitude, out _);
        }

        public static SkyPosition MoonPosition(DateTime utc, double latitude, double longitude)
        {
            double jd = JulianDay(utc);
            MoonEcliptic(jd, out double l, out double b);
            Equatorial(l, b, out double ra, out double dec);
            return Horizon(ra, dec, jd, latitude, longitude, out _);
        }

        /// <summary>
        /// Which way the lit edge of the moon points, in degrees from straight up, turning towards the right
        /// (clockwise as you look at the moon). This is what makes the crescent lie like a bowl in Arabia and stand
        /// up more like a "C" or a backwards "C" further north. Meeus 48.5 plus the parallactic angle.
        /// </summary>
        public static double BrightLimbFromUp(DateTime utc, double latitude, double longitude)
        {
            double jd = JulianDay(utc);
            Equatorial(SunLongitude(jd), 0, out double ras, out double decs);
            MoonEcliptic(jd, out double l, out double b);
            Equatorial(l, b, out double ram, out double decm);
            // Position angle of the bright limb, from celestial north through east.
            double chi = Math.Atan2(Cos(decs) * Sin(ras - ram), Sin(decs) * Cos(decm) - Cos(decs) * Sin(decm) * Cos(ras - ram)) / Deg;
            Horizon(ram, decm, jd, latitude, longitude, out double h);
            double q = Math.Atan2(Sin(h), Math.Tan(latitude * Deg) * Cos(decm) - Sin(decm) * Cos(h)) / Deg;
            // Position angles go counter-clockwise on the sky (north towards east = left when facing south),
            // so flip to get "clockwise from up" as drawn on screen.
            return Norm(-(chi - q));
        }

        /// <summary>The six parts of the day from the sun's height and whether it is still climbing.</summary>
        public static DayPhase PhaseOf(double sunAltitude, bool morning)
        {
            if (sunAltitude < -18.0) return DayPhase.Night;
            if (morning) return sunAltitude < 0.0 ? DayPhase.Fajr : sunAltitude < 45.0 ? DayPhase.Morning : DayPhase.Noon;
            if (sunAltitude < -12.0) return DayPhase.Night;
            if (sunAltitude < 4.0) return DayPhase.Maghrib;
            return sunAltitude < 35.0 ? DayPhase.Asr : DayPhase.Noon;
        }

        public static DayPhase PhaseAt(DateTime utc, double latitude, double longitude)
        {
            var now = Sun(utc, latitude, longitude);
            var later = Sun(utc.AddMinutes(10), latitude, longitude);
            bool morning = later.Altitude > now.Altitude;
            // Near the top of the sky the sun hardly moves: call it noon.
            if (Math.Abs(later.Altitude - now.Altitude) < 0.05 && now.Altitude > 30) return DayPhase.Noon;
            return PhaseOf(now.Altitude, morning);
        }

        /// <summary>
        /// The UTC instant that matches a stage's authored time (day, sunset or night) on the date given, at the
        /// country's clock. Sunset is set half an hour before the sun goes down.
        /// </summary>
        public static DateTime StageInstant(string time, DateTime localDate, double utcOffset, double latitude, double longitude)
        {
            var midnightUtc = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-utcOffset);
            if (time == "night") return midnightUtc.AddHours(22);
            if (time != "sunset") return midnightUtc.AddHours(10.5);
            // Walk the afternoon in 5-minute steps to find when the sun reaches the horizon.
            for (int minutes = 12 * 60; minutes < 22 * 60; minutes += 5)
            {
                var t = midnightUtc.AddMinutes(minutes);
                if (Sun(t, latitude, longitude).Altitude < 0) return t.AddMinutes(-30);
            }
            return midnightUtc.AddHours(18);
        }
    }

    /// <summary>Colours for the sky dome and the light, keyed on the sun's height.</summary>
    public struct SkyLook
    {
        public float ZenithR, ZenithG, ZenithB, HorizonR, HorizonG, HorizonB, SunR, SunG, SunB;
        /// <summary>Light from the sun, 0..1.2.</summary>
        public float SunIntensity;
        /// <summary>0..1, how much of the star field and Milky Way shows.</summary>
        public float Stars, MilkyWay;

        private struct Key { public float Alt; public float[] Z, H, S; public float I; public Key(float a, float[] z, float[] h, float[] s, float i) { Alt = a; Z = z; H = h; S = s; I = i; } }

        // Night, Fajr/Isha blue hour, the orange band at the horizon, golden hour, day.
        private static readonly Key[] Keys =
        {
            new Key(-18f, new[] { 0.01f, 0.015f, 0.04f }, new[] { 0.03f, 0.04f, 0.08f }, new[] { 0.5f, 0.58f, 0.85f }, 0.0f),
            new Key(-9f, new[] { 0.04f, 0.07f, 0.18f }, new[] { 0.2f, 0.22f, 0.38f }, new[] { 0.6f, 0.62f, 0.85f }, 0.05f),
            new Key(-3f, new[] { 0.12f, 0.2f, 0.42f }, new[] { 0.85f, 0.45f, 0.32f }, new[] { 1f, 0.45f, 0.25f }, 0.2f),
            new Key(2f, new[] { 0.25f, 0.4f, 0.68f }, new[] { 1f, 0.62f, 0.38f }, new[] { 1f, 0.6f, 0.35f }, 0.55f),
            new Key(10f, new[] { 0.3f, 0.5f, 0.82f }, new[] { 0.95f, 0.8f, 0.62f }, new[] { 1f, 0.84f, 0.65f }, 0.9f),
            new Key(30f, new[] { 0.22f, 0.45f, 0.85f }, new[] { 0.7f, 0.82f, 0.95f }, new[] { 1f, 0.96f, 0.9f }, 1.15f),
        };

        public static SkyLook For(double sunAltitude, float cloudCover, float haze, float moonIllumination, bool moonUp)
        {
            float alt = (float)sunAltitude;
            int k = 0;
            while (k < Keys.Length - 2 && alt > Keys[k + 1].Alt) k++;
            var a = Keys[k]; var b = Keys[k + 1];
            float t = Clamp01((alt - a.Alt) / (b.Alt - a.Alt));
            var look = new SkyLook
            {
                ZenithR = Lerp(a.Z[0], b.Z[0], t), ZenithG = Lerp(a.Z[1], b.Z[1], t), ZenithB = Lerp(a.Z[2], b.Z[2], t),
                HorizonR = Lerp(a.H[0], b.H[0], t), HorizonG = Lerp(a.H[1], b.H[1], t), HorizonB = Lerp(a.H[2], b.H[2], t),
                SunR = Lerp(a.S[0], b.S[0], t), SunG = Lerp(a.S[1], b.S[1], t), SunB = Lerp(a.S[2], b.S[2], t),
                SunIntensity = Lerp(a.I, b.I, t),
            };
            // Overcast pulls everything to grey and dims the sun; haze and dust wash the horizon out.
            float grey = (look.ZenithR + look.ZenithG + look.ZenithB) / 3f;
            float c = Clamp01(cloudCover) * 0.8f;
            look.ZenithR = Lerp(look.ZenithR, grey, c); look.ZenithG = Lerp(look.ZenithG, grey, c); look.ZenithB = Lerp(look.ZenithB, grey * 1.05f, c);
            float hz = Clamp01(haze) * 0.7f;
            look.HorizonR = Lerp(look.HorizonR, 0.85f * Math.Max(0.2f, look.SunIntensity), hz);
            look.HorizonG = Lerp(look.HorizonG, 0.75f * Math.Max(0.2f, look.SunIntensity), hz);
            look.HorizonB = Lerp(look.HorizonB, 0.6f * Math.Max(0.2f, look.SunIntensity), hz);
            look.SunIntensity *= 1f - 0.6f * Clamp01(cloudCover) - 0.25f * Clamp01(haze);
            look.Stars = StarVisibility(sunAltitude, cloudCover, haze, moonIllumination, moonUp);
            look.MilkyWay = look.Stars * (1f - 0.8f * (moonUp ? (float)moonIllumination : 0f)) * (1f - Clamp01(haze));
            return look;
        }

        /// <summary>Stars come out as the sky darkens past -6 degrees and fade behind cloud and a bright moon.</summary>
        public static float StarVisibility(double sunAltitude, float cloudCover, float haze, double moonIllumination, bool moonUp)
        {
            float dark = Clamp01((float)(-sunAltitude - 6.0) / 10f);
            float clear = 1f - Clamp01(cloudCover);
            float moon = moonUp ? 1f - 0.5f * (float)moonIllumination : 1f;
            return dark * clear * clear * moon * (1f - 0.6f * Clamp01(haze));
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
