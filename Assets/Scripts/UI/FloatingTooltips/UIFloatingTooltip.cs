using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using LichLord.Props;
using LichLord.World;
using UnityEngine;
using System;

namespace LichLord.UI
{
    public class UIFloatingTooltip : UIFloatingWidget
    {
        [Header("UI Elements")]
        [SerializeField] private UIStockpileTooltip _stockpileTooltip;
        [SerializeField] private UICryptTooltip _cryptTooltip;
        [SerializeField] private UIRefineryTooltip _refineryTooltip;
        [SerializeField] private UIStorageChestTooltip _storageChestTooltip;

        private Action _updateTooltip;  // Cached update delegate

        public void SetTooltipTarget(IChunkTrackable target)
        {
            _updateTooltip = null; // Reset

            // Disable all tooltips
            _cryptTooltip.gameObject.SetActive(false);
            _stockpileTooltip.gameObject.SetActive(false);
            _refineryTooltip.gameObject.SetActive(false);
            _storageChestTooltip.gameObject.SetActive(false);

            if (target == null)
            {
                base.SetTarget(null);
                return;
            }

            switch (target)
            {
                case Crypt crypt:
                    _cryptTooltip.gameObject.SetActive(true);
                    base.SetTarget(crypt.CachedTransform);
                    _updateTooltip = () => _cryptTooltip.SetCryptData(crypt);
                    break;

                case Stockpile stockpile:
                    _stockpileTooltip.gameObject.SetActive(true);
                    base.SetTarget(stockpile.CachedTransform);
                    _updateTooltip = () => _stockpileTooltip.SetStockpileData(stockpile);
                    break;

                case Refinery refinery:
                    _refineryTooltip.gameObject.SetActive(true);
                    base.SetTarget(refinery.CachedTransform);
                    _updateTooltip = () => _refineryTooltip.SetRefinery(refinery);
                    break;

                case StorageChest storageChest:
                    _storageChestTooltip.gameObject.SetActive(true);
                    base.SetTarget(storageChest.CachedTransform);
                    _updateTooltip = () => _storageChestTooltip.SetStorageChest(storageChest);

                    break;
                case Prop prop:
                    base.SetTarget(prop.CachedTransform);
                    break;
                   
            }
        }

        protected override void OnTick()
        {
            _updateTooltip?.Invoke();
        }
    }
}
