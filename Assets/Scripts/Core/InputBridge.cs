using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class InputBridge
{
    public static float GetAxisRaw(string axisName)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return 0f;
        if (axisName == "Horizontal")
        {
            float val = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) val += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) val -= 1f;
            return val;
        }
        if (axisName == "Vertical")
        {
            float val = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) val += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) val -= 1f;
            return val;
        }
        return 0f;
#else
        return Input.GetAxisRaw(axisName);
#endif
    }

    public static float GetAxis(string axisName)
    {
#if ENABLE_INPUT_SYSTEM
        if (axisName == "Mouse X")
        {
            return Mouse.current != null ? Mouse.current.delta.x.ReadValue() * 0.08f : 0f;
        }
        if (axisName == "Mouse Y")
        {
            return Mouse.current != null ? Mouse.current.delta.y.ReadValue() * 0.08f : 0f;
        }
        if (axisName == "Mouse ScrollWheel")
        {
            return Mouse.current != null ? Mouse.current.scroll.y.ReadValue() * 0.005f : 0f;
        }
        return GetAxisRaw(axisName);
#else
        return Input.GetAxis(axisName);
#endif
    }

    public static bool GetButtonDown(string buttonName)
    {
        if (buttonName == "Jump")
        {
            return GetKeyDown(KeyCode.Space);
        }
        return false;
    }

    public static bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        switch (key)
        {
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
                return (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            case KeyCode.W: return Keyboard.current.wKey.isPressed;
            case KeyCode.A: return Keyboard.current.aKey.isPressed;
            case KeyCode.S: return Keyboard.current.sKey.isPressed;
            case KeyCode.D: return Keyboard.current.dKey.isPressed;
            case KeyCode.Space: return Keyboard.current.spaceKey.isPressed;
            case KeyCode.E: return Keyboard.current.eKey.isPressed;
            case KeyCode.T: return Keyboard.current.tKey.isPressed;
            case KeyCode.O: return Keyboard.current.oKey.isPressed;
            case KeyCode.Escape: return Keyboard.current.escapeKey.isPressed;
            default: return false;
        }
#else
        return Input.GetKey(key);
#endif
    }

    public static bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        switch (key)
        {
            case KeyCode.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
            case KeyCode.E: return Keyboard.current.eKey.wasPressedThisFrame;
            case KeyCode.T: return Keyboard.current.tKey.wasPressedThisFrame;
            case KeyCode.O: return Keyboard.current.oKey.wasPressedThisFrame;
            case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
            case KeyCode.Alpha1: return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
            case KeyCode.Alpha2: return Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
            case KeyCode.Alpha3: return Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }

    public static bool GetMouseButton(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        if (button == 0) return Mouse.current.leftButton.isPressed;
        if (button == 1) return Mouse.current.rightButton.isPressed;
        if (button == 2) return Mouse.current.middleButton.isPressed;
        return false;
#else
        return Input.GetMouseButton(button);
#endif
    }

    public static bool GetMouseButtonDown(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
        if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
        if (button == 2) return Mouse.current.middleButton.wasPressedThisFrame;
        return false;
#else
        return Input.GetMouseButtonDown(button);
#endif
    }
}
