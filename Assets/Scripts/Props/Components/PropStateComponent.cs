using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.Props
{
    public class PropStateComponent : MonoBehaviour
    {
        [SerializeField] private Prop _prop;
        public Prop Prop => _prop;

        [SerializeField] private EPropState _currentState = EPropState.Inactive;
        public EPropState CurrentState => _currentState;

        float _hitReactTimeMax = 0.25f;
        float _hitReactTimer = 0.25f;

        float _deadTimeMax = 3.0f;
        float _deadTimer = 3.0f;

        public void UpdateState(EPropState newState)
        {
            if (_currentState == newState)
                return;

            switch (newState)
            {
                case EPropState.Inactive:
                case EPropState.Destroyed:
                    gameObject.SetActive(false);
                    break;
                case EPropState.Idle:
                    gameObject.SetActive(true);
                    break;
            }

            _currentState = newState;
        }

        public void AuthorityUpdate(float renderDeltaTime)
        {
            switch (_currentState)
            {
                case EPropState.HitReact:

                    _hitReactTimer -= renderDeltaTime;
                    if (_hitReactTimer < 0f)
                    {
                        data.State = ENonPlayerState.Idle;
                        NPC.Replicator.UpdateNPCData(data);
                    }
                    break;
                case EPropState.Destroyed:

                    _deadTimer -= renderDeltaTime;
                    if (_deadTimer < 0f)
                    {
                        data.State = ENonPlayerState.Inactive;
                        NPC.Replicator.UpdateNPCData(data);
                    }
                    break;
            }
        }
    }
}
