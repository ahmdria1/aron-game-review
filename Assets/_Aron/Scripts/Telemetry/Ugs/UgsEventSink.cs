// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aron.Logic.Telemetry;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Aron.Telemetry
{
    /// <summary>
    /// Sends the queued events through Unity Analytics (package com.unity.services.analytics 6.1 or newer, Unity 6.2 or newer).
    /// The SDK is switched on (consent state "granted") only for the length of one send and switched off again straight after,
    /// so between batches nothing leaves the phone, not even the SDK's own session events. Only the checked events are recorded
    /// here, with their schema parameters and nothing else. No advertising id is read and none is set.
    /// What Unity itself adds to every event is listed in docs/privacy_policy.md.
    /// </summary>
    public sealed class UgsEventSink : IEventSink
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { if (TelemetryService.Sink == null) TelemetryService.Sink = new UgsEventSink(); }

        public IEnumerator Send(List<AnalyticsEvent> batch, Action<bool> done)
        {
            var task = SendAsync(batch);
            while (!task.IsCompleted) yield return null;
            done(!task.IsFaulted && !task.IsCanceled && task.Result);
        }

        private static async Task<bool> SendAsync(List<AnalyticsEvent> batch)
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                SetIntent(ConsentStatus.Granted);
                foreach (var e in batch)
                {
                    var ce = new CustomEvent(e.name);
                    foreach (var p in e.p)
                    {
                        if (p.isNumber) ce.Add(p.k, (int)p.i); else ce.Add(p.k, p.s);
                    }
                    AnalyticsService.Instance.RecordEvent(ce);
                }
                AnalyticsService.Instance.Flush();
                return true;
            }
            catch (Exception) { return false; }
            finally { SetIntent(ConsentStatus.Denied); }
        }

        private static void SetIntent(ConsentStatus status)
        {
            var state = EndUserConsent.GetConsentState();
            state.AnalyticsIntent = status;
            EndUserConsent.SetConsentState(state);
        }

        public void RequestDeletion() => _ = DeleteAsync();

        private static async Task DeleteAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                SetIntent(ConsentStatus.Denied);          // a deletion request is only accepted after consent is denied
                AnalyticsService.Instance.RequestDataDeletion();   // the SDK keeps retrying it across sessions
            }
            catch (Exception) { /* offline or services not set up: nothing was collected without consent in the first place */ }
        }
    }
}
