// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Heroes
{
    /// <summary>Aron and Noura's play stats. Lives in Resources/Heroes/heroes.json.</summary>
    [Serializable]
    public class HeroFile
    {
        public HeroDef[] heroes = Array.Empty<HeroDef>();

        public HeroDef Get(string id)
        {
            foreach (var h in heroes) if (h.id == id) return h;
            return null;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            var aron = Get("aron"); var noura = Get("noura");
            if (aron == null || noura == null) { e.Add("needs aron and noura"); return e; }
            if (noura.radius >= aron.radius) e.Add("Noura is slimmer than Aron, so she fits gaps he can't");
            if (noura.speed <= aron.speed) e.Add("Noura is quicker than Aron");
            if (noura.jumps < 2) e.Add("Noura double jumps");
            if (noura.glideTime <= 0f) e.Add("Noura glides on her abaya");
            if (!noura.opensLocks) e.Add("Noura opens locked doors and gates");
            if (noura.companion != CompanionIds.Oryx) e.Add("Noura's companion is the oryx");
            if (!aron.rollsSleeves) e.Add("Aron rolls up his sleeves for a fight");
            if (noura.rollsSleeves) e.Add("Noura never rolls up her sleeves: her arms stay covered");
            foreach (var h in heroes)
                if (h.speed <= 0f || h.radius <= 0f || h.jumps < 1) e.Add(h.id + ": speed, radius and jumps must be positive");
            return e;
        }
    }

    [Serializable]
    public class HeroDef
    {
        public string id;
        public string nameAr;
        /// <summary>Run speed multiplier on top of the player config.</summary>
        public float speed = 1f;
        /// <summary>Capsule radius. A gap narrower than twice this plus a little room blocks the hero.</summary>
        public float radius = 0.4f;
        public int jumps = 2;
        /// <summary>Seconds of slow fall after the last jump while the finger stays down.</summary>
        public float glideTime;
        /// <summary>Fall speed while gliding, in m/s.</summary>
        public float glideFall = 1.5f;
        public bool opensLocks;
        public bool seesSecrets;
        public string weapon = "agal";
        /// <summary>Handbag boomerang: how far it flies out and how long the round trip takes.</summary>
        public float throwRange;
        public float throwTime = 0.9f;
        public string companion = CompanionIds.Falcon;
        /// <summary>Aron rolls his thobe sleeves up for a fight. Noura never does: her abaya covers her arms at all times.</summary>
        public bool rollsSleeves;
    }

    /// <summary>Who's playing and who's tagging along. Swap by tapping the partner, never with a button.</summary>
    public class HeroSwitch
    {
        public float Cooldown { get; set; } = 0.6f;
        /// <summary>The partner has to be on screen and close enough to tap.</summary>
        public float Range { get; set; } = 12f;
        public string Active { get; private set; }
        public string Partner { get; private set; }
        private float last = -10f;

        public HeroSwitch(string active = "aron", string partner = "noura") { Active = active; Partner = partner; }

        public bool CanSwitch(float now, float partnerDistance, bool busy)
            => !busy && now - last >= Cooldown && partnerDistance >= 0f && partnerDistance <= Range;

        public bool TrySwitch(float now, float partnerDistance, bool busy)
        {
            if (!CanSwitch(now, partnerDistance, busy)) return false;
            (Active, Partner) = (Partner, Active);
            last = now;
            return true;
        }
    }

    /// <summary>Jump counting plus Noura's abaya glide after her last jump.</summary>
    public class JumpGlide
    {
        private readonly HeroDef hero;
        public int JumpsUsed { get; private set; }
        public bool Gliding { get; private set; }
        public float GlideLeft { get; private set; }

        public JumpGlide(HeroDef hero) { this.hero = hero; }

        public bool TryJump(bool grounded)
        {
            if (grounded) { JumpsUsed = 0; GlideLeft = hero.glideTime; Gliding = false; }
            if (JumpsUsed >= hero.jumps) return false;
            JumpsUsed++;
            Gliding = false;
            return true;
        }

        /// <summary>Returns the fastest the hero may fall this frame (negative), or null for normal gravity.</summary>
        public float? Tick(float dt, bool grounded, bool fingerDown, float verticalSpeed)
        {
            if (grounded) { JumpsUsed = 0; GlideLeft = hero.glideTime; Gliding = false; return null; }
            Gliding = fingerDown && JumpsUsed >= hero.jumps && GlideLeft > 0f && verticalSpeed < 0f;
            if (!Gliding) return null;
            GlideLeft = Math.Max(0f, GlideLeft - dt);
            return -hero.glideFall;
        }
    }

    /// <summary>Handbag thrown out and back like a boomerang. Distance along the throw for any moment.</summary>
    public class Boomerang
    {
        private readonly float range, time;
        private float thrownAt = -10f;
        public bool Out { get; private set; }

        public Boomerang(float range, float time) { this.range = range; this.time = Math.Max(0.1f, time); }

        public bool TryThrow(float now)
        {
            if (Out) return false;
            thrownAt = now; Out = true;
            return true;
        }

        /// <summary>0 at the hand, range at the far point, back to 0. Ends the throw once it's home.</summary>
        public float Distance(float now)
        {
            if (!Out) return 0f;
            float t = (now - thrownAt) / time;
            if (t >= 1f) { Out = false; return 0f; }
            return range * (float)Math.Sin(t * Math.PI);
        }

        public bool Returning(float now) => Out && (now - thrownAt) / time > 0.5f;
    }

    public enum CoopKind
    {
        /// <summary>Aron cups his hands and boosts Noura up; she then helps him from the top.</summary>
        BoostUp,
        /// <summary>Noura is on top already and pulls Aron up.</summary>
        PullUp,
        /// <summary>A gap only Noura fits through; she opens the gate on the far side for Aron.</summary>
        NarrowGap,
        /// <summary>A locked gate Noura picks open.</summary>
        LockedGate,
    }

    /// <summary>Co-op beats that need both heroes.</summary>
    public static class Coop
    {
        public const float Reach = 2.2f;

        /// <summary>Both heroes have to be standing at the ledge or gate for it to work.</summary>
        public static bool Ready(CoopKind kind, float aronDistance, float nouraDistance, string active)
        {
            switch (kind)
            {
                case CoopKind.BoostUp: return aronDistance <= Reach && nouraDistance <= Reach;
                case CoopKind.PullUp: return aronDistance <= Reach && nouraDistance <= Reach + 1.5f;
                case CoopKind.NarrowGap:
                case CoopKind.LockedGate: return active == "noura" && nouraDistance <= Reach;
                default: return false;
            }
        }

        /// <summary>Who gets lifted, or null if nobody moves.</summary>
        public static string Lifted(CoopKind kind) => kind == CoopKind.BoostUp ? "noura" : kind == CoopKind.PullUp ? "aron" : null;

        /// <summary>Whether this hero fits through a gap of the given width.</summary>
        public static bool Fits(HeroDef hero, float gapWidth) => gapWidth >= hero.radius * 2f + 0.15f;
    }
}
