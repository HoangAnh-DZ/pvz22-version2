using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.UI;

public sealed class SunCounterView : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private Text label;

    public void Configure(ResourceManager resources, Text sunLabel)
    {
        Unsubscribe();
        resourceManager = resources;
        label = sunLabel;
        Subscribe();
        Refresh(resourceManager != null ? resourceManager.CurrentSun : 0);
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh(resourceManager != null ? resourceManager.CurrentSun : 0);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.OnSunChanged -= Refresh;
        resourceManager.OnSunChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (resourceManager != null)
        {
            resourceManager.OnSunChanged -= Refresh;
        }
    }

    private void Refresh(int amount)
    {
        if (label != null)
        {
            label.text = amount.ToString();
        }
    }
}
