using UnityEngine;
using DWD.Pooling;
using LichLord.World;

namespace LichLord.Props
{
    public class Nexus : Prop
    {

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            base.OnSpawned(propRuntimeState, propManager);
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);
        }
    }
}