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
    }
}
