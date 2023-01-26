using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using Celeste.Mod.StrawberryJam2021.Entities;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    [CustomEntity("SJ2021/CS_PrologueOutro")]
    public class CS_PrologueOutro : CutsceneEntity {

        public class TitleLogo : Entity {

            private class Particle {
                public readonly MTexture texture = OVR.Atlas["StrawberryJam2021/sparkle"].GetSubtexture(1, 1, 700, 700);
                public const float DefaultParticleLifetime = 1f;
                public Vector2 offset;
                public float opacity;
                public float size;

                private const float StartingSize = 0.3f;

                private float livedTime;
                private float lifetime;
                private float relSize;
                private float preLivedTime;
                private float appearTime;

                public Particle(float timeOffset) {
                    Reset(timeOffset);
                }

                public void Update() {
                    if (preLivedTime < appearTime) {
                        preLivedTime += Engine.DeltaTime;
                        opacity = 0f;
                        return;
                    }
                    if (livedTime > lifetime) {
                        Reset(0f, rand.NextFloat(DefaultParticleLifetime) + 1f);
                    }
                    livedTime += Engine.DeltaTime;
                    float percLived = livedTime / lifetime;
                    float adjustedEaser = (-4 * (float)Math.Pow(percLived - 0.5f, 2)) + 1;
                    opacity = adjustedEaser;
                    size = ((adjustedEaser / (1 / StartingSize)) + StartingSize) * relSize;
                }

                private void Reset(float time = 0f, float lifetime = DefaultParticleLifetime) {
                    this.lifetime = lifetime;
                    preLivedTime = 0f;
                    appearTime = rand.NextFloat(4f);
                    livedTime = time % lifetime;
                    relSize = rand.NextFloat(1f) + 0.25f;
                    offset = new Vector2(rand.Next(-700, 700), rand.Next(-200, 200));
                    opacity = 0f;
                    size = 0.5f * relSize;
                }
            }

            private const int ParticleCount = 13;
            private ArrayList particles;

            private Sprite sprite;
            private float opacity;
            private float size;

            public Vector2 pos;
            private Vector2 initPos;

            private int renderPhase;

            private static Random rand;

            public TitleLogo() {
                rand = new Random();
                sprite = new Sprite(GFX.Gui, "StrawberryJam2021/logo/");
                renderPhase = 1;
                sprite.AddLoop("wave", "logo", 0.08f);
                sprite.AddLoop("idle", "logoIdle", 1.5f);
                sprite.Play("idle");
                sprite.OnLoop = delegate {
                    renderPhase++;
                    if (renderPhase == 2) {
                        sprite.Play("idle");
                    } else if (renderPhase == 3) {
                        renderPhase = 0;
                        sprite.Play("wave");
                    }
                };
                Tag = Tags.HUD;
                initPos = new Vector2(960, 0);
                pos = initPos;
                opacity = 0f;
                size = 0f;
                particles = new ArrayList();
                for (int i = 0; i < ParticleCount; i++) {
                    particles.Add(new Particle(rand.NextFloat(Particle.DefaultParticleLifetime)));
                }
            }

            public override void Render() {
                sprite.Texture.DrawCentered(pos, Color.White * opacity, size);
                foreach (Particle particle in particles) {
                    particle.texture.DrawCentered(Celeste.TargetCenter + (particle.offset * size), Color.White * particle.opacity * opacity, particle.size * size * 0.15f);
                }
            }

            public override void Update() {
                base.Update();
                sprite.Update();
                foreach (Particle particle in particles) {
                    particle.Update();
                }
            }

            public IEnumerator EaseIn() {
                for (float p = 0f; p < 1f; p += Engine.DeltaTime / 2.5f) {
                    pos = initPos + (Celeste.TargetCenter - initPos) * Ease.CubeOut(p);
                    opacity = Ease.CubeOut(p);
                    size = (Ease.CubeOut(p) / 2) + (1 / 2f);
                    yield return null;
                }
                opacity = 1f;
                size = 1f;
            }
        }

        private Player player;
        private PrologueBasket basket;
        private TitleLogo logo;
        private Vector2 buttonTarget = new Vector2(1728, 972);
        private Vector2 buttonOffScreen = new Vector2(1728, 1188);
        private Vector2 buttonPos;

        public CS_PrologueOutro(Player player, PrologueBasket basket) {
            this.player = player;
            this.basket = basket;
            Tag = Tags.HUD;
            buttonPos = buttonOffScreen;
        }

        public override void Render() {
            base.Render();
            MTexture confirmButton = Input.GuiButton(Input.MenuConfirm, "controls/keyboard/oemquestion");
            confirmButton.DrawCentered(buttonPos, Color.White, 1f);
        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }

        private IEnumerator Cutscene(Level level) {
            player.StateMachine.State = 11;
            player.Dashes = 1;
            Audio.SetMusicParam("outro", 1);
            yield return 0.5f;
            yield return player.DummyWalkTo(basket.X - 12f);
            yield return 0.4f;
            player.Facing = Facings.Right;
            yield return 0.3f;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("sitDown");
            yield return 2f;
            Add(new Coroutine(PanCamera(level)));
            yield return 3f;
            logo = new TitleLogo();
            Scene.Add(logo);
            yield return logo.EaseIn();
            yield return 4f;
            yield return ShowConfirmButton();
            while (!Input.MenuConfirm.Pressed) {
                yield return null;
            }
            EndCutscene(level);
        }

        private IEnumerator ShowConfirmButton() {
            Vector2 src = buttonPos;
            Vector2 dest = buttonTarget;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 3) {
                buttonPos = (dest - src) * Ease.CubeOut(p) + src;
                yield return null;
            }
        }

        private IEnumerator PanCamera(Level level) {
            Vector2 from = level.Camera.Position;
            Vector2 to = from - Vector2.UnitY * 3000f;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 4.5f) {
                level.Camera.Position = Vector2.Lerp(from, to, Ease.CubeInOut(p));
                yield return null;
            }
        }

        public override void OnEnd(Level level) {
            level.CompleteArea(false, false, true);
        }
    }
}
