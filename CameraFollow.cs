using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 followOffset = new Vector3(0, 15, 0);
    public float smoothSpeed = 10f;
    
    [Header("Free Roam Settings")]
    public float moveSpeed = 20f;
    public float turboMultiplier = 2.5f;

    private bool isFreeRoaming = false;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Detect if we should start roaming (WASD or Right Click)
        CheckForManualInput();

        // 2. Return to Player (Left Click or Space)
        if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isFreeRoaming = false;
        }

        if (isFreeRoaming)
        {
            HandleFreeRoamMovement();
        }
        else
        {
            // 3. Smooth Follow Logic
            Vector3 desiredPosition = target.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }

    void CheckForManualInput()
    {
        // If any movement key is pressed, break away from the player
        if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || 
            Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed ||
            Mouse.current.rightButton.isPressed)
        {
            isFreeRoaming = true;
        }
    }

    void HandleFreeRoamMovement()
    {
        float currentSpeed = moveSpeed;

        // "Turbo" mode with Left Shift (just like the Editor)
        if (Keyboard.current.leftShiftKey.isPressed)
            currentSpeed *= turboMultiplier;

        Vector3 move = Vector3.zero;

        // Forward/Backward (W/S)
        if (Keyboard.current.wKey.isPressed) move += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) move += Vector3.back;

        // Left/Right (A/D)
        if (Keyboard.current.aKey.isPressed) move += Vector3.left;
        if (Keyboard.current.dKey.isPressed) move += Vector3.right;
        
        // Up/Down (E/Q) - Optional but very "Editor-like"
        if (Keyboard.current.eKey.isPressed) move += Vector3.up;
        if (Keyboard.current.qKey.isPressed) move += Vector3.down;

        // Move the camera
        transform.Translate(move * currentSpeed * Time.deltaTime, Space.World);
    }
}