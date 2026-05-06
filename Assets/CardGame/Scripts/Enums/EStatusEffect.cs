namespace LichLord.CardGame
{
    /// <summary>
    /// Status effects that can be applied to either the player or an enemy.
    ///
    /// Duration-based effects (all except Strength and Dexterity) decrement by 1 at the
    /// end of each round and are removed when they reach 0.
    ///
    /// Strength and Dexterity are permanent for the duration of combat.
    /// </summary>
    public enum EStatusEffect
    {
        /// <summary>
        /// The affected combatant deals 25% less damage (rounds down).
        /// One stack = one round of reduced damage.
        /// </summary>
        Weak,

        /// <summary>
        /// The affected combatant receives 50% more damage (rounds up).
        /// One stack = one round of increased vulnerability.
        /// </summary>
        Vulnerable,

        /// <summary>
        /// The affected combatant takes damage equal to current stacks at the start of each round,
        /// then loses 1 stack. Represents fire-over-time damage.
        /// </summary>
        Burning,

        /// <summary>
        /// The affected combatant takes damage equal to current stacks at the start of each round,
        /// then loses 1 stack. Represents poison-over-time damage.
        /// </summary>
        Poisoned,

        /// <summary>
        /// The affected combatant skips their attack phase for 1 round per stack.
        /// (Currently affects the enemy only.)
        /// </summary>
        Stunned,

        /// <summary>
        /// The affected combatant gains 25% less block (rounds down).
        /// One stack = one round of reduced block gains.
        /// </summary>
        Frail,

        /// <summary>
        /// Permanent combat buff. The affected combatant deals extra flat damage equal to stacks
        /// on every DealDamage effect. Does not decrement at end of round.
        /// </summary>
        Strength,

        /// <summary>
        /// Permanent combat buff. The affected combatant gains extra block equal to stacks
        /// on every GainBlock effect. Does not decrement at end of round.
        /// </summary>
        Dexterity,
    }
}
