// (c) Ahmed Alamrani. All rights reserved.
using Aron.Input;
using Aron.Logic;
using Aron.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Aron.EditorTools
{
    /// <summary>Lets the mouse stand in for a finger in Play mode, and replays the touch tutorial.</summary>
    public static class TouchSimulationMenu
    {
        private const string SimulatePath = "Aron/Simulate Touch With Mouse";

        [MenuItem(SimulatePath, priority = 40)]
        private static void ToggleSimulation()
        {
            bool on = !EditorPrefs.GetBool(TouchGestureInput.SimulatePref, false);
            EditorPrefs.SetBool(TouchGestureInput.SimulatePref, on);
            if (!Application.isPlaying) return;
            if (on) TouchSimulation.Enable();
            else TouchSimulation.Disable();
        }

        [MenuItem(SimulatePath, true)]
        private static bool ToggleSimulationCheck()
        {
            Menu.SetChecked(SimulatePath, EditorPrefs.GetBool(TouchGestureInput.SimulatePref, false));
            return true;
        }

        [MenuItem("Aron/Replay Touch Tutorial", priority = 41)]
        private static void ResetTutorial()
        {
            SaveSystem.Data.touchTutorialStep = (int)TutorialStep.Steer;
            SaveSystem.Save();
            Debug.Log("[Aron] The touch tutorial will show again on the next level start.");
        }
    }
}
