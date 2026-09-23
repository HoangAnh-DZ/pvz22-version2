using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea;
    Vector2Int lastScreen;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            Apply();
    }

void Apply()
    {
        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        bool invalid = safe.width <= 0f || safe.height <= 0f || safe.xMin < 0f || safe.yMin < 0f ||
                       safe.xMax > Screen.width + 1f || safe.yMax > Screen.height + 1f;
        if (invalid) safe = new Rect(0f, 0f, Screen.width, Screen.height);
        rectTransform.anchorMin = new Vector2(
            Mathf.Clamp01(safe.xMin / Screen.width), Mathf.Clamp01(safe.yMin / Screen.height));
        rectTransform.anchorMax = new Vector2(
            Mathf.Clamp01(safe.xMax / Screen.width), Mathf.Clamp01(safe.yMax / Screen.height));
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        lastSafeArea = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
    }
}