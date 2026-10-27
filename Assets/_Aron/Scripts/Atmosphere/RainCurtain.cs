// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>
    /// Rain or snow as one GPU-animated mesh (Aron/Rain). The mesh holds the most drops the phone may draw; the
    /// <see cref="Amount"/> slider hides the rest in the shader, so changing the weather never rebuilds anything.
    /// </summary>
    public class RainCurtain : MonoBehaviour
    {
        private static readonly int AmountId = Shader.PropertyToID("_Amount"), CenterId = Shader.PropertyToID("_Center"),
            WindId = Shader.PropertyToID("_WindVec"), LightId = Shader.PropertyToID("_Light");

        private Material mat;
        private MeshRenderer mr;
        private Vector3 box;
        public bool Snow { get; private set; }

        /// <summary>Returns null when the shader is missing from the build, so the caller can fall back to particles.</summary>
        public static RainCurtain Create(Transform parent, int drops, bool snow)
        {
            var shader = Shader.Find("Aron/Rain");
            if (shader == null || drops <= 0) return null;
            var go = new GameObject(snow ? "SnowCurtain" : "RainCurtain", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<RainCurtain>();
            c.Snow = snow;
            c.box = snow ? new Vector3(28f, 16f, 34f) : new Vector3(24f, 16f, 30f);
            c.mat = new Material(shader) { name = go.name };
            if (snow)
            {
                c.mat.EnableKeyword("_ARON_SNOW");
                c.mat.SetColor("_Color", new Color(1f, 1f, 1f, 0.9f));
                c.mat.SetFloat("_Fall", 1.4f);
                c.mat.SetVector("_Size", new Vector4(0.06f, 0.06f, 0f, 0f));
            }
            else
            {
                c.mat.SetColor("_Color", new Color(0.75f, 0.8f, 0.9f, 0.45f));
                c.mat.SetFloat("_Fall", 11f);
                c.mat.SetVector("_Size", new Vector4(0.012f, 0.5f, 0f, 0f));
            }
            c.mat.SetVector("_Box", c.box);
            go.GetComponent<MeshFilter>().sharedMesh = BuildMesh(drops, c.box);
            c.mr = go.GetComponent<MeshRenderer>();
            c.mr.sharedMaterial = c.mat;
            c.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c.mr.receiveShadows = false;
            c.Amount = 0f;
            return c;
        }

        private static Mesh BuildMesh(int drops, Vector3 box)
        {
            var v = new Vector3[drops * 4];
            var uv = new Vector2[drops * 4];
            var seed = new Vector2[drops * 4];
            var tri = new int[drops * 6];
            var rnd = new System.Random(1234);
            for (int i = 0; i < drops; i++)
            {
                // Shuffle the ranks so any amount takes drops from all over the box, not one corner.
                float s = (float)rnd.NextDouble() * 100f;
                float rank = (i + 0.5f) / drops;
                for (int k = 0; k < 4; k++)
                {
                    v[i * 4 + k] = Vector3.zero;
                    seed[i * 4 + k] = new Vector2(s, rank);
                }
                uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(0, 1); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(1, 0);
                int b = i * 4, t = i * 6;
                tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 2; tri[t + 3] = b; tri[t + 4] = b + 2; tri[t + 5] = b + 3;
            }
            var m = new Mesh { name = "RainMesh", indexFormat = drops * 4 > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.vertices = v; m.uv = uv; m.uv2 = seed; m.triangles = tri;
            // The vertices are placed in the shader; the bounds just have to cover the box around the camera.
            m.bounds = new Bounds(Vector3.zero, box * 4f);
            return m;
        }

        private float amount;
        /// <summary>0..1 of the drops in the mesh.</summary>
        public float Amount
        {
            get => amount;
            set
            {
                amount = Mathf.Clamp01(value);
                if (mat != null) mat.SetFloat(AmountId, amount);
                if (mr != null) mr.enabled = amount > 0.001f;
            }
        }

        public void SetWind(Vector3 push) { if (mat != null) mat.SetVector(WindId, push); }
        public void SetLight(float light) { if (mat != null) mat.SetFloat(LightId, light); }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || mat == null) return;
            // Centre the box a little ahead of the camera, where the player looks.
            var c = cam.transform.position + cam.transform.forward * (box.z * 0.35f);
            transform.position = c;   // keeps the renderer's bounds around the camera
            mat.SetVector(CenterId, c);
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
        }
    }
}
