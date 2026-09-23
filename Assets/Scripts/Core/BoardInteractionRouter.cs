using UnityEngine;

namespace PvZ2.Foundation
{
    public enum BoardInteractionResult
    {
        None,
        SunCollected,
        PlantPlaced
    }

    public sealed class BoardInteractionRouter : MonoBehaviour
    {
        [SerializeField] private PlantPlacementController placementController;

        public void Configure(PlantPlacementController placement)
        {
            placementController = placement;
        }

        public BoardInteractionResult HandleWorldClick(Vector3 worldPosition, float currentTime)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                SunCollectible sun = hit.GetComponentInParent<SunCollectible>();
                if (sun == null)
                {
                    continue;
                }

                sun.Collect();
                return BoardInteractionResult.SunCollected;
            }

            if (placementController != null &&
                placementController.TryPlaceSelectedAtWorld(worldPosition, currentTime))
            {
                return BoardInteractionResult.PlantPlaced;
            }

            return BoardInteractionResult.None;
        }
    }
}
