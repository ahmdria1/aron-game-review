// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>Keeps a 1920x1080 layout whole on any aspect, from 4:3 tablets to 21:9 phones.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvas : MonoBehaviour
    {
        private CanvasScaler scaler;
        private Vector2Int size;

        private void Awake() => scaler = GetComponent<CanvasScaler>();

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.width != size.x || Screen.height != size.y) Apply();
        }

        private void Apply()
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            size = new Vector2Int(Screen.width, Screen.height);
            scaler.matchWidthOrHeight = ScreenFit.CanvasMatch(size.x, size.y);
        }
    }
}
