using DWD.Pooling;
using Fusion;
using LichLord.NonPlayerCharacters;
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
        private StockpileCurrencyStack _ironPilePrefab;

        [SerializeField]
        private StockpileCurrencyStack _deathcapsPilePrefab;

        [SerializeField]
        private int _stockpileIndex = -1;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

            _stockpileIndex = RuntimeState.GetStockpileIndex();

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
            _stockpileIndex = RuntimeState.GetStockpileIndex();
            var stockPileData = Context.ContainerManager.GetStockPile(_stockpileIndex);

            for (int i = 0; i < 4; i++)
            {
                FCurrencyStack currentStack = stockPileData.GetCurrencyStack(i);
                StockpileCurrencyStack currentPile = _piles[i];

                if (currentStack.Value > 0)
                {
                    if (currentPile == null)
                    {
                        StockpileCurrencyStack currencyStackPrefab = currentStack.CurrencyType switch
                        {
                            ECurrencyType.Wood => _woodPilePrefab,
                            ECurrencyType.Stone => _stonePilePrefab,
                            ECurrencyType.Deathcaps => _deathcapsPilePrefab,
                            ECurrencyType.Iron => _ironPilePrefab,
                            _ => throw new System.ArgumentOutOfRangeException()
                        };

                        Vector3 worldPos = CachedTransform.TransformPoint(_pilePositions[i]);
                        Quaternion rotation = CachedTransform.localRotation;
                        currentPile = DWDObjectPool.Instance.SpawnAttached(currencyStackPrefab, worldPos, rotation, CachedTransform) as StockpileCurrencyStack;
                        currentPile.SetCurrencyCount(currentStack.Value);
                        _piles[i] = currentPile;
                    }
                    else
                    {
                        currentPile.SetCurrencyCount(currentStack.Value);
                    }
                }
                else
                {

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

            if (RuntimeState.DataDefinition is not StockpileDataDefinition dataDefinition)
                return;
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Stockpile.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Stockpile Interaction complete.");
            // Trigger effects, state changes, or events

            if (RuntimeState.DataDefinition is not StockpileDataDefinition dataDefinition)
                return;

            NetworkRunner runner = Context.Runner;
            PlayerCharacter pc = interactor.PC;

            var currencyType = ECurrencyType.None;
            var value = 0;

            // i want to grab the first currency with a stack and add it to the stockpile 
            pc.Currency.GetCurrencyWithCount(ref currencyType, ref value);

            if (currencyType == ECurrencyType.None)
                return;

            pc.Currency.AddCurrency(currencyType, -value);

            int stockpileIndex = RuntimeState.GetStockpileIndex();

            Context.ContainerManager.RPC_StockpileDropOff_Player(stockpileIndex, currencyType, value, pc);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                Context.ContainerManager.Predict_StockpileDropOff(stockpileIndex, currencyType, value);

        }

        public void DropOffCurrency(NonPlayerCharacter npc)
        {
            var currencyType = npc.RuntimeState.GetCarriedCurrencyType();
            var value = npc.RuntimeState.GetCarriedCurrencyAmount(); ;

            if (currencyType == ECurrencyType.None)
                return;

            npc.RuntimeState.SetCarriedCurrencyType(ECurrencyType.None);

            int stockpileIndex = RuntimeState.GetStockpileIndex();

            int returnValue = Context.ContainerManager.AddToStockpile(stockpileIndex, currencyType, value);
        }
    }
}