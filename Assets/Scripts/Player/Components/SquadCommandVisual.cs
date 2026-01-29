using UnityEngine;

namespace LichLord
{
    public class SquadCommandVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _directionPosition;
        [SerializeField] private GameObject[] _unitFormationPositions;

        public void UpdateVisuals(CommanderComponent component, int squadId)
        {
            var stance = component.GetStance(squadId);

            if (stance == ESquadStance.Follow)
            {
                _directionPosition.transform.position = Vector3.zero;
            }
            else
            {
                var commandTransform = component.GetCommandTransformForSquad(squadId);

                _directionPosition.transform.position = commandTransform.Item1 + (Vector3.up * 0.2f); ;
                _directionPosition.transform.rotation = commandTransform.Item2;
            }

            for (int i = 0; i < _unitFormationPositions.Length; i++)
            {
                var unitPosition = _unitFormationPositions[i];

                if (stance == ESquadStance.Follow)
                {
                    unitPosition.transform.position = Vector3.zero;
                }
                else
                {
                  
                    Vector3 position = component.GetFormationPosition(squadId, i);
                    unitPosition.transform.position = position + (Vector3.up * 0.2f);
                }
            }
        }
    }
}
