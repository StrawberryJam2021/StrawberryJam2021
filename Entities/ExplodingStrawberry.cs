using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ExplodingStrawberry")]
    [RegisterStrawberry(true, false)]
    [Tracked]
    public class ExplodingStrawberry : Strawberry {
        private Sprite explosionSprite;
        public ExplodingStrawberry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) { }

        public override void Added(Scene scene) {
            base.Added(scene);
            explosionSprite = StrawberryJam2021Module.ExplodingStrawberrySpriteBank.Create("explodingStrawberry");
            explosionSprite.Visible = false;
            Add(explosionSprite);
        }

        public static void Load() {
            On.Celeste.Puffer.Explode += OnPufferExplode;
        }

        public static void Unload() {
            On.Celeste.Puffer.Explode -= OnPufferExplode;
        }

        private static void OnPufferExplode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            foreach (ExplodingStrawberry strawberry in Engine.Scene.Tracker.GetEntities<ExplodingStrawberry>()) {
                strawberry.Get<Sprite>().Visible = false;
                strawberry.explosionSprite.Visible = true;
                strawberry.Components.Add(new Coroutine(Explode(strawberry)));
            }

            orig(self);
        }

        private static IEnumerator Explode(ExplodingStrawberry strawberry) {
            strawberry.explosionSprite.Play(SaveData.Instance.CheckStrawberry(strawberry.ID)
                ? "ghostexplode"
                : "explode");
            while (strawberry.explosionSprite.Animating) {
                if (strawberry.Follower.Leader != null) {
                    strawberry.Get<Sprite>().Visible = true;
                    strawberry.explosionSprite.Visible = false;
                    yield break;
                }

                yield return null;
            }

            Engine.Scene.Remove(strawberry);
        }
    }
}