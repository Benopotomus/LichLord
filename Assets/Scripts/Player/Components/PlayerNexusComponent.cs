using LichLord.Props;
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    public class PlayerNexusComponent : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        public override void Render()
        {
            base.Render();

        }
    }
}
