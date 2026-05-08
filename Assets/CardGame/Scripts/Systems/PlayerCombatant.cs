using System;
using System.Collections.Generic;

namespace LichLord.CardGame
{
    /// <summary>
    /// Runtime state for the player during a combat encounter.
    /// </summary>
    public class PlayerCombatant
    {
        public int MaxHealth     { get; }
        public int CurrentHealth { get; private set; }
        public int Block         { get; private set; }
        public bool IsAlive      => CurrentHealth > 0;

        public DiceManager       Dice          { get; }
        public DeckManager       Deck          { get; }
        public EnergyManager     Energy        { get; }
        public StatusEffectState StatusEffects { get; } = new StatusEffectState();

        /// <summary>Cards with Persistent duration currently in play.</summary>
        public List<CardData> ActivePersistentCards { get; } = new List<CardData>();

        public PlayerCombatant(int maxHealth, int maxEnergy, List<CardData> deck, int diceCount = 5)
        {
            MaxHealth     = maxHealth;
            CurrentHealth = maxHealth;
            Dice   = new DiceManager(diceCount);
            Deck   = new DeckManager(deck);
            Energy = new EnergyManager(maxEnergy);
        }

        /// <summary>
        /// Applies incoming damage after subtracting current block.
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

        /// <summary>
        /// Called at the start of each round: clears block, resets energy,
        /// discards the previous hand, draws a new hand, and rolls all dice.
        /// </summary>
        public void StartRound()
        {
            ClearBlock();
            Energy.ResetEnergy();
            Deck.DiscardHand();
            Deck.DrawCards();
            Dice.RollAll();
        }
    }
}
