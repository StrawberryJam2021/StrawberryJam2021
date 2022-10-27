using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CS_PrologueOutro")]
    public class CS_PrologueOutro : CutsceneEntity {

        public class TitleLogo : Entity {

            private class Particle {
                public readonly MTexture texture = OVR.Atlas["star"].GetSubtexture(1, 1, 256, 256);
                public const float DefaultParticleLifetime = 3f;
                public const float ParticleLifetimeVariance = 1f;
                public Vector2 offset;
                public float opacity;
                public float size;

                private float livedTime;
                private float lifetime;

                public Particle(float timeOffset) {
                    Reset(timeOffset);
                }

                public void Update() {
                    if (livedTime > lifetime) {
                        Reset(0f, Calc.Random.NextFloat(ParticleLifetimeVariance) - (ParticleLifetimeVariance / 2) + DefaultParticleLifetime);
                    }
                    livedTime += Engine.DeltaTime;
                    float percLived = livedTime / lifetime;
                    float adjustedEaser = (-4 * (float)Math.Pow(percLived - 0.5f, 2)) + 1;
                    opacity = adjustedEaser;
                    size = (adjustedEaser / 2) + 0.5f;
                }

                private void Reset(float time = 0f, float lifetime = DefaultParticleLifetime) {
                    this.lifetime = lifetime;
                    livedTime = time % lifetime;
                    offset = new Vector2(Calc.Random.Next(-180, 180), Calc.Random.Next(-90, 90));
                    opacity = 0f;
                    size = 0.5f;
                }
            }

            private const int ParticleCount = 5;
            private ArrayList particles;

            private Sprite sprite;
            private float opacity;
            private float size;

            public TitleLogo() {
                sprite = new Sprite(GFX.Gui, "StrawberryJam2021/logo/logo");
                sprite.Add("idle", "", 0.07f);
                sprite.Play("idle");
                Tag = Tags.HUD;
                opacity = 0f;
                size = 0f;
                particles = new ArrayList();
                for (int i = 0; i < ParticleCount; i++) {
                    particles.Add(new Particle(Calc.Random.NextFloat(Particle.DefaultParticleLifetime)));
                }
            }

            public override void Render() {
                sprite.Texture.DrawCentered(Celeste.TargetCenter, Color.White * opacity, size);
                foreach (Particle particle in particles) {
                    particle.texture.DrawCentered(Celeste.TargetCenter + (particle.offset * size), Color.White * particle.opacity * opacity, particle.size * size * 0.25f);
                }
            }

            public override void Update() {
                base.Update();
                foreach (Particle particle in particles) {
                    particle.Update();
                }
            }

            public IEnumerator EaseIn() {
                for (float p = 0f; p < 1f; p += Engine.DeltaTime / 3) {
                    opacity = Ease.CubeOut(p);
                    size = (Ease.CubeOut(p) / 2) + 0.5f;
                    yield return null;
                }
                opacity = 1f;
                size = 1f;
            }
        }

        private Player player;
        private PrologueBasket basket;
        private TitleLogo logo;
        private int state; // TEMP, allows me to restart the cutscene when it finishes

        public CS_PrologueOutro(Player player, PrologueBasket basket) {
            this.player = player;
            this.basket = basket;
        }

        public override void Update() {
            base.Update();
        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }

        private IEnumerator Cutscene(Level level) {
            state = player.StateMachine.State; // TEMP
            player.StateMachine.State = 11;
            player.Dashes = 1;
            yield return 0.5f;
            yield return player.DummyWalkTo(basket.X - 12f);
            yield return 0.4f;
            player.Facing = Facings.Right;
            yield return 0.3f;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("sitDown");
            yield return 2f;
            yield return PanCamera(level);
            logo = new TitleLogo();
            Scene.Add(logo);
            yield return logo.EaseIn(); // don't know if should be yield return, should people be able to confirm before the logo is fully formed?
            while (!Input.MenuConfirm.Pressed) {
                yield return null;
            }
            logo.Visible = false;
            EndCutscene(level);
        }

        private IEnumerator PanCamera(Level level) {
            float from = level.Camera.Position.Y;
            float to = from - 5000f;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 4) {
                level.Camera.Position = new Vector2(level.Camera.Position.X, from + (to - from) * Ease.CubeInOut(p));
                yield return null;
            }
        }

        public override void OnEnd(Level level) {
            player.StateMachine.State = state; // TEMP
            //level.CompleteArea(false, false, true);
        }
    }
}
