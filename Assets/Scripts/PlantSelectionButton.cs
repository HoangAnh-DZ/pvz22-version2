using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public sealed class PlantSelectionButton : MonoBehaviour
{
    [SerializeField] private PlantData plantData;
    [SerializeField] private PlantSelectionController selectionController;
    [SerializeField] private Text label;
    [SerializeField] private Image background;
    [SerializeField] private Color baseColor = Color.white;

    private Button button;

    public void Configure(
        PlantData data,
        PlantSelectionController selection,
        Text buttonLabel,
        Image buttonBackground,
        Color color)
    {
        UnsubscribeSelection();
        plantData = data;
        selectionController = selection;
        label = buttonLabel;
        background = buttonBackground;
        baseColor = color;
        SubscribeSelection();
        Refresh();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        button.onClick.AddListener(SelectThisPlant);
        Refresh();
    }

    private void OnEnable()
    {
        SubscribeSelection();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeSelection();
    }

    private void SelectThisPlant()
    {
        selectionController?.SelectPlant(plantData);
    }

    private void SubscribeSelection()
    {
        if (selectionController == null)
        {
            return;
        }

        selectionController.OnSelectionChanged -= HandleSelectionChanged;
        selectionController.OnSelectionChanged += HandleSelectionChanged;
    }

    private void UnsubscribeSelection()
    {
        if (selectionController != null)
        {
            selectionController.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(PlantData _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (label != null && plantData != null)
        {
            label.text = $"{plantData.displayName}\n{plantData.sunCost} sun";
        }

        if (background != null)
        {
            bool selected = selectionController != null && selectionController.SelectedPlant == plantData;
            background.color = selected ? Color.Lerp(baseColor, Color.white, 0.45f) : baseColor;
        }
    }
}
