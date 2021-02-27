using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ExplodingStrawberry")]
    [RegisterStrawberry(true, false)]
    [Tracked]
    public class ExplodingStrawberry : Strawberry {
        public ExplodingStrawberry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) { }

        public static void Load() {
            On.Celeste.Puffer.Explode += OnPufferExplode;
        }

        public static void Unload() {
            On.Celeste.Puffer.Explode -= OnPufferExplode;
        }

        private static void OnPufferExplode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            foreach (ExplodingStrawberry strawberry in Engine.Scene.Tracker.GetEntities<ExplodingStrawberry>()) {
                strawberry.Components.Add(new Coroutine(Explode(strawberry)));
            }
            orig(self);
        }

        private static IEnumerator Explode(ExplodingStrawberry strawberry) {
            yield return 0.5f;
            if (strawberry.Follower.Leader == null) {
                Engine.Scene.Remove(strawberry);
            }
        }
    }
}