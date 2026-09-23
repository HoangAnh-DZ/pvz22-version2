using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image), typeof(CanvasGroup))]
public sealed class PlantSelectionButton : MonoBehaviour
{
    [SerializeField] private PlantData plantData;
    [SerializeField] private PlantSelectionController selectionController;
    [SerializeField] private PlantPlacementController placementController;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text costLabel;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private Color baseColor = Color.white;

    private Button button;
    private CanvasGroup canvasGroup;

    public void Configure(
        PlantData data,
        PlantSelectionController selection,
        PlantPlacementController placement,
        ResourceManager resources,
        Text plantNameLabel,
        Text sunCostLabel,
        Image buttonBackground,
        Image plantIcon,
        Image cooldownImage,
        Outline outline,
        Color color)
    {
        Unsubscribe();
        plantData = data;
        selectionController = selection;
        placementController = placement;
        resourceManager = resources;
        nameLabel = plantNameLabel;
        costLabel = sunCostLabel;
        background = buttonBackground;
        icon = plantIcon;
        cooldownOverlay = cooldownImage;
        selectionOutline = outline;
        baseColor = color;
        CacheComponents();
        Subscribe();
        Refresh();
    }

    private void Awake()
    {
        CacheComponents();
        button.onClick.AddListener(SelectThisPlant);
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        RefreshDynamicState();
    }

    private void CacheComponents()
    {
        button ??= GetComponent<Button>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        background ??= GetComponent<Image>();
    }

    private void SelectThisPlant()
    {
        selectionController?.SelectPlant(plantData);
    }

    private void Subscribe()
    {
        if (selectionController != null)
        {
            selectionController.OnSelectionChanged -= HandleSelectionChanged;
            selectionController.OnSelectionChanged += HandleSelectionChanged;
        }

        if (resourceManager != null)
        {
            resourceManager.OnSunChanged -= HandleSunChanged;
            resourceManager.OnSunChanged += HandleSunChanged;
        }
    }

    private void Unsubscribe()
    {
        if (selectionController != null)
        {
            selectionController.OnSelectionChanged -= HandleSelectionChanged;
        }

        if (resourceManager != null)
        {
            resourceManager.OnSunChanged -= HandleSunChanged;
        }
    }

    private void HandleSelectionChanged(PlantData _)
    {
        Refresh();
    }

    private void HandleSunChanged(int _)
    {
        RefreshDynamicState();
    }

    private void Refresh()
    {
        if (plantData != null)
        {
            if (nameLabel != null)
            {
                nameLabel.text = plantData.displayName;
            }

            if (costLabel != null)
            {
                costLabel.text = plantData.sunCost.ToString();
            }
        }

        RefreshDynamicState();
    }

    private void RefreshDynamicState()
    {
        CacheComponents();

        bool valid = plantData != null && plantData.prefab != null;
        bool selected = valid && selectionController != null &&
                        selectionController.SelectedPlant == plantData;
        bool affordable = valid && resourceManager != null &&
                          resourceManager.CanAfford(plantData.sunCost);
        float remaining = valid && placementController != null
            ? placementController.GetCooldownRemaining(plantData, Time.time)
            : 0f;
        bool coolingDown = remaining > 0f;

        if (button != null)
        {
            button.interactable = valid;
        }

        if (background != null)
        {
            background.color = selected
                ? Color.Lerp(baseColor, Color.white, 0.42f)
                : baseColor;
        }

        if (icon != null)
        {
            Color iconColor = icon.color;
            iconColor.a = affordable && !coolingDown ? 1f : 0.45f;
            icon.color = iconColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = affordable && !coolingDown ? 1f : 0.58f;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (cooldownOverlay != null)
        {
            float duration = plantData != null ? Mathf.Max(0.01f, plantData.cooldown) : 1f;
            cooldownOverlay.fillAmount = coolingDown ? Mathf.Clamp01(remaining / duration) : 0f;
            cooldownOverlay.enabled = coolingDown;
        }
    }
}
