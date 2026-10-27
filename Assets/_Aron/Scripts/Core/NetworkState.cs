// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Online;
using Aron.Logic.Performance;
using Aron.Save;
using UnityEngine;

namespace Aron.Core
{
    /// <summary>What kind of connection the phone has, and whether a given kind of traffic may use it right now.</summary>
    public static class NetworkState
    {
        public static readonly DataGate Gate = new DataGate();
        public static readonly DataMeter Meter = new DataMeter();

        public static NetworkKind Current
        {
            get
            {
                switch (Application.internetReachability)
                {
                    case NetworkReachability.ReachableViaLocalAreaNetwork: return NetworkKind.Wifi;
                    case NetworkReachability.ReachableViaCarrierDataNetwork: return NetworkKind.Mobile;
                    default: return NetworkKind.None;
                }
            }
        }

        public static PowerSettings Settings
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.power == null) d.power = new PowerSettings();
                return d.power;
            }
        }

        public static bool Allow(Traffic kind) => Gate.Allow(kind, Current, Settings, DeviceSettings.Mode);

        public static void NoteSent(Traffic kind) => Gate.NoteSent(kind, Current);
    }

    /// <summary>Everything that can give memory back registers here; DeviceSettings trims it on a low-memory warning.</summary>
    public static class Caches
    {
        public static readonly Aron.Logic.Performance.CacheRegistry Registry = new Aron.Logic.Performance.CacheRegistry();
    }

    /// <summary>The current quality step (see QualityLadder), for effects that scale themselves.</summary>
    public static class QualityBus
    {
        public static QualityPlan Current { get; private set; } = QualityLadder.Plan(0);
        public static event System.Action<QualityPlan> Changed;

        public static void Set(QualityPlan plan)
        {
            Current = plan;
            Changed?.Invoke(plan);
        }
    }
}
