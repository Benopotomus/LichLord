using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void ApplyDamageToProp(int index, int damage, int tick)
        {
            // Find the state
            PropRuntimeState authorityState = _propRuntimeStates[index];

            // Apply the damage
            authorityState.ApplyDamage(damage, tick);

            ReplicatePropState(authorityState);
        }

        public void Predict_ApplyDamageToProp(int index, int damage, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];

            if (_predictedStates.TryGetValue(index, out var predictedState))
            {
                predictedState.ApplyDamage(damage, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.ApplyDamage(damage, tick);

                //Debug.Log("Creating Predicted Data");
                _predictedStates.Add(index, newPredictedState);
            }
        }
    }
}
