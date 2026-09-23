using System.Collections.Generic;
using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class BoardPointerInput : MonoBehaviour
{
    [SerializeField] private BoardInteractionRouter interactionRouter;
    [SerializeField] private Camera gameplayCamera;

    public void Configure(BoardInteractionRouter router, Camera camera)
    {
        interactionRouter = router;
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
        if (interactionRouter == null || gameplayCamera == null ||
            !TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            return;
        }

        if (IsPointerOverUi(screenPosition, pointerId))
        {
            return;
        }

        Vector3 screenPoint = new(screenPosition.x, screenPosition.y, -gameplayCamera.transform.position.z);
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPoint);
        worldPosition.z = 0f;
        interactionRouter.HandleWorldClick(worldPosition, Time.time);
    }

    public static bool IsPointerOverUi(Vector2 screenPosition, int pointerId)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (eventSystem.IsPointerOverGameObject(pointerId))
        {
            return true;
        }

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition,
            pointerId = pointerId
        };
        var results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);
        foreach (RaycastResult result in results)
        {
            if (result.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
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
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;
            pointerId = touch.fingerId;
            return true;
        }

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
