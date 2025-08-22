using System.IO;

namespace Hellscape.Domain {
    public static class SnapshotCodec {
        public static byte[] Encode(WorldSnapshot snapshot) {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            writer.Write(snapshot.tick);
            writer.Write(snapshot.actors.Length);
            
            foreach (var actor in snapshot.actors) {
                writer.Write(actor.id);
                writer.Write(actor.positionX);
                writer.Write(actor.positionY);
                writer.Write(actor.velocityX);
                writer.Write(actor.velocityY);
                writer.Write(actor.hp);
                writer.Write(actor.type);
                writer.Write((byte)actor.team);
                writer.Write(actor.radius);
                writer.Write(actor.alive);
            }
            
            return stream.ToArray();
        }
        
        public static WorldSnapshot Decode(byte[] data) {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            
            var tick = reader.ReadInt32();
            var actorCount = reader.ReadInt32();
            var actors = new ActorState[actorCount];
            
            for (int i = 0; i < actorCount; i++) {
                var id = reader.ReadInt32();
                var positionX = reader.ReadSingle();
                var positionY = reader.ReadSingle();
                var velocityX = reader.ReadSingle();
                var velocityY = reader.ReadSingle();
                var hp = reader.ReadInt16();
                var type = reader.ReadByte();
                var team = (Team)reader.ReadByte();
                var radius = reader.ReadSingle();
                var alive = reader.ReadBoolean();
                
                actors[i] = new ActorState(id, positionX, positionY, velocityX, velocityY, hp, type, team, radius, alive);
            }
            
            return new WorldSnapshot(tick, actors);
        }
    }
}


