using System.Collections.Generic;

namespace LichLord
{
    public struct FDamagePotential
    {
        public int DamageValue;
        public EDamageType DamageType;
        public float CriticalChance;
        public float CriticalMultiplier;
        public int StaggerRating;
        public float KnockbackStrength;
        public int ArmorPenetration;
        public EDamageSource DamageSource;

        public void ApplyChargeScalar(float damageScalar)
        {
            DamageValue = (int)(DamageValue * damageScalar);
        }

        public void Copy(FDamagePotential other)
        {
            // Copy the remaining fields
            DamageValue = other.DamageValue;
            DamageType = other.DamageType;
            CriticalChance = other.CriticalChance;
            CriticalMultiplier = other.CriticalMultiplier;
            StaggerRating = other.StaggerRating;
            KnockbackStrength = other.KnockbackStrength;
            ArmorPenetration = other.ArmorPenetration;
            DamageSource = other.DamageSource;
        }
    }

    public enum EDamageSource
    {
        None = 0,
        Physical,
        Magic,
        Consumeable,
    }
}
