using LichLord.Buildables;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIStorageChestTooltip : UIWidget
    {
        [SerializeField] private StorageChest _storageChest;
        [SerializeField] private UIFloatingHealthbar _healthbar;

        public void SetStorageChest(StorageChest storageChest)
        {
            _storageChest = storageChest;

            _healthbar.SetHealth(_storageChest.RuntimeState.GetHealth(), _storageChest.RuntimeState.GetMaxHealth());
        }
    }
}