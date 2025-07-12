using UnityEngine;
using DWD.Pooling;
using LichLord.World;
using System.Collections.Generic;

namespace LichLord.Props
{
    public class Nexus : Prop
    {
        [SerializeField]
        private InteractableComponent _interactableComponent;

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            base.OnSpawned(propRuntimeState, propManager);

           
        }

        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);

            
        }

      
    }
}
