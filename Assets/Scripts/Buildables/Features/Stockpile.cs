using DWD.Pooling;
using Fusion;
using LichLord.Items;
using LichLord.NonPlayerCharacters;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class Stockpile : Buildable
    {
        public override float BonusRadius { get { return 0; } }
        public override bool IsAttackable
        {
            get
            {
                if (_healthComponent.CurrentHealth == 0)
                    return false;

                return true;
            }
        }
        public override Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        [SerializeField] protected BuildableHealthComponent _healthComponent;
        [SerializeField] protected BuildableStateComponent _stateComponent;
        [SerializeField]  private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        private StockpileCurrencyStack[] _piles = new StockpileCurrencyStack[4];

        [SerializeField]
        private Vector3[] _pilePositions = new Vector3[4];

        [SerializeField]
        private StockpileCurrencyStack _woodPilePrefab;

        [SerializeField]
        private StockpileCurrencyStack _stonePilePrefab;

        [SerializeField]
        private StockpileCurrencyStack _ironOrePilePrefab;

        [SerializeField]
        private StockpileCurrencyStack _ironBarPilePrefab;

        [SerializeField]
        private StockpileCurrencyStack _deathcapsPilePrefab;

        [SerializeField]
        private int _containerIndex = -1;

        [SerializeField]
        private int _itemSlotIndexStart = -1;

        [SerializeField]
        private int _itemSlotIndexEnd = -1;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

            _containerIndex = RuntimeState.GetContainerIndex();
            var indexes = RuntimeState.GetItemSlotIndexes();
            _itemSlotIndexStart = indexes.start;
            _itemSlotIndexEnd = indexes.end;

            _interactableComponent.Activate(
                this,
                IsPotentialInteractor,
                IsInteractionValid,
                GetInteractionText,
                GetTicksToComplete,
                GetInteractType,
                GetInteractDistance
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, int tick, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, tick, hasAuthority);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());
            UpdateCurrencyStacks();
        }

        private void UpdateCurrencyStacks()
        {
            _containerIndex = RuntimeState.GetContainerIndex();
            var indexes = RuntimeState.GetItemSlotIndexes();
            _itemSlotIndexStart = indexes.start;
            _itemSlotIndexEnd = indexes.end;

            List<FItemSlotData> itemSlots = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(_containerIndex);

            for (int i = 0; i < itemSlots.Count; i++)
            {
                FItemSlotData currentSlotData = itemSlots[i];
                StockpileCurrencyStack currentPile = _piles[i];

                if (!currentSlotData.ItemData.IsValid())
                {
                    if (currentPile != null)
                    {
                        currentPile.StartRecycle();
                        _piles[i] = null;
                    }
                    continue;
                }

                CurrencyDefinition currencyDef = Global.Tables.ItemTable.TryGetDefinition(currentSlotData.ItemData.DefinitionID) as CurrencyDefinition;
                int stackCount = currencyDef.DataDefinition.GetStackCount(ref currentSlotData.ItemData);

                if (stackCount > 0)
                {
                    // Check if we need to swap type
                    if (currentPile != null && currentPile.CurrencyType != currencyDef.CurrencyType)
                    {
                        // Different type – recycle the old pile
                        currentPile.StartRecycle();
                        _piles[i] = null;
                        currentPile = null;
                    }

                    if (currentPile == null)
                    {
                        StockpileCurrencyStack currencyStackPrefab = currencyDef.CurrencyType switch
                        {
                            ECurrencyType.Wood => _woodPilePrefab,
                            ECurrencyType.Stone => _stonePilePrefab,
                            ECurrencyType.Deathcaps => _deathcapsPilePrefab,
                            ECurrencyType.IronOre => _ironOrePilePrefab,
                            ECurrencyType.IronBar => _ironBarPilePrefab,
                            _ => _woodPilePrefab
                        };

                        Vector3 worldPos = CachedTransform.TransformPoint(_pilePositions[i]);
                        Quaternion rotation = CachedTransform.localRotation;

                        currentPile = DWDObjectPool.Instance.SpawnAttached(currencyStackPrefab, worldPos, rotation, CachedTransform) as StockpileCurrencyStack;
                        currentPile.SetCurrencyCount(stackCount);
                        _piles[i] = currentPile;
                    }
                    else
                    {
                        // Same type – just update the count
                        currentPile.SetCurrencyCount(stackCount);
                    }
                }
                else
                {
                    // Empty slot – recycle
                    if (currentPile != null)
                    {
                        currentPile.StartRecycle();
                        _piles[i] = null;
                    }
                }
            }
        }


        public override void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;

            base.StartRecycle();
        }

        public override void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public override void ProcessHit(ref FHitUtilityData hit)
        {
        }

        // Interactable

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (RuntimeState.GetIsInteracting())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Stockpile";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return -1;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.Container;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Stockpile.");
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Stockpile.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Stockpile Interaction complete.");
        }

        public void DropOffCurrency(NonPlayerCharacter npc)
        {
            var carriedItem = npc.RuntimeState.GetCarriedItem();
            if (!carriedItem.IsValid())
                return;

            CurrencyDefinition definition = Global.Tables.ItemTable.TryGetDefinition(carriedItem.DefinitionID) as CurrencyDefinition;
            if (definition == null)
                return;

            npc.RuntimeState.SetCarriedItem(new FItemData());

            int containerIndex = RuntimeState.GetContainerIndex();

            if (containerIndex >= 0)
            {

                Context.ContainerManager.AddItemToContainer(containerIndex, carriedItem);
            }
        }
    }
}