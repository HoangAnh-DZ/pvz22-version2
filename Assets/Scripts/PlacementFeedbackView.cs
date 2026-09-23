using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlacementFeedbackView : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlantSelectionController selectionController;
    [SerializeField] private PlantPlacementController placementController;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private SpriteRenderer highlightRenderer;
    [SerializeField] private Color validColor = new(0.72f, 0.92f, 0.22f, 0.42f);
    [SerializeField] private Color invalidColor = new(0.62f, 0.16f, 0.12f, 0.42f);

    public void Configure(
        GridManager grid,
        PlantSelectionController selection,
        PlantPlacementController placement,
        Camera camera,
        SpriteRenderer renderer)
    {
        gridManager = grid;
        selectionController = selection;
        placementController = placement;
        gameplayCamera = camera;
        highlightRenderer = renderer;
        Hide();
    }

    private void Update()
    {
        if (gridManager == null || selectionController == null || placementController == null ||
            gameplayCamera == null || highlightRenderer == null ||
            !TryGetPointerPosition(out Vector2 screenPosition, out int pointerId))
        {
            Hide();
            return;
        }

        if (BoardPointerInput.IsPointerOverUi(screenPosition, pointerId))
        {
            Hide();
            return;
        }

        Vector3 screenPoint = new(screenPosition.x, screenPosition.y, -gameplayCamera.transform.position.z);
        Vector3 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPoint);
        worldPosition.z = 0f;

        if (!gridManager.TryWorldToCell(worldPosition, out GridCell cell) ||
            selectionController.SelectedPlant == null)
        {
            Hide();
            return;
        }

        transform.position = cell.WorldPosition;
        highlightRenderer.color = placementController.CanPlace(
            cell,
            selectionController.SelectedPlant,
            Time.time)
            ? validColor
            : invalidColor;
        highlightRenderer.enabled = true;
    }

    private void Hide()
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = false;
        }
    }

    private static bool TryGetPointerPosition(out Vector2 screenPosition, out int pointerId)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;
            pointerId = touch.fingerId;
            return true;
        }

        screenPosition = Input.mousePosition;
        pointerId = -1;
        return true;
#endif

        screenPosition = default;
        pointerId = -1;
        return false;
    }
}
