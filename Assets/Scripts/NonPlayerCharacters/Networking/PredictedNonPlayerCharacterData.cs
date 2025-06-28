namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterState
    {
        FNonPlayerCharacterData _data = new FNonPlayerCharacterData();
        public FNonPlayerCharacterData Data => _data;

        NonPlayerCharacterDefinition cachedDefinition;

        public NonPlayerCharacterState(ref FNonPlayerCharacterData data)
        {
            CopyData(ref data);
        }

        public void CopyData(ref FNonPlayerCharacterData other)
        { 
            _data.Copy(ref other);
            cachedDefinition = other.Definition;
        }

        public void ApplyDamage(int damage, int hitReactIndex)
        {
            NonPlayerCharacterDataUtility.ApplyDamage(ref _data, cachedDefinition, damage, hitReactIndex);
        }
    }
}
