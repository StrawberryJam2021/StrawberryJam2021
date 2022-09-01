﻿using Celeste.Mod.Entities;
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

            public override void Added(Scene scene) {
                base.Added(scene);
                base.Tag = (int) Tags.HUD | (int) Tags.PauseUpdate;
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
                while ((level.FormationBackdrop.Alpha += Engine.DeltaTime / 0.5f) < 1f) {
                    yield return null;
                }
                level.FormationBackdrop.Alpha = 1;
                yield return 0.5f;
            }


            public override void Update() {
                timer += Engine.DeltaTime;
                base.Update();
                sprite.Update();
                
            }

            public override void Render() {
                float num = Ease.CubeOut((Scene as Level).FormationBackdrop.Alpha);
                Vector2 vector = global::Celeste.Celeste.TargetCenter + new Vector2(0f, 64f);
                Vector2 vector2 = Vector2.UnitY * 64f * (1f - num);
                sprite.Texture.DrawJustified(vector - vector2 + new Vector2(0f, 32f), new Vector2(0.5f, 1f), Color.White * num);
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

        private string collectAudioEvent, flagOnCollect;

        private Vector2[] nodes;

        internal EntityID id;

        public FakeCassette(Vector2 position)
            : base(position) {
            base.Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(OnPlayer));
        }

        public FakeCassette(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset) {
            collectAudioEvent = data.Attr("remixEvent");
            flagOnCollect = data.Attr("flagOnCollect");
            nodes = data.NodesOffset(offset);
            if(nodes.Length < 2) {
                nodes = new Vector2[] { Position, Position };
            }
            this.id = id;
        }

        public override void Added(Scene scene) {
            if ((scene as Level).Session.DoNotLoad.Contains(id)) {
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
            if (!collecting && base.Scene.OnInterval(0.1f)) {
                SceneAs<Level>().Particles.Emit(P_Shine, 1, base.Center, new Vector2(12f, 10f));
            }
        }

        private void OnPlayer(Player player) {
            if (!collected) {
                player?.RefillStamina();
                Audio.Play(collectAudioEvent, Position);
                collected = true;
                global::Celeste.Celeste.Freeze(0.1f);
                Add(new Coroutine(CollectRoutine(player)));
            }
        }

        private IEnumerator CollectRoutine(Player player) {
            collecting = true;
            Level level = Scene as Level;
            List<Entity> blocks = Scene.Tracker.GetEntities<CassetteBlock>();
            level.PauseLock = true;
            level.Frozen = true;
            Tag = Tags.FrozenUpdate;
            level.Session.DoNotLoad.Add(id);
            level.Session.RespawnPoint = level.GetSpawnPoint(Position);
            level.Session.UpdateLevelStartDashes();
            Depth = -1000000;
            level.Shake();
            level.Flash(Color.White);
            level.Displacement.Clear();
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
            UnlockedBSide message = new UnlockedBSide();
            Scene.Add(message);
            yield return message.EaseIn();
            yield return DoFakeRoutine(player, message);
            duration2 = 0.25f;
            Add(new Coroutine(level.ZoomBack(duration2 - 0.05f)));
            for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2) {
                level.Camera.Position = Vector2.Lerp(camTo, camWas, Ease.SineInOut(p3));
                yield return null;
            }
            yield return 0.1f;
            player.Active = true;
            yield return 0.5f;
            level.Frozen = false;
            yield return 0.25f;
            level.PauseLock = false;
            player.StateMachine.State = 0;
            level.ResetZoom();
            level.EndCutscene();
            RemoveSelf();

        }

        private IEnumerator DoFakeRoutine(Player player, UnlockedBSide message) {
            Level level = Scene as Level;
            int panAmount = 64;
            Vector2 panFrom = level.Camera.Position;
            Vector2 panTo = level.Camera.Position + new Vector2(-panAmount, 0f);
            Vector2 birdFrom = new Vector2(panTo.X - 16f, player.Y - 20f);
            Vector2 birdTo = new Vector2(panFrom.X + 320f + 16f, player.Y - 20f);
            yield return 2f;
            Glitch.Value = 0.75f;
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
            message.sprite.Rate = 1f;
            while (message.sprite.Animating) 
                yield return null;
            level.Session.SetFlag(flagOnCollect);
            message.RemoveSelf();
            level.FormationBackdrop.Alpha = 0f;
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
    }
}
