
using LichLord.Items;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterCarriedItemComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField] private GameObject _stoneGO;
        [SerializeField] private GameObject _woodGO;
        [SerializeField] private GameObject _ironGO;
        [SerializeField] private GameObject _deathCapsGO;


        private FItemData _carriedItem;
        public FItemData CarriedItem => _carriedItem;

        [SerializeField]
        private ItemDefinition _definition;

        public void OnSpawned()
        {
            if (!_npc.RuntimeState.IsWorker())
                return;

            _stoneGO.SetActive(false);
            _woodGO.SetActive(false);
            _ironGO.SetActive(false);
            _deathCapsGO.SetActive(false);
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
            FItemData oldItem = _carriedItem;
            FItemData newItem = runtimeState.GetCarriedItem();

            if (oldItem.IsEqual(newItem))
                return;

            _carriedItem = newItem;

            if (!_carriedItem.IsValid())
            {
                _stoneGO.SetActive(false);
                _woodGO.SetActive(false);
                _ironGO.SetActive(false);
                _deathCapsGO.SetActive(false);
                return;
            }

            ItemDefinition definition = Global.Tables.ItemTable.TryGetDefinition(_carriedItem.DefinitionID);
            _definition = definition;

            if (definition == null)
                return;

            if (definition is CurrencyDefinition currencyDefinition)
            {
                switch (currencyDefinition.CurrencyType)
                {
                    case ECurrencyType.None:
                        _stoneGO.SetActive(false);
                        _woodGO.SetActive(false);
                        _ironGO.SetActive(false);
                        _deathCapsGO.SetActive(false);
                        break;
                    case ECurrencyType.Wood:
                        _woodGO.SetActive(true);
                        _stoneGO.SetActive(false);
                        _ironGO.SetActive(false);
                        _deathCapsGO.SetActive(false);
                        break;
                    case ECurrencyType.Stone:
                        _stoneGO.SetActive(true);
                        _woodGO.SetActive(false);
                        _ironGO.SetActive(false);
                        _deathCapsGO.SetActive(false);
                        break;
                    case ECurrencyType.IronOre:
                        _ironGO.SetActive(true);
                        _stoneGO.SetActive(false);
                        _woodGO.SetActive(false);
                        _deathCapsGO.SetActive(false);
                        break;
                    case ECurrencyType.Deathcaps:
                        _deathCapsGO.SetActive(true);
                        _stoneGO.SetActive(false);
                        _woodGO.SetActive(false);
                        _ironGO.SetActive(false);
                        break;
                }
            }
        }
    }
}
