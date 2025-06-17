using System;

namespace LichLord
{
    [Serializable]
    public struct FDamagePotential
    {
        public int DamageValue;
        public EDamageType DamageType;
        public int StaggerRating;
        public float KnockbackStrength;
        public int ArmorPenetration;

        public void ApplyChargeScalar(float damageScalar)
        {
            DamageValue = (int)(DamageValue * damageScalar);
        }

        public void Copy(FDamagePotential other)
        {
            // Copy the remaining fields
            DamageValue = other.DamageValue;
            DamageType = other.DamageType;
            StaggerRating = other.StaggerRating;
            KnockbackStrength = other.KnockbackStrength;
            ArmorPenetration = other.ArmorPenetration;
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
