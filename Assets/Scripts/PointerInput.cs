using UnityEngine;

public static class PointerInput
{
    private const float TapThreshold = 24f;

    private static bool hasUsedTouch;
    private static bool initialized;
    private static int cachedFrame = -1;
    private static Vector2 lastPosition;
    private static Vector2 pressPosition;
    private static Vector2 delta;
    private static float maxDragDistance;
    private static bool isPressed;
    private static bool pressedThisFrame;
    private static bool releasedThisFrame;

    public static bool HasUsedTouch
    {
        get { UpdateState(); return hasUsedTouch; }
    }

    public static Vector2 Position
    {
        get { UpdateState(); return lastPosition; }
    }

    public static Vector2 Delta
    {
        get { UpdateState(); return delta; }
    }

    public static bool IsPressed
    {
        get { UpdateState(); return isPressed; }
    }

    public static bool PressedThisFrame
    {
        get { UpdateState(); return pressedThisFrame; }
    }

    public static bool ReleasedThisFrame
    {
        get { UpdateState(); return releasedThisFrame; }
    }

    public static bool TappedThisFrame
    {
        get { UpdateState(); return releasedThisFrame && maxDragDistance <= TapThreshold; }
    }

    public static int TouchCount
    {
        get { return Input.touchCount; }
    }

    public static Touch GetTouch(int index)
    {
        return Input.GetTouch(index);
    }

    private static void UpdateState()
    {
        if (cachedFrame == Time.frameCount)
        {
            return;
        }

        cachedFrame = Time.frameCount;

        Vector2 pos = RawPosition();

        if (!initialized)
        {
            initialized = true;
            lastPosition = pos;
            delta = Vector2.zero;
        }
        else
        {
            delta = pos - lastPosition;
            lastPosition = pos;
        }

        pressedThisFrame = RawPressed();
        releasedThisFrame = RawReleased();

        if (pressedThisFrame)
        {
            isPressed = true;
            pressPosition = pos;
            maxDragDistance = 0f;
        }

        if (isPressed)
        {
            float distance = Vector2.Distance(pos, pressPosition);

            if (distance > maxDragDistance)
            {
                maxDragDistance = distance;
            }
        }

        if (releasedThisFrame)
        {
            isPressed = false;
        }
    }

    private static Vector2 RawPosition()
    {
        if (Input.touchCount > 0)
        {
            hasUsedTouch = true;
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    private static bool RawPressed()
    {
        if (Input.touchCount > 0)
        {
            hasUsedTouch = true;
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }

        return Input.GetMouseButtonDown(0);
    }

    private static bool RawReleased()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }

        return Input.GetMouseButtonUp(0);
    }
}