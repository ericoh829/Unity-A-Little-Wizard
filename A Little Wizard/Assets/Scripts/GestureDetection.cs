using UnityEngine;
using System;

public class GestureDetection : MonoBehaviour
{
    // Time-based thresholds
    public float tapGuaranteedMaxDuration   = 0.10f;
    public float swipeGuaranteedMinDuration = 0.30f;  
    public float holdMinDuration            = 0.50f;  // Minimum time (s) to count as a hold
    public float doubleTapMaxInterval       = 0.10f;  // Maximum time (s) between taps for a double‑tap

    // Movement‑based thresholds
    public float swipeMinDistance       = 150f;   // Minimum pixels to count as a swipe
    public float holdMaxDistance       = 30f;   // Maximum pixels finger/cursor may move to still count as a hold

    private TouchType touchType;
    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    private float   touchStartTime;
    private float   lastTapTime;
    private bool    isHold;

    public enum TouchType
    {
        None,
        Tap,
        DoubleTap,
        Hold,
        SwipeUp,
        SwipeDown,
        SwipeLeft,
        SwipeRight
    }

    public (TouchType, Vector2) GetTouchInput()
    {
        touchType = TouchType.None;

#if UNITY_EDITOR || UNITY_STANDALONE
// Mouse Input
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPosition = Input.mousePosition;
            touchStartTime     = Time.time;
            isHold             = false;
        }

        if (Input.GetMouseButton(0))
        {
            if (Time.time - touchStartTime > holdMinDuration && !isHold)
            {
                isHold    = true;
                touchType = TouchType.Hold;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            touchType = DetermineTouchType(Input.mousePosition);

            return (touchType, touchStartPosition);
        }
#else
// Touch Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    touchStartTime     = Time.time;
                    isHold             = false;
                    break;

                case TouchPhase.Stationary:
                    if (Time.time - touchStartTime > holdMinDuration && !isHold)
                    {
                        isHold    = true;
                        touchType = TouchType.Hold;
                    }
                    break;

                case TouchPhase.Ended:
                    touchType = DetermineTouchType(touch.position);
                    
                    return (touchType, touchStartPosition);
            }
        }
#endif
        return (TouchType.None, touchStartPosition);
    }

    private TouchType DetermineTouchType(Vector2 touchCurrentPosition)
    {
        TouchType touchTypeDetermined = TouchType.None;
        touchEndPosition = touchCurrentPosition;
        float touchDuration  = Time.time - touchStartTime;
        float movedDistance  = (touchEndPosition - touchStartPosition).magnitude;

        if (isHold)
        {
            touchTypeDetermined = (movedDistance <= holdMaxDistance) ? TouchType.Hold : TouchType.None;
            Debug.Log(touchTypeDetermined);
        }
        else if (touchDuration < tapGuaranteedMaxDuration)
        {
            touchTypeDetermined = DetermineTapOrDoubleTap();
            Debug.Log(touchTypeDetermined + ", Guaranteed Duration");
        }
        else if (touchDuration < swipeGuaranteedMinDuration)
        {
            touchTypeDetermined = (movedDistance < swipeMinDistance) ? DetermineTapOrDoubleTap() : DetermineSwipeDirection(touchStartPosition, touchEndPosition);
            Debug.Log(touchTypeDetermined + ", Gray Zone");     
        }
        else 
        {
            touchTypeDetermined = DetermineSwipeDirection(touchStartPosition, touchEndPosition);   
            Debug.Log(touchTypeDetermined + ", Guaranteed Duration");   
        }

        return touchTypeDetermined;
    }

    private TouchType DetermineTapOrDoubleTap()
    {
        bool isDoubleTap = Time.time - lastTapTime < doubleTapMaxInterval;
        return isDoubleTap ? TouchType.DoubleTap : TouchType.Tap;
        lastTapTime = isDoubleTap ? 0f : Time.time;
    }

    private TouchType DetermineSwipeDirection(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        if (delta.magnitude <= swipeMinDistance)
            return TouchType.None;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? TouchType.SwipeRight : TouchType.SwipeLeft;
        else
            return delta.y > 0 ? TouchType.SwipeUp : TouchType.SwipeDown;
    }
}
