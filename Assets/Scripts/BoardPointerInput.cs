using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class BoardPointerInput : MonoBehaviour
{
    [SerializeField] private PlantPlacementController placementController;
    [SerializeField] private Camera gameplayCamera;

    public void Configure(PlantPlacementController placement, Camera camera)
    {
        placementController = placement;
        gameplayCamera = camera;
    }

    private void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (placementController == null || gameplayCamera == null ||
            !TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            return;
        }

        Vector3 screenPoint = new(screenPosition.x, screenPosition.y, -gameplayCamera.transform.position.z);
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPoint);
        worldPosition.z = 0f;
        placementController.TryPlaceSelectedAtWorld(worldPosition, Time.time);
    }

    private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            pointerId = -1;
            return true;
        }
#endif

        screenPosition = default;
        pointerId = -1;
        return false;
    }
}
