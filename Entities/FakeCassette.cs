using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/FakeCassette")]
    public class FakeCassette : Entity {

        private class UnlockedBSide : Entity {
            public Sprite sprite;

            private float timer;
            private bool shaking;
            private Vector2 shakeVector;
            private float shakeTimer;

            public override void Added(Scene scene) {
                base.Added(scene);
                base.Tag = (int) TagsExt.SubHUD | (int) Tags.PauseUpdate;
                base.Depth = -10000;
                sprite = new Sprite(GFX.Gui, "StrawberryJam2021/fakeCassette/shatter");
                sprite.Add("shatter", "", 0.07f, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22); //this wasn't working for some reason so this was my fix
                sprite.Rate = 0f;
                sprite.Play("shatter");
                sprite.Justify = new Vector2(0.5f, 1f);
            }

            public IEnumerator EaseIn() {
                _ = Scene;
                Level level = Scene as Level;
                level.FormationBackdrop.Display = true;
                while ((level.FormationBackdrop.Alpha += Engine.DeltaTime / 0.75f) < 1f) {
                    yield return null;
                }
                level.FormationBackdrop.Alpha = 1;
                yield return 0.5f;
            }


            public override void Update() {
                timer += Engine.DeltaTime;
                base.Update();
                sprite.Update();
                if (shaking) {
                    shakeTimer += Engine.DeltaTime;
                    //magnitude of the shake vector determined by a cube root function, because they ramp up slowly and cleanly and have f(1) = 1 which lets us use a different function for t < 1 and t > 1
                    //use linear function for values of time below 1, cube root function ramps too fast.
                    shakeVector = 0.01F * Calc.Random.ShakeVector() * (shakeTimer > 1 ? (float) Math.Pow(shakeTimer, 0.33) : shakeTimer); 
                }
            }

            public override void Render() {
                float num = Ease.CubeOut((Scene as Level).FormationBackdrop.Alpha);
                Vector2 vector = global::Celeste.Celeste.TargetCenter + new Vector2(0f, 64f);
                Vector2 vector2 = Vector2.UnitY * 64f * (1f - num);
                sprite.Texture.DrawJustified(vector - vector2 + new Vector2(0f, 32f), new Vector2(0.5f + shakeVector.X, 0.75f + shakeVector.Y), Color.White * num, 1.5f);
            }

            public void Shake() {
                shaking = true;
            }

            public void StopShake() {
                shaking = false;
                shakeTimer = 0;
                shakeVector = Vector2.Zero;
            }
        }

        public static ParticleType P_Shine => Cassette.P_Shine;

        public static ParticleType P_Collect => Cassette.P_Collect;

        private Sprite sprite;

        private SineWave hover;

        private BloomPoint bloom;

        private VertexLight light;

        private Wiggler scaleWiggler;

        private bool collected;

        private bool collecting;

        private string collectAudioEventName, flagOnCollect;

        private EventInstance collectAudioEvent;

        private UnlockedBSide message;
        public FakeCassette(Vector2 position)
            : base(position) {
            base.Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(OnPlayer));
        }

        public FakeCassette(EntityData data, Vector2 offset)
            : this(data.Position + offset) {
            collectAudioEventName = data.Attr("remixEvent");
            flagOnCollect = data.Attr("flagOnCollect");
        }

        public override void Added(Scene scene) {
            if ((scene as Level).Session.GetFlag(flagOnCollect)) {
                RemoveSelf();
                return;
            }
            base.Added(scene);
            Add(sprite = GFX.SpriteBank.Create("cassette"));
            sprite.Play("idle");
            Add(scaleWiggler = Wiggler.Create(0.25f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));
            Add(bloom = new BloomPoint(0.25f, 16f));
            Add(light = new VertexLight(Color.White, 0.4f, 32, 64));
            Add(hover = new SineWave(0.5f, 0f));
            hover.OnUpdate = delegate (float f)
            {
                Sprite obj = sprite;
                VertexLight vertexLight = light;
                float num2 = (bloom.Y = f * 2f);
                float num5 = (obj.Y = (vertexLight.Y = num2));
            };
        }

        public override void Update() {
            base.Update();
            if (!collecting && Scene != null && Scene.OnInterval(0.1f)) {
                SceneAs<Level>().Particles.Emit(P_Shine, 1, base.Center, new Vector2(12f, 10f));
            }
        }

        private void OnPlayer(Player player) {
            if (!collected) {
                player?.RefillStamina();
                collectAudioEvent = Audio.Play(collectAudioEventName, Position);
                collected = true;
                global::Celeste.Celeste.Freeze(0.1f);
                (Scene as Level).StartCutscene((level) => SkipCutscene(level, player), fadeInOnSkip: false);
                Add(new Coroutine(CollectRoutine(player)));
            }
        }

        private IEnumerator CollectRoutine(Player player) {
            collecting = true;
            Level level = Scene as Level;
            List<Entity> blocks = Scene.Tracker.GetEntities<CassetteBlock>();
            level.Frozen = true;
            Tag = Tags.FrozenUpdate;
            level.Session.RespawnPoint = level.GetSpawnPoint(Position);
            level.Session.UpdateLevelStartDashes();
            Depth = -1000000;
            level.Shake();
            level.Flash(Color.White);
            level.Displacement.Clear();
            level.FormationBackdrop.Alpha = 0f;
            Vector2 camWas = level.Camera.Position;
            Vector2 camTo = (Position - new Vector2(160f, 90f)).Clamp(level.Bounds.Left - 64, level.Bounds.Top - 32, level.Bounds.Right + 64 - 320, level.Bounds.Bottom + 32 - 180);
            level.Camera.Position = camTo;
            level.ZoomSnap((Position - level.Camera.Position).Clamp(60f, 60f, 260f, 120f), 2f);
            sprite.Play("spin", restart: true);
            sprite.Rate = 2f;
            for (float p3 = 0f; p3 < 1.5f; p3 += Engine.DeltaTime) {
                sprite.Rate += Engine.DeltaTime * 4f;
                yield return null;
            }
            sprite.Rate = 0f;
            sprite.SetAnimationFrame(0);
            scaleWiggler.Start();
            yield return 0.25f;
            Vector2 from = Position;
            Vector2 to = new Vector2(X, level.Camera.Top - 16f);
            float duration2 = 0.4f;
            for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2) {
                sprite.Scale.X = MathHelper.Lerp(1f, 0.1f, p3);
                sprite.Scale.Y = MathHelper.Lerp(1f, 3f, p3);
                Position = Vector2.Lerp(from, to, Ease.CubeIn(p3));
                yield return null;
            }
            Visible = false;
            message = new UnlockedBSide();
            Scene.Add(message);
            yield return message.EaseIn();
            level.PauseLock = true;
            yield return DoFakeRoutine(player);
            duration2 = 0.25f;
            Add(new Coroutine(level.ZoomBack(duration2 - 0.05f)));
            for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2) {
                level.Camera.Position = Vector2.Lerp(camTo, camWas, Ease.SineInOut(p3));
                yield return null;
            }
            yield return 0.1f;
            player.Active = true;
            yield return GroundPound(player);

            level.EndCutscene();
            level.Frozen = false;
            level.PauseLock = false;
            yield return 0.25f;
            level.ResetZoom();
            RemoveSelf();

        }

        private IEnumerator DoFakeRoutine(Player player) {
            Level level = Scene as Level;
            yield return 1f;
            Glitch.Value = 0.75f;
            message.Shake();
            while (Glitch.Value > 0f) {
                Glitch.Value = Calc.Approach(Glitch.Value, 0f, Engine.RawDeltaTime * 4f);
                level.Shake();
                    
                yield return null;
            }

            yield return 1.1f;
            Glitch.Value = 0.75f;
            while (Glitch.Value > 0f) {
                Glitch.Value = Calc.Approach(Glitch.Value, 0f, Engine.RawDeltaTime * 4f);
                level.Shake();
                yield return null;
            }
            yield return 0.4f;


            Engine.TimeRate = 0f;
            level.Frozen = false;
            player.Active = false;
            player.StateMachine.State = Player.StDummy;
            while (Engine.TimeRate != 1f) {
                Engine.TimeRate = Calc.Approach(Engine.TimeRate, 1f, 0.5f * Engine.RawDeltaTime);
                yield return null;
            }

            message.StopShake();
            message.sprite.Rate = 1f;
            while (message.sprite.Animating) {
                if (message.sprite.CurrentAnimationFrame == 12) {
                    level.Session.SetFlag(flagOnCollect);
                }
                yield return null;
            }
            message.RemoveSelf();
            level.FormationBackdrop.Alpha = 1f;
            level.FormationBackdrop.Display = false;
            Engine.TimeRate = 1f;
            level.Shake();
            Glitch.Value = 0.8f;
            while (Glitch.Value > 0f) {
                Glitch.Value -= Engine.DeltaTime * 4f;
                yield return null;
            }
            yield return 0.25f;
            player.Depth = 0;
        }

        // Also sets flag as a fallback / for effect on skip cutscene
        private IEnumerator GroundPound(Player player) {
            player.StateMachine.State = Player.StDummy;
            while (!player.Dead && !player.OnGround()) {
                yield return null;
            }

            player.StateMachine.ForceState(Player.StTempleFall);
            player.SceneAs<Level>().Session.SetFlag(flagOnCollect, true);
        }

        public void SkipCutscene(Level level, Player player) {
            level.Frozen = false;
            level.Paused = false;
            level.PauseLock = false;
            Glitch.Value = 0f;
            level.FormationBackdrop.Alpha = 1f;
            level.FormationBackdrop.Display = false;
            Audio.Stop(collectAudioEvent);

            player.Add(new Coroutine(GroundPound(player)));

            message?.RemoveSelf();
            level.Camera.Zoom = 1f;
            RemoveSelf();
        }

    }
}
