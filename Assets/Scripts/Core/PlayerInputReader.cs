using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceShip.Core
{
    public static class PlayerInputReader
    {
        private const float StickDeadzone = 0.2f;

        public static Vector2 Move
        {
            get
            {
                Vector2 axis = Vector2.zero;

                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                    {
                        axis.x -= 1f;
                    }
                    if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                    {
                        axis.x += 1f;
                    }
                    if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                    {
                        axis.y += 1f;
                    }
                    if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                    {
                        axis.y -= 1f;
                    }
                }

                if (axis.sqrMagnitude < 0.01f)
                {
                    Gamepad gamepad = Gamepad.current;
                    if (gamepad != null)
                    {
                        Vector2 stick = gamepad.leftStick.ReadValue();
                        if (stick.sqrMagnitude > StickDeadzone * StickDeadzone)
                        {
                            axis = stick;
                        }
                        else
                        {
                            if (gamepad.dpad.left.isPressed) { axis.x -= 1f; }
                            if (gamepad.dpad.right.isPressed) { axis.x += 1f; }
                            if (gamepad.dpad.up.isPressed) { axis.y += 1f; }
                            if (gamepad.dpad.down.isPressed) { axis.y -= 1f; }
                        }
                    }
                }

                return axis.sqrMagnitude > 1f ? axis.normalized : axis;
            }
        }

        public static bool FireHeld
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.isPressed
                                         || keyboard.leftCtrlKey.isPressed))
                {
                    return true;
                }

                Gamepad gamepad = Gamepad.current;
                return gamepad != null && gamepad.buttonSouth.isPressed;
            }
        }

        public static bool StartPressed
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame
                                         || keyboard.enterKey.wasPressedThisFrame
                                         || keyboard.numpadEnterKey.wasPressedThisFrame
                                         || keyboard.rKey.wasPressedThisFrame))
                {
                    return true;
                }

                Gamepad gamepad = Gamepad.current;
                return gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame
                                           || gamepad.startButton.wasPressedThisFrame);
            }
        }

        public static bool PausePressed
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame
                                         || keyboard.pKey.wasPressedThisFrame))
                {
                    return true;
                }

                Gamepad gamepad = Gamepad.current;
                return gamepad != null && gamepad.startButton.wasPressedThisFrame;
            }
        }

        public static int MenuStep
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    {
                        return -1;
                    }
                    if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    {
                        return 1;
                    }
                }

                Gamepad gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    if (gamepad.dpad.left.wasPressedThisFrame) { return -1; }
                    if (gamepad.dpad.right.wasPressedThisFrame) { return 1; }
                }

                return 0;
            }
        }

        public static int MenuVerticalStep
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                    {
                        return 1;
                    }
                    if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                    {
                        return -1;
                    }
                }

                Gamepad gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    if (gamepad.dpad.up.wasPressedThisFrame) { return 1; }
                    if (gamepad.dpad.down.wasPressedThisFrame) { return -1; }
                }

                return 0;
            }
        }

        public static bool ConfirmPressed
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                                         || keyboard.numpadEnterKey.wasPressedThisFrame
                                         || keyboard.spaceKey.wasPressedThisFrame))
                {
                    return true;
                }

                Gamepad gamepad = Gamepad.current;
                return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            }
        }

        public static bool SlowMotionPressed
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame
                                         || keyboard.qKey.wasPressedThisFrame))
                {
                    return true;
                }

                Gamepad gamepad = Gamepad.current;
                return gamepad != null && (gamepad.buttonWest.wasPressedThisFrame
                                           || gamepad.leftShoulder.wasPressedThisFrame);
            }
        }
    }
}
