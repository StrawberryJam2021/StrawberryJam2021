using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ExplodingStrawberry")]
    [RegisterStrawberry(true, false)]
    public class ExplodingStrawberry : Strawberry {
        public ExplodingStrawberry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) { }
    }
}