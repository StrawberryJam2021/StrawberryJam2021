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

            public string text;

            private bool waitForKeyPress;

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
                ActiveFont.Draw(text, vector + vector2, new Vector2(0.5f, 0f), Vector2.One, Color.White * num);
                if (waitForKeyPress) {
                    GFX.Gui["textboxbutton"].DrawCentered(new Vector2(1824f, 984 + ((timer % 1f < 0.25f) ? 6 : 0)));
                }
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

        private EventInstance remixSfx;

        private bool collecting;

        private string remixEvent, unlockText, flagOnCollect;

        private Vector2[] nodes;

        public FakeCassette(Vector2 position)
            : base(position) {
            base.Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(OnPlayer));
        }

        public FakeCassette(EntityData data, Vector2 offset)
            : this(data.Position + offset) {
            remixEvent = data.Attr("remixEvent");
            if (remixEvent == "")
                remixEvent = null;
            unlockText = data.Attr("unlockText");
            flagOnCollect = data.Attr("flagOnCollect");
            nodes = data.NodesOffset(offset);
            if(nodes.Length < 2) {
                nodes = new Vector2[] { Position, Position };
            }
        }

        public override void Added(Scene scene) {
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

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            Audio.Stop(remixSfx);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(remixSfx);
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
                Audio.Play("event:/game/general/cassette_get", Position);
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
            level.Session.Cassette = true;
            level.Session.RespawnPoint = level.GetSpawnPoint(Position);
            level.Session.UpdateLevelStartDashes();
            SaveData.Instance.RegisterCassette(level.Session.Area);
            foreach (CassetteBlock block in blocks)
                block.SetActivatedSilently(false);
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
            remixSfx = remixEvent == null ? Audio.Play("event:/game/general/cassette_preview", "remix", level.Session.Area.ID) : Audio.Play(remixEvent);
            UnlockedBSide message = new UnlockedBSide();
            message.text = ActiveFont.FontSize.AutoNewline(Dialog.Clean(unlockText), 900);
            Scene.Add(message);
            yield return message.EaseIn();
            yield return DoFakeRoutine(player, message);
            Audio.SetParameter(remixSfx, "end", 1f);
            duration2 = 0.25f;
            Add(new Coroutine(level.ZoomBack(duration2 - 0.05f)));
            for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2) {
                level.Camera.Position = Vector2.Lerp(camTo, camWas, Ease.SineInOut(p3));
                yield return null;
            }

            player.Active = true;
            Audio.Play("event:/game/general/cassette_bubblereturn", level.Camera.Position + new Vector2(160f, 90f));
            player.StartCassetteFly(nodes[1], nodes[0]);
            yield return 0.5f;
            level.Frozen = false;
            yield return 0.25f;
            foreach(CassetteBlock block in blocks)
                block.SetActivatedSilently(true);
            level.PauseLock = false;
            level.Session.SetFlag(flagOnCollect);
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
            player.StateMachine.State = 11;
            while (Engine.TimeRate != 1f) {
                Engine.TimeRate = Calc.Approach(Engine.TimeRate, 1f, 0.5f * Engine.RawDeltaTime);
                yield return null;
            }
            message.sprite.Rate = 1f;
            while (message.sprite.Animating) { Console.WriteLine(message.sprite.CurrentAnimationID + " " + message.sprite.CurrentAnimationFrame); yield return null; }
            message.RemoveSelf();
            level.FormationBackdrop.Alpha = 0f;
            level.FormationBackdrop.Display = false;
            Engine.TimeRate = 1f;
            for (int i = 0; i < 10; i++) {
                Vector2 position = Position + new Vector2(0,96);
                Vector2 value = Position + new Vector2(0, -180f);
                Scene.Add(new AbsorbOrb(position, null, value));
            }
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
