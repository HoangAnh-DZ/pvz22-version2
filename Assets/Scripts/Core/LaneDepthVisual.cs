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
            if (visualRoot == null) return;
            float t = laneCount <= 1 ? .5f : Mathf.Clamp01(lane / (laneCount - 1f));
            visualRoot.localScale = baseScale * Mathf.Lerp(1.08f, .9f, t);
        }
    }
}