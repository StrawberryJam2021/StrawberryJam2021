using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ExplodingStrawberry")]
    [RegisterStrawberry(true, false)]
    [Tracked]
    public class ExplodingStrawberry : Strawberry {
        private Sprite explosionSprite;
        private Vector2 lastPlayerPos;
        public ExplodingStrawberry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) { }

        public override void Added(Scene scene) {
            base.Added(scene);
            explosionSprite = StrawberryJam2021Module.ExplodingStrawberrySpriteBank.Create("explodingStrawberry");
            explosionSprite.Visible = false;
            Add(explosionSprite);
        }

        public override void Update() {
            base.Update();
            Player entity = Scene.Tracker.GetEntity<Player>();
            if (entity != null) {
                lastPlayerPos = entity.Center;
            }
        }

        public override void Render() {
            // Taken and modified from Puffer.cs
            float num1 = 1f;
            if (explosionSprite.Visible) {
                num1 = 1f - (float) explosionSprite.CurrentAnimationFrame / explosionSprite.CurrentAnimationTotalFrames;
            }

            if (num1 > 0.0) {
                bool flag2 = false;
                if (lastPlayerPos.Y < Y) {
                    lastPlayerPos.Y = Y - (float) (((double) lastPlayerPos.Y - Y) * 0.5);
                    lastPlayerPos.X += lastPlayerPos.X - X;
                    flag2 = true;
                }

                float radiansB = (lastPlayerPos - Position).Angle();
                for (int index = 0; index < 14; ++index) {
                    float num2 = (float) Math.Sin(Scene.TimeActive * 0.5) * 0.02f;
                    float num3 =
                        Calc.Map(index / 14f + num2, 0.0f, 1f,
                            0.10415082179037f, 3.246313f);
                    Vector2 vector = Calc.AngleToVector(num3, 1f);
                    Vector2 start = Position - new Vector2(0, 1.5f) + vector * 16f;
                    if (num1 > 0.0) {
                        if (index == 0 || index == 13) {
                            Draw.Line(start, start - vector * 7.5f, Color.White * num1);
                        } else {
                            Vector2 distorted = vector * (float) Math.Sin(Scene.TimeActive * 2.0 +
                                                                          index * 0.600000023841858);
                            if (index % 2 == 0)
                                distorted *= -1f;
                            Vector2 translated = start + distorted;
                            if (!flag2 && Calc.AbsAngleDiff(num3, radiansB) <= 0.174532920122147)
                                Draw.Line(translated, translated - vector * 3f, Color.White * num1);
                            else
                                Draw.Point(translated, Color.White * num1);
                        }
                    }
                }
            }

            base.Render();
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