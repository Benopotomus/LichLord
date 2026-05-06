using System.Collections.Generic;

namespace LichLord.CardGame
{
    /// <summary>
    /// Tracks all active status effects for one combatant.
    /// Stacks are additive — adding the same status increases its stack count.
    /// </summary>
    public class StatusEffectState
    {
        private readonly List<StatusEntry> _entries = new List<StatusEntry>();

        private struct StatusEntry
        {
            public EStatusEffect effect;
            public int stacks;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adds stacks of the given effect, creating a new entry if none exists.</summary>
        public void Add(EStatusEffect effect, int stacks)
        {
            if (stacks <= 0) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].effect == effect)
                {
                    var e = _entries[i];
                    e.stacks += stacks;
                    _entries[i] = e;
                    return;
                }
            }
            _entries.Add(new StatusEntry { effect = effect, stacks = stacks });
        }

        /// <summary>Returns the current stack count of the given effect, or 0 if not present.</summary>
        public int Get(EStatusEffect effect)
        {
            foreach (var e in _entries)
                if (e.effect == effect) return e.stacks;
            return 0;
        }

        /// <summary>Returns true if the combatant has at least 1 stack of the given effect.</summary>
        public bool Has(EStatusEffect effect) => Get(effect) > 0;

        /// <summary>Removes all stacks of the given effect.</summary>
        public void Remove(EStatusEffect effect)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].effect == effect) { _entries.RemoveAt(i); return; }
        }

        /// <summary>
        /// Decrements all duration-based status effects by 1 and removes any that reach 0.
        /// <see cref="EStatusEffect.Strength"/> and <see cref="EStatusEffect.Dexterity"/> are
        /// permanent and are never decremented here.
        /// </summary>
        public void Tick()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.effect == EStatusEffect.Strength || e.effect == EStatusEffect.Dexterity)
                    continue;

                e.stacks--;
                if (e.stacks <= 0)
                    _entries.RemoveAt(i);
                else
                    _entries[i] = e;
            }
        }

        /// <summary>Removes all active status effects.</summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
