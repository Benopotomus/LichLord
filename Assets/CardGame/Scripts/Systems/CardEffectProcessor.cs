using System;

namespace LichLord.CardGame
{
    /// <summary>
    /// Resolves a single <see cref="CardEffectData"/> against the current combat state.
    /// All methods are stateless and operate on the provided combatant references.
    ///
    /// Damage modifiers applied here (player's perspective):
    ///   Strength  — flat bonus added to each DealDamage result.
    ///   Weak      — player deals 25% less damage (rounds down).
    ///   Vulnerable — enemy receives 50% more damage (rounds up).
    /// </summary>
    public static class CardEffectProcessor
    {
        /// <summary>Processes every effect on a card in order.</summary>
        public static void ProcessCard(CardData card, PlayerCombatant player, EnemyCombatant enemy)
        {
            foreach (var effect in card.effects)
                ProcessEffect(effect, player, enemy);
        }

        /// <summary>Resolves one effect entry against the current dice state.</summary>
        public static void ProcessEffect(CardEffectData effect, PlayerCombatant player, EnemyCombatant enemy)
        {
            int[] targetDice = effect.diceTarget == ECardTarget.PlayerDice
                ? player.Dice.CurrentRoll
                : enemy.Dice.CurrentRoll;

            switch (effect.effectType)
            {
                case EEffectType.DealDamage:
                {
                    int triggers = PokerEvaluator.EvaluateTriggerCount(
                        effect.triggerOn, targetDice, effect.dieValue, effect.valueThreshold);
                    int damage = triggers * effect.magnitude;

                    // Strength: flat bonus per DealDamage invocation
                    damage += player.StatusEffects.Get(EStatusEffect.Strength);

                    // Weak: player deals 25% less (round down)
                    if (player.StatusEffects.Has(EStatusEffect.Weak))
                        damage = (int)(damage * 0.75f);

                    // Vulnerable: enemy receives 50% more (round up)
                    if (enemy.StatusEffects.Has(EStatusEffect.Vulnerable))
                        damage = (int)Math.Ceiling(damage * 1.5);

                    enemy.TakeDamage(Math.Max(0, damage));
                    break;
                }

                case EEffectType.ConditionalDamage:
                {
                    int triggers = PokerEvaluator.EvaluateTriggerCount(
                        effect.triggerOn, targetDice, effect.dieValue, effect.valueThreshold);
                    if (triggers > 0)
                    {
                        int damage = effect.magnitude;
                        damage += player.StatusEffects.Get(EStatusEffect.Strength);
                        if (player.StatusEffects.Has(EStatusEffect.Weak))
                            damage = (int)(damage * 0.75f);
                        if (enemy.StatusEffects.Has(EStatusEffect.Vulnerable))
                            damage = (int)Math.Ceiling(damage * 1.5);
                        enemy.TakeDamage(Math.Max(0, damage));
                    }
                    else
                    {
                        player.TakeDamage(effect.altMagnitude);
                    }
                    break;
                }

                case EEffectType.SelfDamage:
                    player.TakeDamage(effect.magnitude);
                    break;

                case EEffectType.GainBlock:
                {
                    int triggers = PokerEvaluator.EvaluateTriggerCount(
                        effect.triggerOn, targetDice, effect.dieValue, effect.valueThreshold);
                    int blockAmount = triggers * effect.magnitude;

                    // Route to the correct combatant based on diceTarget
                    if (effect.diceTarget == ECardTarget.EnemyDice)
                        enemy.GainBlock(blockAmount);
                    else
                        player.GainBlock(blockAmount);
                    break;
                }

                case EEffectType.RerollDice:
                    if (effect.diceTarget == ECardTarget.PlayerDice)
                        player.Dice.RerollCount(effect.count);
                    else
                        enemy.Dice.RerollCount(effect.count);
                    break;

                case EEffectType.RerollAllDice:
                    if (effect.diceTarget == ECardTarget.PlayerDice)
                        player.Dice.RerollAll();
                    else
                        enemy.Dice.RerollAll();
                    break;

                case EEffectType.RerollByValue:
                    if (effect.diceTarget == ECardTarget.PlayerDice)
                        player.Dice.RerollDiceShowingValue(effect.dieValue);
                    else
                        enemy.Dice.RerollDiceShowingValue(effect.dieValue);
                    break;

                case EEffectType.AddDie:
                    if (effect.diceTarget == ECardTarget.PlayerDice)
                        player.Dice.AddDie();
                    else
                        enemy.Dice.AddDie();
                    break;

                case EEffectType.RemoveDie:
                    if (effect.diceTarget == ECardTarget.PlayerDice)
                        player.Dice.RemoveDie();
                    else
                        enemy.Dice.RemoveDie();
                    break;

                case EEffectType.ApplyStatusEffect:
                    if (effect.diceTarget == ECardTarget.EnemyDice)
                        enemy.StatusEffects.Add(effect.statusEffect, effect.magnitude);
                    else
                        player.StatusEffects.Add(effect.statusEffect, effect.magnitude);
                    break;
            }
        }
    }
}
