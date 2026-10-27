// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Aron.Input
{
    /// <summary>
    /// Keys and mouse for testing in the editor: WASD/Arrows, Space jump, J/E/right mouse attack, C/Ctrl slide,
    /// Shift sprint, Esc/P pause. On phones only Escape is read, which is how Android reports the Back button.
    /// A paired Bluetooth gamepad works everywhere.
    /// </summary>
    public class KeyboardInput : IIntentSource
    {
        public PlayerIntent Poll()
        {
            var i = new PlayerIntent();
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
#if UNITY_EDITOR
            if (k != null)
            {
                float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                i.move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
                i.jumpPressed = k.spaceKey.wasPressedThisFrame;
                i.jumpHeld = k.spaceKey.isPressed;
                i.fingerDown = i.jumpHeld;
                i.attackPressed = k.jKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
                i.slidePressed = k.cKey.wasPressedThisFrame || k.leftCtrlKey.wasPressedThisFrame;
                i.pausePressed = k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame;
                i.sprintHeld = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            }
            var m = Mouse.current;
            if (m != null && m.rightButton.wasPressedThisFrame) i.attackPressed = true;
#else
            if (k != null) i.pausePressed = k.escapeKey.wasPressedThisFrame;
#endif
            var g = Gamepad.current;
            if (g != null)
            {
                var stick = g.leftStick.ReadValue();
                if (stick.sqrMagnitude > i.move.sqrMagnitude) i.move = Vector2.ClampMagnitude(stick, 1f);
                i.jumpPressed |= g.buttonSouth.wasPressedThisFrame;
                i.jumpHeld |= g.buttonSouth.isPressed;
                i.attackPressed |= g.buttonWest.wasPressedThisFrame;
                i.slidePressed |= g.buttonEast.wasPressedThisFrame;
                i.pausePressed |= g.startButton.wasPressedThisFrame;
                i.sprintHeld |= g.leftStickButton.isPressed || g.rightShoulder.isPressed;
            }
#elif UNITY_EDITOR
            i.move = Vector2.ClampMagnitude(new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical")), 1f);
            i.jumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            i.jumpHeld = UnityEngine.Input.GetKey(KeyCode.Space);
            i.fingerDown = i.jumpHeld;
            i.attackPressed = UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetKeyDown(KeyCode.E);
            i.slidePressed = UnityEngine.Input.GetKeyDown(KeyCode.C) || UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
            i.pausePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P);
            i.sprintHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift);
#else
            i.pausePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
            return i;
        }
    }
}
