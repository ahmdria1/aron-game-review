// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>
    /// For a boss placed by hand in a scene: swaps the placeholder model for one of the built bodies when the
    /// scene loads. Stage scenes don't need it, StageBuilder dresses their boss from the region file.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BossLook : MonoBehaviour
    {
        [SerializeField] private string model = "scientist";
        [SerializeField] private string headgear = "none";
        [SerializeField] private Color body = new Color(0.3f, 0.32f, 0.25f);
        [SerializeField] private Color trim = new Color(0.85f, 0.7f, 0.25f);
        [SerializeField] private Color glow = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Material material;

        private void Awake()
        {
            var built = BossBodies.Build(model, headgear, body, trim, glow, material);
            if (built == null) return;
            var old = transform.Find("Model");
            if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            built.transform.SetParent(transform, false);
            built.transform.localPosition = new Vector3(0f, -1.6f, 0f);
            var boss = GetComponent<RiyadhBoss>();
            if (boss != null) boss.UseBody(null);
        }
    }
}
