using System;

namespace LichLord.CardGame
{
    /// <summary>
    /// Runtime state for an enemy during a combat encounter.
    /// Enemies have no cards — they deal damage through passive effects evaluated each round.
    /// </summary>
    public class EnemyCombatant
    {
        public EnemyData         Data          { get; }
        public int               CurrentHealth { get; private set; }
        public int               Block         { get; private set; }
        public bool              IsAlive       => CurrentHealth > 0;
        public DiceManager       Dice          { get; }
        public StatusEffectState StatusEffects { get; } = new StatusEffectState();

        public EnemyCombatant(EnemyData data)
        {
            Data          = data;
            CurrentHealth = data.maxHealth;
            Dice          = new DiceManager(data.diceCount);
        }

        /// <summary>
        /// Applies damage after subtracting current block.
        /// Block is consumed first; any remainder reduces health.
        /// </summary>
        public void TakeDamage(int amount)
        {
            amount = Math.Max(0, amount);
            int absorbed = Math.Min(Block, amount);
            Block         -= absorbed;
            CurrentHealth -= amount - absorbed;
            if (CurrentHealth < 0) CurrentHealth = 0;
        }

        /// <summary>
        /// Gains block, applying Dexterity bonus first then Frail penalty.
        /// Frail reduces block gained by 25% (rounds down), applied after Dexterity.
        /// </summary>
        public void GainBlock(int amount)
        {
            amount = Math.Max(0, amount);
            amount += StatusEffects.Get(EStatusEffect.Dexterity);
            if (StatusEffects.Has(EStatusEffect.Frail))
                amount = (int)(amount * 0.75f);
            Block += Math.Max(0, amount);
        }

        public void ClearBlock()
        {
            Block = 0;
        }

        /// <summary>Rolls all dice at the start of a round.</summary>
        public void StartRound()
        {
            Dice.RollAll();
        }

        /// <summary>
        /// Applies pre-round passive effects (e.g., rerolling specific die values).
        /// Call this after StartRound and before the player turn begins.
        /// </summary>
        public void ApplyPreRoundEffects()
        {
            foreach (var passive in Data.passiveEffects)
            {
                switch (passive.effectType)
                {
                    case EEffectType.RerollDice:
                        Dice.RerollCount(passive.count);
                        break;
                    case EEffectType.RerollAllDice:
                        Dice.RerollAll();
                        break;
                    case EEffectType.RerollByValue:
                        Dice.RerollDiceShowingValue(passive.dieValue);
                        break;
                }
            }
        }

        /// <summary>
        /// Calculates block from all GainBlock passives and applies it.
        /// Call this at the start of the enemy's turn phase, before damage.
        /// </summary>
        public void CalculateAndApplyBlock()
        {
            ClearBlock();
            foreach (var passive in Data.passiveEffects)
            {
                if (passive.effectType != EEffectType.GainBlock) continue;
                int triggers = PokerEvaluator.EvaluateTriggerCount(
                    passive.triggerOn, Dice.CurrentRoll, passive.dieValue, passive.valueThreshold);
                GainBlock(triggers * passive.magnitude);
            }
        }

        /// <summary>
        /// Applies any ApplyStatusEffect passives to the player.
        /// Call this during the enemy's turn phase.
        /// </summary>
        public void ApplyStatusEffectsToPlayer(PlayerCombatant player)
        {
            foreach (var passive in Data.passiveEffects)
            {
                if (passive.effectType != EEffectType.ApplyStatusEffect) continue;
                int triggers = PokerEvaluator.EvaluateTriggerCount(
                    passive.triggerOn, Dice.CurrentRoll, passive.dieValue, passive.valueThreshold);
                if (triggers > 0)
                    player.StatusEffects.Add(passive.statusEffect, passive.magnitude * triggers);
            }
        }

        /// <summary>
        /// Calculates total damage this enemy deals this round from all DealDamage passives.
        /// Does NOT apply Weak/Strength modifiers — those are applied in CombatManager.
        /// </summary>
        public int CalculateDamage()
        {
            int total = 0;
            foreach (var passive in Data.passiveEffects)
            {
                if (passive.effectType != EEffectType.DealDamage) continue;
                int triggers = PokerEvaluator.EvaluateTriggerCount(
                    passive.triggerOn, Dice.CurrentRoll, passive.dieValue, passive.valueThreshold);
                total += triggers * passive.magnitude;
            }
            return total;
        }
    }
}
