using UnityEngine;
namespace PvZ2.Foundation
{
    public sealed class LaneDepthVisual : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        Vector3 baseScale = Vector3.one;
        void Awake() { if (visualRoot == null) visualRoot = transform.Find("Visual"); if (visualRoot != null) baseScale = visualRoot.localScale; }
        public void Configure(Transform root) { visualRoot = root; if (root != null) baseScale = root.localScale; }
public void ApplyLane(int lane, int laneCount)
        {
            if (visualRoot != null) visualRoot.localScale = baseScale;
            int depth = (Mathf.Max(1, laneCount) - 1 - Mathf.Clamp(lane, 0, Mathf.Max(0, laneCount - 1))) * 20;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                int localOrder = renderer.gameObject.name.Contains("Shadow") ? 2 : 10 + renderer.transform.GetSiblingIndex();
                renderer.sortingOrder = depth + localOrder;
            }
        }
    }
}