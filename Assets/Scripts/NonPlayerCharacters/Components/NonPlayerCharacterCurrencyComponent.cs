
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterCurrencyComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField] private GameObject _stoneGO;
        [SerializeField] private GameObject _woodGO;

        private ECurrencyType _carriedCurrency;

        public void OnSpawned()
        {
            if (!_npc.RuntimeState.IsWorker())
                return;

            _stoneGO.SetActive(false);
            _woodGO.SetActive(false);
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (!runtimeState.IsWorker())
                return;

            UpdateCarriedCurrencyChange(runtimeState);

            //Debug.Log(runtimeState.GetCarriedCurrencyType().ToString() + " " + runtimeState.GetHarvestProgress());
        }

        private void UpdateCarriedCurrencyChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            ECurrencyType oldCurrency = _carriedCurrency;
            ECurrencyType newCurrency = runtimeState.GetCarriedCurrencyType();

            if (oldCurrency == newCurrency)
                return;

            _carriedCurrency = newCurrency;

            switch (_carriedCurrency)
            {
                case ECurrencyType.None:
                    _stoneGO.SetActive(false);
                    _woodGO.SetActive(false);
                    break;
                case ECurrencyType.Wood:

                    if (_stoneGO == null)
                    {
                        Debug.Log(runtimeState.GetSpawnType());
                    }

                    _stoneGO.SetActive(false);
                    _woodGO.SetActive(true);
                    break;
                case ECurrencyType.Stone:
                    _stoneGO.SetActive(true);
                    _woodGO.SetActive(false);
                    break;
            }
        }
    }
}
