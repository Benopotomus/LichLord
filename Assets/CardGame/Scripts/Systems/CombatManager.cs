using System;
using System.Collections.Generic;

namespace LichLord.CardGame
{
    /// <summary>
    /// Drives a single combat encounter between the player and one enemy.
    ///
    /// Turn order each round:
    ///   1. Both sides roll their dice (StartRound).
    ///   2. Enemy pre-round passives fire (rerolls of specific values, etc.).
    ///   3. DOT phase: Burning and Poisoned deal their damage to both combatants.
    ///   4. Persistent card effects apply (block from shield cards, forced enemy rerolls).
    ///   5. Player turn — cards may be played freely while energy allows.
    ///   6. Player calls EndPlayerTurn() to trigger the enemy's phase.
    ///   7. Enemy phase: enemy gains block, applies status effects to player, then deals damage
    ///      (modified by Stunned, Weak, Strength, Vulnerable).
    ///   8. Status effects tick (duration decrements). Enemy block clears.
    ///   9. A new round begins unless combat has ended.
    /// </summary>
    public class CombatManager
    {
        public PlayerCombatant Player { get; private set; }
        public EnemyCombatant  Enemy  { get; private set; }
        public ECombatState    State  { get; private set; } = ECombatState.Idle;

        /// <summary>Fired whenever the combat state changes.</summary>
        public event Action<ECombatState> OnStateChanged;

        /// <summary>Fired at the start of every new round after setup is complete.</summary>
        public event Action OnRoundStarted;

        /// <summary>
        /// Fired when combat ends.
        /// Parameter is true if the player won, false if the player was defeated.
        /// </summary>
        public event Action<bool> OnCombatEnded;

        /// <summary>Fired whenever a combat event worth showing in the log occurs.</summary>
        public event Action<string> OnCombatLog;

        // ── Setup ─────────────────────────────────────────────────────────────────

        /// <summary>Initialises combat and begins the first round.</summary>
        public void StartCombat(EnemyData enemyData, List<CardData> playerDeck,
                                int playerHealth = 30, int playerEnergy = 3)
        {
            Player = new PlayerCombatant(playerHealth, playerEnergy, playerDeck);
            Enemy  = new EnemyCombatant(enemyData);
            BeginRound();
        }

        // ── Player Actions ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to play a card from the player's hand.
        /// Returns false if the card is not in hand, energy is insufficient, or it is not the player's turn.
        /// </summary>
        public bool TryPlayCard(CardData card)
        {
            if (State != ECombatState.PlayerTurn)          return false;
            if (!Player.Energy.CanAfford(card.energyCost)) return false;
            if (!Player.Deck.Hand.Contains(card))          return false;

            Player.Energy.TrySpendEnergy(card.energyCost);
            Player.Deck.PlayCard(card);
            CardEffectProcessor.ProcessCard(card, Player, Enemy);

            if (card.duration == ECardDuration.Persistent)
                Player.ActivePersistentCards.Add(card);

            if (!Enemy.IsAlive)
            {
                SetState(ECombatState.Victory);
                OnCombatEnded?.Invoke(true);
            }

            return true;
        }

        /// <summary>Ends the player's turn and triggers the enemy phase.</summary>
        public void EndPlayerTurn()
        {
            if (State != ECombatState.PlayerTurn) return;
            ExecuteEnemyTurn();
        }

        // ── Private Round Flow ────────────────────────────────────────────────────

        private void BeginRound()
        {
            Player.StartRound();
            Enemy.StartRound();

            // Pre-round enemy passives (e.g., Stone Golem rerolls all [1]s)
            Enemy.ApplyPreRoundEffects();

            // DOT phase: Burning and Poisoned deal damage to both combatants
            ApplyDotEffects();
            if (!Player.IsAlive) { SetState(ECombatState.Defeat);  OnCombatEnded?.Invoke(false); return; }
            if (!Enemy.IsAlive)  { SetState(ECombatState.Victory); OnCombatEnded?.Invoke(true);  return; }

            // Persistent cards apply their recurring effects (block, enemy rerolls)
            ApplyPersistentCardEffects();

            SetState(ECombatState.PlayerTurn);
            OnRoundStarted?.Invoke();
        }

        /// <summary>
        /// Applies Burning and Poisoned DOT damage to both combatants.
        /// Uses TakeDamage so enemy block (if any remains from last round) absorbs first.
        /// </summary>
        private void ApplyDotEffects()
        {
            int playerBurning = Player.StatusEffects.Get(EStatusEffect.Burning);
            if (playerBurning > 0)
            {
                Player.TakeDamage(playerBurning);
                OnCombatLog?.Invoke($"You take {playerBurning} Burning damage.");
            }

            int playerPoison = Player.StatusEffects.Get(EStatusEffect.Poisoned);
            if (playerPoison > 0)
            {
                Player.TakeDamage(playerPoison);
                OnCombatLog?.Invoke($"You take {playerPoison} Poison damage.");
            }

            int enemyBurning = Enemy.StatusEffects.Get(EStatusEffect.Burning);
            if (enemyBurning > 0)
            {
                Enemy.TakeDamage(enemyBurning);
                OnCombatLog?.Invoke($"{Enemy.Data.enemyName} takes {enemyBurning} Burning damage.");
            }

            int enemyPoison = Enemy.StatusEffects.Get(EStatusEffect.Poisoned);
            if (enemyPoison > 0)
            {
                Enemy.TakeDamage(enemyPoison);
                OnCombatLog?.Invoke($"{Enemy.Data.enemyName} takes {enemyPoison} Poison damage.");
            }
        }

        /// <summary>
        /// Re-applies recurring effects from persistent cards.
        /// One-time effects (AddDie, RemoveDie, ApplyStatusEffect) are skipped
        /// — they already fired when the card was first played.
        /// </summary>
        private void ApplyPersistentCardEffects()
        {
            foreach (var card in Player.ActivePersistentCards)
            {
                foreach (var effect in card.effects)
                {
                    if (effect.effectType == EEffectType.AddDie         ||
                        effect.effectType == EEffectType.RemoveDie      ||
                        effect.effectType == EEffectType.ApplyStatusEffect)
                        continue;

                    CardEffectProcessor.ProcessEffect(effect, Player, Enemy);
                }
            }
        }

        private void ExecuteEnemyTurn()
        {
            SetState(ECombatState.EnemyTurn);

            // 1. Enemy gains block from GainBlock passives
            int blockBefore = Enemy.Block;
            Enemy.CalculateAndApplyBlock();
            if (Enemy.Block > blockBefore)
                OnCombatLog?.Invoke($"{Enemy.Data.enemyName} gains {Enemy.Block - blockBefore} Block.");

            // 2. Enemy applies status effects to the player
            Enemy.ApplyStatusEffectsToPlayer(Player);

            // 3. Enemy deals damage (skipped while Stunned)
            if (Enemy.StatusEffects.Has(EStatusEffect.Stunned))
            {
                OnCombatLog?.Invoke($"{Enemy.Data.enemyName} is Stunned and cannot attack!");
            }
            else
            {
                int damage = Enemy.CalculateDamage();

                // Weak: enemy deals 25% less damage (rounds down)
                if (Enemy.StatusEffects.Has(EStatusEffect.Weak))
                    damage = (int)(damage * 0.75f);

                // Strength: enemy deals extra flat damage
                damage += Enemy.StatusEffects.Get(EStatusEffect.Strength);
                damage = Math.Max(0, damage);

                // Vulnerable: player receives 50% more damage (rounds up)
                if (Player.StatusEffects.Has(EStatusEffect.Vulnerable))
                    damage = (int)Math.Ceiling(damage * 1.5);

                int playerBlockBefore = Player.Block;
                Player.TakeDamage(damage);
                int absorbed = Math.Min(playerBlockBefore, damage);
                int dealt    = damage - absorbed;
                if (damage > 0)
                    OnCombatLog?.Invoke($"{Enemy.Data.enemyName} attacks for {damage}" +
                        (absorbed > 0 ? $" ({absorbed} blocked, {dealt} to HP)" : "") + ".");
            }

            // 4. Tick all status effects (duration-based statuses decrement by 1)
            Player.StatusEffects.Tick();
            Enemy.StatusEffects.Tick();

            // 5. Clear enemy block (block does not carry between rounds)
            Enemy.ClearBlock();

            if (!Player.IsAlive)
            {
                SetState(ECombatState.Defeat);
                OnCombatEnded?.Invoke(false);
                return;
            }

            BeginRound();
        }

        private void SetState(ECombatState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
