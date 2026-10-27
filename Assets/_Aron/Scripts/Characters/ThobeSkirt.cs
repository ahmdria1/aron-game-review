// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Characters
{
    /// <summary>
    /// The lower half of a thobe, rebuilt every frame around the legs. When the wearer sprints he reaches down
    /// for the front hem with his right hand, lifts it and bites the edge, the way men run in a thobe, then runs
    /// with both arms pumping and his sirwal and shoes showing. Slowing down, he lets go and the hem drops.
    /// The hem edge vertices follow the mouth (and the hand on the way up); see <see cref="HemShape"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ThobeSkirt : MonoBehaviour
    {
        private const int Segments = 20;
        private const int Rings = 5;

        [SerializeField] private Transform body;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform head;
        [SerializeField] private Transform mouth;
        [SerializeField] private Transform footL;
        [SerializeField] private Transform footR;
        [SerializeField] private float waistHeight = 0.78f;
        [SerializeField] private float hemHeight = 0.1f;
        [SerializeField] private float waistRadius = 0.24f;
        [SerializeField] private float hemRadius = 0.3f;
        [SerializeField] private float headDip = 14f;
        [Tooltip("Without a PlayerController above it, the hem goes up after running this fast for a while.")]
        [SerializeField] private float autoSprintSpeed = 6f;

        private Mesh mesh;
        private Vector3[] verts;
        private PlayerController runner;
        private Vector3 lastBodyPos;
        private float fastTime;
        private readonly HemGrab grab = new HemGrab();
        private Transform armR, foreR, fistR, armL, foreL, fistL;
        private float pumpPhase;
        private Transform spine;

        public float Lift => grab.Raise;
        public HemGrab Grab => grab;

        public void Setup(Transform body, Transform hips, Transform head, Transform mouth, Transform footL, Transform footR,
                          float waistHeight, float hemHeight, float waistRadius, float hemRadius)
        {
            this.body = body; this.hips = hips; this.head = head; this.mouth = mouth;
            this.footL = footL; this.footR = footR;
            this.waistHeight = waistHeight; this.hemHeight = hemHeight;
            this.waistRadius = waistRadius; this.hemRadius = hemRadius;
        }

        private void Awake()
        {
            if (body == null) body = transform.parent != null ? transform.parent : transform;
            runner = GetComponentInParent<PlayerController>();
            armR = Find(body, "UpperArm.R"); foreR = Find(body, "LowerArm.R"); fistR = Find(body, "Fist.R");
            spine = Find(body, "Abdomen");
            armL = Find(body, "UpperArm.L"); foreL = Find(body, "LowerArm.L"); fistL = Find(body, "Fist.L");
            BuildMesh();
            lastBodyPos = body.position;
        }

        private void BuildMesh()
        {
            int ring = Segments + 1;
            int n = Rings * ring;
            verts = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var tris = new int[(Rings - 1) * Segments * 12];
            int k = 0;
            for (int r = 0; r < Rings - 1; r++)
                for (int s = 0; s < Segments; s++)
                {
                    int a = r * ring + s, b = a + 1, c = a + ring, d = c + 1;
                    tris[k++] = a; tris[k++] = c; tris[k++] = b;
                    tris[k++] = b; tris[k++] = c; tris[k++] = d;
                    tris[k++] = n + a; tris[k++] = n + b; tris[k++] = n + c;
                    tris[k++] = n + b; tris[k++] = n + d; tris[k++] = n + c;
                }
            for (int r = 0; r < Rings; r++)
                for (int s = 0; s <= Segments; s++)
                    uv[r * ring + s] = uv[n + r * ring + s] = new Vector2(s / (float)Segments, 1f - r / (float)(Rings - 1));

            mesh = new Mesh { name = "Thobe" };
            mesh.MarkDynamic();
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static V3 L(Vector3 v) => new V3(v.x, v.y, v.z);
        private static Vector3 U(V3 v) => new Vector3(v.x, v.y, v.z);

        private bool Sprinting()
        {
            if (runner != null) return runner.IsSprinting && !runner.IsDead;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 step = body.position - lastBodyPos;
            step.y = 0f;
            lastBodyPos = body.position;
            fastTime = step.magnitude / dt > autoSprintSpeed ? fastTime + dt : 0f;
            return fastTime > 1.2f;
        }

        private void LateUpdate()
        {
            if (body == null || hips == null) return;
            grab.Tick(Sprinting(), Time.deltaTime);
            float raise = grab.Raise;

            Vector3 up = Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(body.forward, up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            Vector3 right = Vector3.Cross(up, fwd);
            float scale = body.lossyScale.y;

            // He bends forward to get the hem, straightening as it comes up.
            float bend = grab.Hand * (1f - raise) * 38f;
            if (spine != null && bend > 0.01f) spine.rotation = Quaternion.AngleAxis(bend, right) * spine.rotation;

            // Chin down a touch while the teeth hold the cloth.
            float bite = grab.Bitten ? 1f : Mathf.Clamp01((raise - 0.7f) / 0.3f);
            if (head != null && bite > 0.001f) head.rotation = Quaternion.AngleAxis(headDip * bite, right) * head.rotation;

            Vector3 ground = body.position;
            Vector3 waist = new Vector3(hips.position.x, ground.y + waistHeight * scale, hips.position.z);
            Vector3 hem = new Vector3(waist.x, ground.y + hemHeight * scale, waist.z);
            if (footL != null && footR != null)
            {
                Vector3 feet = (footL.position + footR.position) * 0.5f;
                hem.x = Mathf.Lerp(waist.x, feet.x, 0.6f);
                hem.z = Mathf.Lerp(waist.z, feet.z, 0.6f);
            }
            Vector3 mouthAt = mouth != null ? mouth.position : head != null ? head.position + fwd * 0.12f * scale : waist + up * 0.6f * scale;
            mouthAt += fwd * 0.04f * scale - up * 0.02f * scale;

            int ring = Segments + 1;
            int n = Rings * ring;
            for (int r = 0; r < Rings; r++)
            {
                float t = r / (float)(Rings - 1);
                float radius = Mathf.Lerp(waistRadius, hemRadius, t) * scale;
                Vector3 centre = Vector3.Lerp(waist, hem, t);
                for (int s = 0; s <= Segments; s++)
                {
                    float angle = s / (float)Segments * Mathf.PI * 2f;
                    float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                    Vector3 dir = fwd * cos + right * sin;
                    Vector3 p = centre + dir * radius;
                    if (raise > 0.001f)
                    {
                        float front = Mathf.Max(0f, cos);
                        front = front * front * front;
                        // The bitten edge is a few centimetres wide, not a point.
                        Vector3 held = mouthAt + right * sin * 0.06f * scale;
                        p = U(HemShape.Point(t, front, L(p), L(waist + dir * waistRadius * scale), L(hem + dir * hemRadius * scale),
                                             L(held), L(up), L(fwd), raise, scale));
                    }
                    Vector3 local = transform.InverseTransformPoint(p);
                    verts[r * ring + s] = local;
                    verts[n + r * ring + s] = local;
                }
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            PoseArms(waist, hem, mouthAt, up, fwd, right, scale);
        }

        /// <summary>Right hand down to the hem and up with it; then both arms swing with the run.</summary>
        private void PoseArms(Vector3 waist, Vector3 hem, Vector3 mouthAt, Vector3 up, Vector3 fwd, Vector3 right, float scale)
        {
            if (grab.Hand > 0.001f && armR != null && foreR != null && fistR != null)
            {
                Vector3 front = U(HemShape.Point(1f, 1f, L(hem + fwd * hemRadius * scale), L(waist + fwd * waistRadius * scale),
                                                 L(hem + fwd * hemRadius * scale), L(mouthAt), L(up), L(fwd), grab.Raise, scale));
                Reach(armR, foreR, fistR, front, right + fwd * 0.3f - up * 0.2f, grab.Hand);
            }
            if (grab.Pump > 0.001f)
            {
                float speed = runner != null ? runner.Velocity.magnitude : 8f;
                pumpPhase += Time.deltaTime * Mathf.Clamp(speed * 1.3f, 6f, 16f);
                float swing = Mathf.Sin(pumpPhase);
                // Bent at the elbow, fists coming forward to chest height and back past the hip.
                if (armR != null && foreR != null && fistR != null)
                    Reach(armR, foreR, fistR, Pumped(armR.position, swing, up, fwd, right, scale, 1f), -up + right * 0.4f - fwd * 0.4f, grab.Pump);
                if (armL != null && foreL != null && fistL != null)
                    Reach(armL, foreL, fistL, Pumped(armL.position, -swing, up, fwd, right, scale, -1f), -up - right * 0.4f - fwd * 0.4f, grab.Pump);
            }
        }

        private static Vector3 Pumped(Vector3 shoulder, float swing, Vector3 up, Vector3 fwd, Vector3 right, float scale, float side)
        {
            return shoulder + fwd * (0.24f * swing) * scale - up * (0.22f - 0.12f * Mathf.Max(0f, swing)) * scale + right * side * 0.06f * scale;
        }

        /// <summary>Two-bone reach: shoulder, elbow, fist toward a point, the elbow bending toward the hint.</summary>
        private static void Reach(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint, float weight)
        {
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float la = (b - a).magnitude, lb = (c - b).magnitude;
            if (la < 1e-4f || lb < 1e-4f) return;
            Vector3 to = target - a;
            float d = Mathf.Clamp(to.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f);
            Vector3 dir = to.normalized;
            Vector3 axis = Vector3.Cross(dir, hint);
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(dir, Vector3.up);
            axis.Normalize();
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f);
            Vector3 elbow = a + Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, axis) * dir * la;
            Vector3 wrist = a + dir * d;

            Quaternion upperAim = Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation;
            upper.rotation = Quaternion.Slerp(upper.rotation, upperAim, weight);
            b = lower.position; c = end.position;
            Quaternion lowerAim = Quaternion.FromToRotation(c - b, Vector3.Lerp(c, wrist, weight) - b) * lower.rotation;
            lower.rotation = lowerAim;
        }
    }
}
