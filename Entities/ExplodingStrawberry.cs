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
        private Sprite sprite;
        private Sprite explosionSprite;
        private Vector2 lastPlayerPos;
        public ExplodingStrawberry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) { }

        public override void Added(Scene scene) {
            base.Added(scene);
            sprite = Get<Sprite>();
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
            float opacity = 1f;
            if (explosionSprite.Visible) {
                opacity = 1f - (float) Math.Pow(
                    (double) explosionSprite.CurrentAnimationFrame / explosionSprite.CurrentAnimationTotalFrames, 2);
            } else if (sprite.CurrentAnimationID == "collect") {
                opacity = 1f - (float) Math.Pow(
                    (double) sprite.CurrentAnimationFrame / sprite.CurrentAnimationTotalFrames, 2);
            }

            if (opacity > 0) {
                bool belowPlayer = false;
                if (lastPlayerPos.Y < Y) {
                    lastPlayerPos.Y = Y - (float) ((lastPlayerPos.Y - Y) * 0.5);
                    lastPlayerPos.X += lastPlayerPos.X - X;
                    belowPlayer = true;
                }

                float angleFromPlayer = (lastPlayerPos - Position).Angle();
                for (int i = 0; i < 14; ++i) {
                    float offset = (float) Math.Sin(Scene.TimeActive * 0.5) * 0.02f;
                    float angle = Calc.Map(i / 14f + offset, 0.0f, 1f, (float) (Math.PI / 30),
                        (float) (31 * Math.PI / 30));
                    Vector2 vector = Calc.AngleToVector(angle, 1f);
                    Vector2 start = Position - new Vector2(0, 1.5f) + vector * 16f;
                    if (i == 0 || i == 13) {
                        Draw.Line(start, start - vector * 7.5f, Color.White * opacity);
                    } else {
                        Vector2 distorted = vector * (float) Math.Sin(Scene.TimeActive * 2f + i * 0.6f);
                        if (i % 2 == 0) {
                            distorted *= -1f;
                        }

                        Vector2 translated = start + distorted;
                        if (!belowPlayer && Calc.AbsAngleDiff(angle, angleFromPlayer) <= (float) (Math.PI / 18)) {
                            Draw.Line(translated, translated - vector * 3f, Color.White * opacity);
                        } else {
                            Draw.Point(translated, Color.White * opacity);
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
                strawberry.sprite.Visible = false;
                strawberry.explosionSprite.Visible = true;
                strawberry.Add(new Coroutine(Explode(strawberry)));
            }

            orig(self);
        }

        private static IEnumerator Explode(ExplodingStrawberry strawberry) {
            strawberry.explosionSprite.Play(SaveData.Instance.CheckStrawberry(strawberry.ID)
                ? "ghostexplode"
                : "explode");
            while (strawberry.explosionSprite.Animating) {
                if (strawberry.Follower.Leader != null) {
                    strawberry.sprite.Visible = true;
                    strawberry.explosionSprite.Visible = false;
                    yield break;
                }

                yield return null;
            }

            strawberry.RemoveSelf();
        }
    }
}