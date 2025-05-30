using Fusion;
using LichLord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LichLord.Props
{
    public class PropManager : ContextBehaviour
    {
        [Networked]
        [Capacity(512)]
        private NetworkArray<PropChunk> _propChunks { get; }


    }
}
