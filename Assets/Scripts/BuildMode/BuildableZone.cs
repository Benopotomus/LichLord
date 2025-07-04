using UnityEngine;

namespace LichLord.Buildables
{
    [RequireComponent(typeof(BoxCollider))]
    public class BuildableZone : ContextBehaviour
    {
        [SerializeField]
        private BuildableGrid _grid;
        public BuildableGrid Grid => _grid;

        [SerializeField]
        private BoxCollider _boxCollider;

        private void Reset()
        {
            // Ensure BoxCollider is set up correctly in editor when component is added or reset
            SetupCollider();
        }

        private void OnValidate()
        {
            // Called when values change in inspector, update collider size
            SetupCollider();
        }

        private void Awake()
        {
            SetupCollider();
        }

        private void SetupCollider()
        {
            if (_grid == null)
                return;

            _boxCollider = GetComponent<BoxCollider>();

            // Calculate size
            float sizeX = _grid.GridSizeX * _grid.TileSizeXZ;
            float sizeY = _grid.GridSizeY * _grid.TileSizeXZ;
            float sizeZ = _grid.GridSizeZ * _grid.TileSizeXZ;

            // Set BoxCollider size and center
            _boxCollider.size = new Vector3(sizeX, sizeY, sizeZ);  // height of 2 is arbitrary, adjust if needed
            _boxCollider.center = new Vector3(sizeX / 2f, sizeY / 2f, sizeZ / 2f);  // center so box covers grid area

            _boxCollider.isTrigger = true;
        }
    }
}
