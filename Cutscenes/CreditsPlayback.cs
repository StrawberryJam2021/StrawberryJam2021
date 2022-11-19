using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    public class CreditsPlayback : PlayerPlayback {
        private Player.ChaserState current;
        private Player player;
        private float dashTrailTimer;
        private int dashTrailCounter;
        private Coroutine dashRoutine;

        public CreditsPlayback(EntityData data, Vector2 offset)
            : base(data.Position + offset, PlayerSpriteMode.Madeline, PlaybackData.Tutorials[data.Attr("tutorial", "")]) {
            Depth = Depths.Player;
            DynamicData.For(this).Set("index", 0);
            Visible = true;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            player = scene.Tracker.GetEntity<Player>();
            if (player != null) {
                player.Visible = player.Collidable = false;
            }
        }

        public override void Update() {
            if (FrameIndex >= FrameCount - 1 || Time >= TrimEnd) {
                RemoveSelf();
                return;
            }

            base.Update();

            if (dashTrailTimer > 0f) {
                dashTrailTimer -= Engine.DeltaTime;
                if (dashTrailTimer <= 0f) {
                    CreateTrail();
                    dashTrailCounter--;
                    if (dashTrailCounter > 0) {
                        dashTrailTimer = 0.1f;
                    }
                }
            }

            if (player != null) {
                player.Position = Position;
            }

            Level level = SceneAs<Level>();
            if (dashRoutine?.Active ?? false && level.OnInterval(0.02f)) {
                level.ParticlesFG.Emit(Player.P_DashA, Center + Calc.Random.Range(Vector2.One * -2f, Vector2.One * 2f), current.DashDirection.Angle());
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (player != null) {
                player.Visible = player.Collidable = true;
            }
        }

        public IEnumerator Wait(float buffer = 1f) {
            yield return Math.Max(0f, Duration - Time - buffer);
        }

        private void Dash() {
            dashRoutine?.Cancel();
            Celeste.Freeze(0.05f);
            dashTrailTimer = 0f;
            dashTrailCounter = 0;
            Add(dashRoutine = new Coroutine(DashCoroutine()));
        }

        private void Jump() {
            Dust.Burst(BottomCenter, (float) -Math.PI / 2f, 4, null);
        }

        private void CreateTrail() {
            Vector2 scale = new(Math.Abs(Sprite.Scale.X) * (float) Hair.Facing, Sprite.Scale.Y);
            TrailManager.Add(this, scale, Player.UsedHairColor, 1f);
        }

        private IEnumerator DashCoroutine() {
            yield return null;

            Level level = SceneAs<Level>();
            level.Displacement.AddBurst(Center, 0.4f, 8f, 64f, 0.5f, Ease.QuadOut, Ease.QuadOut);
            level.DirectionalShake(current.DashDirection, 0.2f);
            SlashFx.Burst(Center, current.DashDirection.Angle());

            CreateTrail();
            dashTrailTimer = 0.08f;
            dashTrailCounter = 1;
            yield return 0.15f;
            CreateTrail();
            yield break;
        }

        internal static void Load() {
            On.Celeste.PlayerPlayback.SetFrame += PlayerPlayback_SetFrame;
        }

        internal static void Unload() {
            On.Celeste.PlayerPlayback.SetFrame -= PlayerPlayback_SetFrame;
        }

        private static void PlayerPlayback_SetFrame(On.Celeste.PlayerPlayback.orig_SetFrame orig, PlayerPlayback self, int index) {
            if (self is CreditsPlayback playback) {
                Player.ChaserState current = playback.current;
                Player.ChaserState next = playback.Timeline[index];

                if (index > 0) {
                    if (current.DashDirection == Vector2.Zero && next.DashDirection != Vector2.Zero) {
                        playback.Dash();
                    }

                    if (current.OnGround != next.OnGround) {
                        playback.Jump();
                    }
                }

                playback.Position = DynamicData.For(playback).Get<Vector2>("start") + next.Position;
                playback.Hair.Color = next.HairColor;
                playback.Sprite.Scale = next.Scale;
                if (next.Scale.X != 0f) {
                    playback.Hair.Facing = (Facings) Math.Sign(next.Scale.X);
                }

                if (next.Animation != playback.Sprite.CurrentAnimationID && playback.Sprite.Has(next.Animation)) {
                    playback.Sprite.Play(next.Animation, restart: true);
                }

                playback.current = next;
                return;
            }

            orig(self, index);
        }
    }
}
