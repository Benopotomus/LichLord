using DWD.Pooling;
using Fusion;
using UnityEngine;
using DG.Tweening;
using LichLord.NonPlayerCharacters;
using LichLord.Items;

namespace LichLord.Props
{
    public class HarvestNode : Prop
    {
        [SerializeField]
        private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        [SerializeField]
        private RockExplosionSystem _harvestCompletePrefab;

        [SerializeField]
        protected PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        [SerializeField]
        private float _shakeDuration = 0.25f;

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            base.OnSpawned(propRuntimeState, propManager);

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

            _stateComponent.UpdateState(_runtimeState.GetState());
        }

        public override void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;

            base.StartRecycle();
        }

        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);

            _stateComponent.UpdateState(_runtimeState.GetState());

            //if (_interactEffect != null)
            //    _interactEffect.Toggle(propRuntimeState.GetIsInteracting());
        }

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetIsInteracting())
                return false;

            if (_runtimeState.GetIsActivated())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetIsActivated())
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Harvest Node";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return 32;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.HarvestNode;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Harvest Node.");

            if (RuntimeState.Definition.PropDataDefinition is not HarvestNodeDataDefinition harvestData)
                return;

            var currencyComponent = interactor.PC.Currency;

            if (!currencyComponent.HasRoomForCurrency(harvestData.CurrencyTypeHarvested.CurrencyType, harvestData.PlayerResourcesPerHarvest))
                interactor.CancelInteract(interactable, "Inventory Full");
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Harvest Node.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Harvest Node Interaction complete.");
            // Trigger effects, state changes, or events

            if (RuntimeState.Definition.PropDataDefinition is not HarvestNodeDataDefinition harvestData)
                return;

            PlayerCharacter pc = interactor.PC;

            Harvest(pc);

            CurrencyDefinition currencyDef = harvestData.CurrencyTypeHarvested;
            if (currencyDef == null)
                return;

            FItemData tempItemData = new FItemData();
            tempItemData.DefinitionID = currencyDef.TableID;
            currencyDef.DataDefinition.SetStackCount(harvestData.PlayerResourcesPerHarvest, ref tempItemData);

            pc.Inventory.AddItemToInventory(tempItemData);
        }

        public void Harvest(PlayerCharacter pc)
        {
            if (RuntimeState.Definition.PropDataDefinition is not HarvestNodeDataDefinition harvestData)
                return;

            SceneContext context = Context;
            NetworkRunner runner = Context.Runner;

            context.PropManager.RPC_HarvestNode_PC(ChunkID, Index, harvestData.HarvestPointsCost, pc);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.PropManager.Predict_HarvestNode(ChunkID, Index, harvestData.HarvestPointsCost);
        }

        public void ProgressHarvest(NonPlayerCharacter npc)
        {
            if (RuntimeState.Definition.PropDataDefinition is not HarvestNodeDataDefinition harvestData)
                return;

            SceneContext context = Context;
            NetworkRunner runner = Context.Runner;

            if (npc.RuntimeState.GetHarvestProgress() >= (harvestData.HarvestProgressMax - 1))
            {
                CurrencyDefinition currencyDefinition = Global.Tables.CurrencyTable.TryGetDefinition(harvestData.CurrencyTypeHarvested.CurrencyType);

                FItemData constructedItem = new FItemData();
                constructedItem.DefinitionID = currencyDefinition.TableID;
                currencyDefinition.DataDefinition.SetStackCount(25, ref constructedItem);

                npc.RuntimeState.SetHarvestProgress(0);
                npc.RuntimeState.SetCarriedItem(constructedItem);
                context.PropManager.RPC_HarvestNode_NPC(ChunkID, Index, harvestData.HarvestPointsCost, npc.Replicator, (byte)npc.Index);
                //Debug.Log(npc.RuntimeState.GetHarvestProgress());
            }
            else
            {
                npc.RuntimeState.AddHarvestProgress(1);
                context.PropManager.RPC_HarvestProgress_NPC(ChunkID, Index, harvestData.HarvestPointsCost, npc.Replicator, (byte)npc.Index);
                //Debug.Log(npc.RuntimeState.GetHarvestProgress());
            }
        }

        public void PlayHarvestShake()
        {
            // Create a DOTween Sequence to handle both shake effects
            Sequence shakeSequence = DOTween.Sequence();

            // Add position shake
            shakeSequence.Join(transform.DOShakePosition(
                duration: _shakeDuration, // Duration of the shake
                strength: 0.05f, // Strength of the position shake
                vibrato: 20, // Number of oscillations
                randomness: 90, // Randomness of the shake
                snapping: false, // Smooth movement
                fadeOut: true // Gradually reduce shake intensity
            ));

            // Add rotation shake
            shakeSequence.Join(transform.DOShakeRotation(
                duration: _shakeDuration, // Same duration as position shake
                strength: new Vector3(2f, 2f, 2f), // Slight rotation shake (degrees)
                vibrato: 20, // Number of oscillations
                randomness: 90, // Randomness of the shake
                fadeOut: true // Gradually reduce shake intensity
            ));

            shakeSequence.OnComplete(() =>
            {

            });
        }

        public void PlayHarvestParticles(Transform suckTransform)
        {
            RockExplosionSystem system = DWDObjectPool.Instance.SpawnAt(_harvestCompletePrefab, transform.position, Quaternion.identity) as RockExplosionSystem;

            system.target = suckTransform;
            system.GetComponent<VisualEffectBase>().Initialize();
        }
    }
}