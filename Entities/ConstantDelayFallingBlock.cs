using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/ConstantDelayFallingBlockController")]
    public class ConstantDelayFallingBlockController : Entity {
        public ConstantDelayFallingBlockController(EntityData data, Vector2 offset) : base() { }

        public static void Load() {
            On.Celeste.FallingBlock.PlayerWaitCheck += On_FallingBlock_PlayerWaitCheck;
        }

        public static void Unload() {
            On.Celeste.FallingBlock.PlayerWaitCheck -= On_FallingBlock_PlayerWaitCheck;
        }

        private static bool On_FallingBlock_PlayerWaitCheck(On.Celeste.FallingBlock.orig_PlayerWaitCheck orig, FallingBlock self) {
            if (self.Scene.Tracker.GetEntity<ConstantDelayFallingBlockController>() is not null) {
                return false;
            }
            return orig(self);
        }
    }
}
