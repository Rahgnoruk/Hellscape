using UnityEngine;

namespace Hellscape.Domain
{
    public struct VfxEvent
    {
        public enum Type
        {
            Shot
        }
        
        public Type type;
        public Vector2 from;
        public Vector2 to;
        
        public VfxEvent(Type type, Vector2 from, Vector2 to)
        {
            this.type = type;
            this.from = from;
            this.to = to;
        }
        
        public static VfxEvent Shot(Vector2 from, Vector2 to)
        {
            return new VfxEvent(Type.Shot, from, to);
        }
    }
    
    public interface IVfxEventBroadcaster
    {
        void BroadcastVfxEvent(VfxEvent vfxEvent);
    }
    
    public static class VfxEventSystem
    {
        private static IVfxEventBroadcaster _broadcaster;
        
        public static void SetBroadcaster(IVfxEventBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }
        
        public static void Broadcast(VfxEvent vfxEvent)
        {
            _broadcaster?.BroadcastVfxEvent(vfxEvent);
        }
    }
}
