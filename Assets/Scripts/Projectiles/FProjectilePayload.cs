namespace LichLord
{
    public struct FProjectilePayload
    {
        public FDamagePotential damagePotential;
        //public List<GameplayEffectDefinition> targetGameplayEffects;
        //public List<GameplayEffectDefinition> instigatorGameplayEffects;

        public void Copy(ref FProjectilePayload other)
        {
            damagePotential = other.damagePotential;

            /*
            // Reuse or reassign the lists
            if (targetGameplayEffects == null)
                targetGameplayEffects = new List<GameplayEffectDefinition>(other.targetGameplayEffects?.Count ?? 0);
            else
                targetGameplayEffects.Clear();

            if (other.targetGameplayEffects != null)
                targetGameplayEffects.AddRange(other.targetGameplayEffects);

            if (instigatorGameplayEffects == null)
                instigatorGameplayEffects = new List<GameplayEffectDefinition>(other.instigatorGameplayEffects?.Count ?? 0);
            else
                instigatorGameplayEffects.Clear();

            if (other.instigatorGameplayEffects != null)
                instigatorGameplayEffects.AddRange(other.instigatorGameplayEffects);
            */
        }
    }
}