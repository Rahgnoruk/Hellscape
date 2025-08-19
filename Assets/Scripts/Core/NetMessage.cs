using UnityEngine;
using System.Runtime.InteropServices;

namespace Hellscape.Core {
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct InputCommand {
        public int tick;
        public float moveX, moveY;
        public float aimX, aimY;
        public byte buttons; // bitfield: 1=fire,2=alt,4=dash,8=use
    }


    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct ActorState {
        public int id;
        public float x, y;
        public float vx, vy;
        public short hp;
        public byte type; // 0=player,1=enemy
    }


    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct WorldSnapshotHeader { 
        public int tick; 
        public int actorCount; 
    }
}
