using DWD.Pooling;

using UnityEngine;

namespace LichLord.Buildables
{
    public class StockpileCurrencyStack : DWDObjectPoolObject
    {
        [SerializeField]
        private GameObject[] _visuals;

        [SerializeField]
        private ECurrencyType _currencyType;
        public ECurrencyType CurrencyType => _currencyType;

        public void SetCurrencyCount(int currencyCount)
        {
            int totalVisuals = _visuals.Length;

            if (currencyCount <= 0)
            {
                // Turn all off if 0 or less
                for (int i = 0; i < totalVisuals; i++)
                    _visuals[i].SetActive(false);

                return;
            }

            // How many visuals should be ON (scaled to max at 250)
            int visualsToEnable = Mathf.CeilToInt((currencyCount / 250f) * totalVisuals);
            visualsToEnable = Mathf.Clamp(visualsToEnable, 1, totalVisuals);

            // Toggle visuals accordingly
            for (int i = 0; i < totalVisuals; i++)
                _visuals[i].SetActive(i < visualsToEnable);
        }

        public void StartRecycle()
        {
            transform.parent = owner.poolRoot;
            DWDObjectPool.Instance.Recycle(this);
        }
    }
}