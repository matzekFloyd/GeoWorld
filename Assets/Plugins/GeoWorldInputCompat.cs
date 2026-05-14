using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Reads keyboard/mouse/gamepad via the Input System so gameplay works when
/// <b>Active Input Handling</b> is set to <b>Input System Package (New)</b> only.
/// Lives under <c>Plugins</c> so Standard Assets (first-pass) can reference it.
/// </summary>
public static class GeoWorldInputCompat
{
    const float MouseAxisScale = 0.05f;
    const float KeyAxisSmoothSpeed = 14f;

    static float s_SmoothHorizontal;
    static float s_SmoothVertical;
    static float s_SmoothJump;

    public static Vector3 MousePosition =>
        Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;

    public static bool AnyKeyOrMouseButtonDownThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;
        if (Mouse.current == null)
            return false;
        return Mouse.current.leftButton.wasPressedThisFrame
            || Mouse.current.rightButton.wasPressedThisFrame
            || Mouse.current.middleButton.wasPressedThisFrame;
    }

    public static bool GetMouseButton(int button)
    {
        if (Mouse.current == null)
            return false;
        return button switch
        {
            0 => Mouse.current.leftButton.isPressed,
            1 => Mouse.current.rightButton.isPressed,
            2 => Mouse.current.middleButton.isPressed,
            _ => false
        };
    }

    public static bool GetMouseButtonDown(int button)
    {
        if (Mouse.current == null)
            return false;
        return button switch
        {
            0 => Mouse.current.leftButton.wasPressedThisFrame,
            1 => Mouse.current.rightButton.wasPressedThisFrame,
            2 => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false
        };
    }

    public static bool GetMouseButtonUp(int button)
    {
        if (Mouse.current == null)
            return false;
        return button switch
        {
            0 => Mouse.current.leftButton.wasReleasedThisFrame,
            1 => Mouse.current.rightButton.wasReleasedThisFrame,
            2 => Mouse.current.middleButton.wasReleasedThisFrame,
            _ => false
        };
    }

    public static float GetAxis(string axisName, bool raw)
    {
        switch (axisName)
        {
            case "Horizontal":
                return SmoothAxis(ReadHorizontalRaw(), raw, ref s_SmoothHorizontal);
            case "Vertical":
                return SmoothAxis(ReadVerticalRaw(), raw, ref s_SmoothVertical);
            case "Mouse X":
                return ReadMouseDeltaX();
            case "Mouse Y":
                return ReadMouseDeltaY();
            case "Mouse ScrollWheel":
                return Mouse.current != null ? Mouse.current.scroll.ReadValue().y * 0.01f : 0f;
            case "Jump":
                {
                    float j = IsJumpPressed() ? 1f : 0f;
                    return SmoothAxis(j, raw, ref s_SmoothJump);
                }
            default:
                return 0f;
        }
    }

    static float SmoothAxis(float target, bool raw, ref float smoothed)
    {
        if (raw)
            return target;
        smoothed = Mathf.MoveTowards(smoothed, target, Time.deltaTime * KeyAxisSmoothSpeed);
        return smoothed;
    }

    static float ReadHorizontalRaw()
    {
        float v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                v += 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
            v = Mathf.Clamp(v + pad.leftStick.x.ReadValue(), -1f, 1f);
        else
            v = Mathf.Clamp(v, -1f, 1f);
        return v;
    }

    static float ReadVerticalRaw()
    {
        float v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                v += 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
            v = Mathf.Clamp(v + pad.leftStick.y.ReadValue(), -1f, 1f);
        else
            v = Mathf.Clamp(v, -1f, 1f);
        return v;
    }

    static float ReadMouseDeltaX()
    {
        return Mouse.current != null ? Mouse.current.delta.x.ReadValue() * MouseAxisScale : 0f;
    }

    static float ReadMouseDeltaY()
    {
        return Mouse.current != null ? Mouse.current.delta.y.ReadValue() * MouseAxisScale : 0f;
    }

    public static bool GetButton(string name)
    {
        return name switch
        {
            "Fire1" => IsFire1Pressed(),
            "Fire2" => Mouse.current != null && Mouse.current.rightButton.isPressed,
            "Jump" => IsJumpPressed(),
            "Cancel" => Keyboard.current != null && Keyboard.current.escapeKey.isPressed,
            "ResetObject" => Keyboard.current != null && Keyboard.current.rKey.isPressed,
            _ => false
        };
    }

    public static bool GetButtonDown(string name)
    {
        return name switch
        {
            "Fire1" => (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame),
            "Fire2" => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame,
            "Jump" => (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame),
            "Cancel" => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame,
            "ResetObject" => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame,
            _ => false
        };
    }

    public static bool GetButtonUp(string name)
    {
        return name switch
        {
            "Fire1" => (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                || (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasReleasedThisFrame),
            "Fire2" => Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame,
            "Jump" => (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame),
            "Cancel" => Keyboard.current != null && Keyboard.current.escapeKey.wasReleasedThisFrame,
            "ResetObject" => Keyboard.current != null && Keyboard.current.rKey.wasReleasedThisFrame,
            _ => false
        };
    }

    static bool IsFire1Pressed()
    {
        return (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed);
    }

    static bool IsJumpPressed()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
    }

    public static bool GetKey(KeyCode key)
    {
        return TryGetKeyControl(key, out var c) && c.isPressed;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        return TryGetKeyControl(key, out var c) && c.wasPressedThisFrame;
    }

    public static bool GetKeyUp(KeyCode key)
    {
        return TryGetKeyControl(key, out var c) && c.wasReleasedThisFrame;
    }

    static bool TryGetKeyControl(KeyCode key, out KeyControl control)
    {
        control = null;
        var kb = Keyboard.current;
        if (kb == null)
            return false;

        control = key switch
        {
            KeyCode.C => kb.cKey,
            KeyCode.LeftShift => kb.leftShiftKey,
            KeyCode.RightShift => kb.rightShiftKey,
            KeyCode.LeftControl => kb.leftCtrlKey,
            KeyCode.RightControl => kb.rightCtrlKey,
            KeyCode.Escape => kb.escapeKey,
            KeyCode.M => kb.mKey,
            KeyCode.Alpha1 => kb.digit1Key,
            KeyCode.Alpha2 => kb.digit2Key,
            KeyCode.Alpha3 => kb.digit3Key,
            KeyCode.Alpha4 => kb.digit4Key,
            KeyCode.Alpha5 => kb.digit5Key,
            KeyCode.Alpha6 => kb.digit6Key,
            KeyCode.Alpha7 => kb.digit7Key,
            KeyCode.Alpha8 => kb.digit8Key,
            KeyCode.Alpha9 => kb.digit9Key,
            KeyCode.Alpha0 => kb.digit0Key,
            KeyCode.Q => kb.qKey,
            KeyCode.W => kb.wKey,
            KeyCode.E => kb.eKey,
            KeyCode.R => kb.rKey,
            KeyCode.T => kb.tKey,
            KeyCode.Y => kb.yKey,
            KeyCode.U => kb.uKey,
            KeyCode.I => kb.iKey,
            KeyCode.O => kb.oKey,
            KeyCode.P => kb.pKey,
            KeyCode.A => kb.aKey,
            KeyCode.S => kb.sKey,
            KeyCode.D => kb.dKey,
            KeyCode.F => kb.fKey,
            KeyCode.G => kb.gKey,
            KeyCode.H => kb.hKey,
            KeyCode.J => kb.jKey,
            KeyCode.K => kb.kKey,
            KeyCode.L => kb.lKey,
            KeyCode.Z => kb.zKey,
            KeyCode.X => kb.xKey,
            KeyCode.V => kb.vKey,
            KeyCode.B => kb.bKey,
            KeyCode.N => kb.nKey,
            KeyCode.Space => kb.spaceKey,
            KeyCode.LeftAlt => kb.leftAltKey,
            KeyCode.RightAlt => kb.rightAltKey,
            KeyCode.Tab => kb.tabKey,
            KeyCode.Return => kb.enterKey,
            KeyCode.Backspace => kb.backspaceKey,
            KeyCode.LeftArrow => kb.leftArrowKey,
            KeyCode.RightArrow => kb.rightArrowKey,
            KeyCode.UpArrow => kb.upArrowKey,
            KeyCode.DownArrow => kb.downArrowKey,
            _ => null
        };
        return control != null;
    }
}
