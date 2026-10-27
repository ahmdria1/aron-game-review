// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using System.Collections.Generic;
using Aron.Audio;
using Aron.Core;
using Aron.Fx;
using Aron.Heroes;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Regions;
using Aron.Player;
using Aron.UI;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// Builds the puzzle sections from the layout: a barred gate across the track and the levers, rune plates,
    /// switch or crate that open it. The rules live in <see cref="LeverPuzzle"/>, <see cref="RunePuzzle"/>,
    /// <see cref="TimedDoor"/> and <see cref="CratePush"/>; these pieces only show them.
    /// </summary>
    public static class StagePuzzles
    {
        private static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color Iron = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color Stone = new Color(0.62f, 0.57f, 0.5f);

        public static PuzzleGate Gate(StageArt art, Transform parent, StageLayout layout, Piece g)
        {
            float hw = layout.HalfWidth;
            var root = StageArt.Group("PuzzleGate_" + g.tag, parent, new Vector3(0f, g.y, g.z));
            foreach (float side in new[] { -1f, 1f })
            {
                art.Box(root, new Vector3(side * (hw + 0.2f), 2f, 0f), new Vector3(0.6f, 4f, 0.6f), Stone);
                art.Box(root, new Vector3(side * (hw + 0.2f), 4.15f, 0f), new Vector3(0.8f, 0.3f, 0.8f), Stone * 0.85f);
            }
            art.Box(root, new Vector3(0f, 3.8f, 0f), new Vector3(hw * 2f + 0.4f, 0.35f, 0.4f), Wood);
            var bars = StageArt.Group("Bars", root, Vector3.zero);
            for (float x = -hw + 0.3f; x <= hw - 0.3f + 0.01f; x += 0.55f)
                art.Box(bars, new Vector3(x, 1.8f, 0f), new Vector3(0.12f, 3.6f, 0.12f), Iron);
            art.Box(bars, new Vector3(0f, 1.2f, 0f), new Vector3(hw * 2f, 0.14f, 0.14f), Iron);
            art.Box(bars, new Vector3(0f, 2.6f, 0f), new Vector3(hw * 2f, 0.14f, 0.14f), Iron);
            var block = bars.gameObject.AddComponent<BoxCollider>();
            block.center = new Vector3(0f, 1.8f, 0f);
            block.size = new Vector3(hw * 2f, 3.6f, 0.5f);

            if (g.tag == PuzzleKinds.Lock)
            {
                // Noura's lock: the existing gate piece does the picking.
                art.Box(root, new Vector3(0f, 1.3f, -0.15f), new Vector3(0.5f, 0.6f, 0.2f), new Color(0.75f, 0.6f, 0.2f));
                root.gameObject.AddComponent<LockedGate>().Setup(bars);
                return null;
            }
            var gate = root.gameObject.AddComponent<PuzzleGate>();
            gate.Setup(bars, g.tag, g.sizeA);
            return gate;
        }

        public static void Part(StageArt art, Transform parent, Piece p, PuzzleGate gate)
        {
            if (gate == null) return;
            var at = new Vector3(p.x, p.y, p.z);
            switch (p.tag)
            {
                case PuzzleKinds.Lever:
                {
                    var root = StageArt.Group("Lever", parent, at);
                    art.Box(root, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.8f, 0.5f), Stone);
                    var arm = StageArt.Group("Arm", root, new Vector3(0f, 0.8f, 0f));
                    art.Box(arm, new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 1f, 0.1f), Wood);
                    art.Ball(arm, new Vector3(0f, 1f, 0f), Vector3.one * 0.22f, new Color(0.8f, 0.15f, 0.12f));
                    arm.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                    var col = root.gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 0.8f, 0f); col.size = new Vector3(0.7f, 1.6f, 0.6f);
                    root.gameObject.AddComponent<PuzzleLever>().Setup(gate, (int)p.sizeA, arm);
                    break;
                }
                case PuzzleKinds.Runes:
                {
                    var root = StageArt.Group("RunePlate", parent, at);
                    var plate = art.Cyl(root, Vector3.zero, 0.55f, 0.08f, Stone * 0.9f);
                    art.Box(root, new Vector3(0f, 0.09f, 0f), new Vector3(0.5f, 0.02f, 0.12f), new Color(0.2f, 0.55f, 0.6f), 45f);
                    art.Box(root, new Vector3(0f, 0.09f, 0f), new Vector3(0.5f, 0.02f, 0.12f), new Color(0.2f, 0.55f, 0.6f), -45f);
                    root.gameObject.AddComponent<RunePlate>().Setup(gate, (int)p.sizeA, plate.GetComponent<Renderer>(), Stone * 0.9f);
                    break;
                }
                case PuzzleKinds.Timed:
                {
                    var root = StageArt.Group("TimedSwitch", parent, at);
                    art.Cyl(root, Vector3.zero, 0.6f, 0.1f, Stone);
                    var button = art.Cyl(root, new Vector3(0f, 0.1f, 0f), 0.35f, 0.12f, new Color(0.85f, 0.65f, 0.15f));
                    root.gameObject.AddComponent<TimedSwitch>().Setup(gate, button.transform);
                    break;
                }
                case PuzzleKinds.Crate:
                {
                    if (p.sizeA < 0.5f)
                    {
                        var root = StageArt.Group("PushCrate", parent, at);
                        art.Box(root, new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), Wood);
                        art.Box(root, new Vector3(0f, 0.6f, 0f), new Vector3(1.24f, 0.16f, 1.24f), Wood * 0.7f);
                        var col = root.gameObject.AddComponent<BoxCollider>();
                        col.center = new Vector3(0f, 0.6f, 0f); col.size = new Vector3(1.2f, 1.2f, 1.2f);
                        root.gameObject.AddComponent<PushCrate>().Setup(gate);
                    }
                    else
                    {
                        var root = StageArt.Group("CratePlate", parent, at);
                        art.Box(root, new Vector3(0f, 0.03f, 0f), new Vector3(1.4f, 0.06f, 1.4f), new Color(0.75f, 0.6f, 0.2f));
                        gate.PlateAt(at.z);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>The barred gate. Opens when its puzzle is solved; a timed one shuts again when the time runs out.</summary>
    public class PuzzleGate : MonoBehaviour
    {
        private Transform bars;
        private Collider block;
        private string kind;
        private float setting;
        private bool hinted, open;

        public LeverPuzzle Levers { get; private set; }
        public RunePuzzle Runes { get; private set; }
        public TimedDoor Door { get; private set; }
        public CratePush Push { get; private set; }
        public string Kind => kind;
        public bool Solved { get; private set; }
        public readonly List<RunePlate> Plates = new List<RunePlate>();
        private float plateZ = float.NaN;
        private int lastTick = -1;

        public void Setup(Transform b, string k, float s)
        {
            bars = b; kind = k; setting = s;
            block = b.GetComponent<Collider>();
            switch (kind)
            {
                case PuzzleKinds.Lever: Levers = new LeverPuzzle((int)s); break;
                case PuzzleKinds.Runes: Runes = new RunePuzzle((int)s); break;
                case PuzzleKinds.Timed: Door = new TimedDoor(s); break;
            }
        }

        public void PlateAt(float z) => plateZ = z;

        public CratePush CrateFrom(float z)
        {
            if (Push == null) Push = new CratePush(z, float.IsNaN(plateZ) ? z + setting : plateZ);
            return Push;
        }

        public void Report(PuzzleStep step)
        {
            switch (step)
            {
                case PuzzleStep.Progress: GameEvents.PlaySfx(AudioCues.PuzzleStep); break;
                case PuzzleStep.Wrong:
                    GameEvents.PlaySfx(AudioCues.PuzzleWrong);
                    GameUI.Banner(Strings.Get("puzzle_wrong"), 1.4f);
                    AudioManager.PlayVoice(AudioCues.VoPuzzleWrong);
                    foreach (var p in Plates) p.Lit(false);
                    if (Runes != null) StartCoroutine(ShowOrder(0.6f));
                    break;
                case PuzzleStep.Solved: Solve(); break;
            }
        }

        private void Solve()
        {
            if (Solved) return;
            Solved = true;
            SetOpen(true);
            GameEvents.PlaySfx(AudioCues.PuzzleSolved);
            GameUI.Banner(Strings.Get("puzzle_solved"), 1.6f);
            AudioManager.Bark(AudioCues.VoPuzzleSolved, AudioCues.VoNouraPuzzleSolved);
            GameEvents.RaisePuzzleSolved(kind);
        }

        private void SetOpen(bool on)
        {
            if (open == on) return;
            open = on;
            if (block != null) block.enabled = !on;
            if (on) { GameEvents.PlaySfx(AudioCues.GateOpen); FxPlayer.Play("dust", transform.position + Vector3.up); }
        }

        /// <summary>The rune plates light up one after another in the order to step on them.</summary>
        public IEnumerator ShowOrder(float delay)
        {
            yield return new WaitForSeconds(delay);
            var order = new List<RunePlate>(Plates);
            order.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            foreach (var p in order)
            {
                if (Solved) yield break;
                p.Lit(true);
                GameEvents.PlaySfx(AudioCues.PuzzleStep);
                yield return new WaitForSeconds(0.55f);
                p.Lit(false);
                yield return new WaitForSeconds(0.15f);
            }
        }

        private void Update()
        {
            var p = PlayerController.Active;
            if (p == null) return;
            float ahead = transform.position.z - p.transform.position.z;
            if (!hinted && ahead > 0f && ahead < 24f)
            {
                hinted = true;
                GameUI.Banner(Strings.Get("puzzle_" + kind), 2.4f);
                if (Runes != null) StartCoroutine(ShowOrder(0.8f));
            }
            if (Door != null)
            {
                bool on = Door.IsOpen(Time.time) || ahead < -1.5f;
                SetOpen(on);
                int left = Mathf.CeilToInt(Door.Left(Time.time));
                if (Door.IsOpen(Time.time) && left != lastTick) { lastTick = left; GameEvents.PlaySfx(AudioCues.TimerTick); }
            }
        }

        private void LateUpdate()
        {
            if (bars == null) return;
            float target = open ? -3.7f : 0f;
            var lp = bars.localPosition;
            lp.y = Mathf.MoveTowards(lp.y, target, (open ? 4f : 7f) * Time.deltaTime);
            bars.localPosition = lp;
        }
    }

    internal static class PuzzleTouch
    {
        public static bool Near(Transform t, float reach)
        {
            var p = PlayerController.Active;
            if (p == null || p.IsDead) return false;
            var d = p.transform.position - t.position;
            d.y = Mathf.Max(0f, Mathf.Abs(d.y) - 0.5f);
            return d.sqrMagnitude < reach * reach;
        }
    }

    /// <summary>Hit it or run into it and it flips.</summary>
    public class PuzzleLever : MonoBehaviour, IDamageable
    {
        private PuzzleGate gate;
        private int index;
        private Transform arm;
        private bool pulled;

        public bool CanBeDamaged => !pulled;
        public void Setup(PuzzleGate g, int i, Transform a) { gate = g; index = i; arm = a; }
        public void TakeDamage(DamageInfo info) => Pull();

        private void Update()
        {
            if (!pulled && PuzzleTouch.Near(transform, 1.1f)) Pull();
            if (arm != null) arm.localRotation = Quaternion.RotateTowards(arm.localRotation, Quaternion.Euler(pulled ? 35f : -35f, 0f, 0f), 300f * Time.deltaTime);
        }

        private void Pull()
        {
            if (pulled || gate == null) return;
            pulled = true;
            GameEvents.PlaySfx(AudioCues.Lever);
            gate.Report(gate.Levers.Pull(index));
        }
    }

    /// <summary>A rune plate: step on it. Lights while it's part of a right run.</summary>
    public class RunePlate : MonoBehaviour
    {
        private PuzzleGate gate;
        private Renderer plate;
        private Color baseColor;
        private bool standing;
        private MaterialPropertyBlock block;
        public int Rank { get; private set; }

        public void Setup(PuzzleGate g, int rank, Renderer r, Color c)
        {
            gate = g; Rank = rank; plate = r; baseColor = c;
            g.Plates.Add(this);
        }

        public void Lit(bool on)
        {
            if (plate == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            plate.GetPropertyBlock(block);
            var c = on ? new Color(0.35f, 0.95f, 1f) : baseColor;
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            plate.SetPropertyBlock(block);
        }

        private void Update()
        {
            if (gate == null || gate.Solved) return;
            bool on = PuzzleTouch.Near(transform, 0.7f);
            if (on && !standing)
            {
                var step = gate.Runes.Step(Rank);
                if (step == PuzzleStep.Progress || step == PuzzleStep.Solved) Lit(true);
                gate.Report(step);
            }
            standing = on;
        }
    }

    /// <summary>Stamp on the switch and the gate opens for a few seconds.</summary>
    public class TimedSwitch : MonoBehaviour
    {
        private PuzzleGate gate;
        private Transform button;
        private bool standing;

        public void Setup(PuzzleGate g, Transform b) { gate = g; button = b; }

        private void Update()
        {
            if (gate == null) return;
            bool on = PuzzleTouch.Near(transform, 0.8f);
            if (on && !standing)
            {
                gate.Door.Trigger(Time.time);
                GameEvents.PlaySfx(AudioCues.Lever);
                GameUI.Banner(Strings.Get("puzzle_timed"), 1.2f);
            }
            standing = on;
            if (button != null)
            {
                var lp = button.localPosition;
                lp.y = gate.Door.IsOpen(Time.time) ? 0.02f : 0.1f;
                button.localPosition = lp;
            }
        }
    }

    /// <summary>Walk into it from behind and it slides ahead of him, until it sits on the plate.</summary>
    public class PushCrate : MonoBehaviour
    {
        private PuzzleGate gate;
        private CratePush push;
        private float sfxAt;

        public void Setup(PuzzleGate g) { gate = g; push = g.CrateFrom(transform.localPosition.z); }

        private void Update()
        {
            var p = PlayerController.Active;
            if (p == null || push == null || push.Solved) return;
            Vector3 d = p.transform.position - transform.position;
            bool behind = d.z < -0.4f && d.z > -1.5f && Mathf.Abs(d.x) < 0.95f && Mathf.Abs(d.y) < 1.2f;
            float forward = p.Velocity.z;
            if (!behind || forward < 0.4f) return;
            float before = push.Position;
            var step = push.Push(Mathf.Min(forward, 4f) * 0.7f * Time.deltaTime);
            var lp = transform.localPosition;
            lp.z += push.Position - before;
            transform.localPosition = lp;
            if (Time.time > sfxAt) { sfxAt = Time.time + 0.4f; GameEvents.PlaySfx(AudioCues.CratePush); }
            if (step == PuzzleStep.Solved) gate.Report(step);
        }
    }
}
