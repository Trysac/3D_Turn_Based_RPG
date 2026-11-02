using UnityEngine;
using UnityEngine.InputSystem;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// Menu-style controls from the Input System. Keyboard: arrows or WASD move, Enter / Space / Z
    /// confirm, Backspace / X / right mouse button go back, Esc or P pause, T toggles auto battle.
    /// Gamepad: d-pad or left stick, A confirm, B back, Start pause, Y auto battle.
    /// The mouse works through the on-screen buttons and by pointing at fighters.
    /// </summary>
    public sealed class GameInput
    {
        const float StickThreshold = 0.6f;
        const float RepeatDelay = 0.4f;
        const float RepeatRate = 0.12f;

        Vector2Int heldDirection;
        float repeatTimer;

        static Keyboard Keys => Keyboard.current;
        static Gamepad Pad => Gamepad.current;
        static Mouse Pointer => Mouse.current;

        /// <summary>One step of menu movement this frame (with key repeat), or zero.</summary>
        public Vector2Int Navigate()
        {
            Vector2Int direction = Held();
            if (direction == Vector2Int.zero)
            {
                heldDirection = Vector2Int.zero;
                return Vector2Int.zero;
            }
            if (direction != heldDirection)
            {
                heldDirection = direction;
                repeatTimer = RepeatDelay;
                return direction;
            }
            repeatTimer -= Time.unscaledDeltaTime;
            if (repeatTimer <= 0f)
            {
                repeatTimer = RepeatRate;
                return direction;
            }
            return Vector2Int.zero;
        }

        static Vector2Int Held()
        {
            int x = 0, y = 0;
            if (Keys != null)
            {
                if (Keys.upArrowKey.isPressed || Keys.wKey.isPressed) y += 1;
                if (Keys.downArrowKey.isPressed || Keys.sKey.isPressed) y -= 1;
                if (Keys.rightArrowKey.isPressed || Keys.dKey.isPressed) x += 1;
                if (Keys.leftArrowKey.isPressed || Keys.aKey.isPressed) x -= 1;
            }
            if (Pad != null)
            {
                Vector2 stick = Pad.leftStick.ReadValue() + Pad.dpad.ReadValue();
                if (stick.y > StickThreshold) y += 1;
                if (stick.y < -StickThreshold) y -= 1;
                if (stick.x > StickThreshold) x += 1;
                if (stick.x < -StickThreshold) x -= 1;
            }
            // One axis at a time: vertical wins, which suits vertical menus.
            if (y != 0)
            {
                return new Vector2Int(0, System.Math.Sign(y));
            }
            return new Vector2Int(System.Math.Sign(x), 0);
        }

        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame ||
                              Keys.spaceKey.wasPressedThisFrame || Keys.zKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        public bool Cancel =>
            (Keys != null && (Keys.backspaceKey.wasPressedThisFrame || Keys.xKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame) ||
            (Pointer != null && Pointer.rightButton.wasPressedThisFrame);

        /// <summary>Esc also backs out of sub-menus before it pauses.</summary>
        public bool Escape => Keys != null && Keys.escapeKey.wasPressedThisFrame;

        public bool Pause =>
            (Keys != null && Keys.pKey.wasPressedThisFrame) || (Pad != null && Pad.startButton.wasPressedThisFrame);

        public bool ToggleAuto =>
            (Keys != null && Keys.tKey.wasPressedThisFrame) || (Pad != null && Pad.buttonNorth.wasPressedThisFrame);

        public bool Click => Pointer != null && Pointer.leftButton.wasPressedThisFrame;

        public Vector2 PointerPosition => Pointer != null ? Pointer.position.ReadValue() : new Vector2(-1000f, -1000f);

        public bool PointerMoved => Pointer != null && Pointer.delta.ReadValue().sqrMagnitude > 0.5f;
    }
}
