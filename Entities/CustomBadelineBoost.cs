using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using FMOD.Studio;
using Celeste.Mod.StrawberryJam2021.Cutscenes;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CustomBadelineBoost")]
    public class CustomBadelineBoost : Entity {
        private const float MoveSpeed = 320f;

        private readonly BloomPoint bloom;
        private readonly VertexLight light;
        private readonly Vector2[] nodes;
        private readonly SoundSource relocateSfx;
        private readonly Sprite sprite;
        private readonly Image stretch;
        private readonly Wiggler wiggler;

        private Vector2 spawnPoint;
        private string room;
        private string colorGrade;
        private Player.IntroTypes transitionType;
        private Color wipeColor;
        private bool forceCameraUpdate;

        public EventInstance FinalBoostSfx;

        private Player holding;

        private int nodeIndex;
        private bool travelling;

        private CustomBadelineBoost(Vector2[] nodes)
            : base(nodes[0]) {
            Depth = -1000000;
            this.nodes = nodes;
            Collider = new Circle(16f);
            Add(new PlayerCollider(OnPlayer));
            Add(sprite = GFX.SpriteBank.Create("badelineBoost"));
            Add(stretch = new Image(GFX.Game["objects/badelineboost/stretch"]));
            stretch.Visible = false;
            stretch.CenterOrigin();
            Add(light = new VertexLight(Color.White, 0.7f, 12, 20));
            Add(bloom = new BloomPoint(0.5f, 12f));
            Add(wiggler = Wiggler.Create(0.4f, 3f, delegate {
                sprite.Scale = Vector2.One * (float) (1.0 + wiggler.Value * 0.4);
            }));
            Add(relocateSfx = new SoundSource());
        }

        public CustomBadelineBoost(EntityData data, Vector2 offset)
            : this(data.NodesWithPosition(offset)) {
            room = data.Attr("room");
            colorGrade = data.Attr("colorGrade");
            wipeColor = Calc.HexToColor(data.Attr("wipeColor", "ffffff"));
            spawnPoint = new Vector2(data.Int("spawnPointX"), data.Int("spawnPointY"));
            transitionType = data.Enum("transitionType", Player.IntroTypes.Transition);
            forceCameraUpdate = data.Bool("forceCameraUpdate");
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (CollideCheck<FakeWall>()) {
                Depth = -12500;
            }
        }

        private void OnPlayer(Player player) {
            Add(new Coroutine(BoostRoutine(player)));
        }

        private IEnumerator BoostRoutine(Player player) {
            holding = player;
            travelling = true;
            nodeIndex++;
            sprite.Visible = false;
            sprite.Position = Vector2.Zero;
            Collidable = false;
            bool finalBoost = nodeIndex >= nodes.Length;
            Level level = player.SceneAs<Level>();
            if (finalBoost) {
                Audio.Play("event:/new_content/char/badeline/booster_finalfinal_part1", Position);
            } else {
                Audio.Play("event:/char/badeline/booster_begin", Position);
            }
            if (player.Holding != null) {
                player.Drop();
            }
            player.StateMachine.State = 11;
            if (forceCameraUpdate) {
                player.ForceCameraUpdate = true;
            }
            player.DummyAutoAnimate = false;
            player.DummyGravity = false;
            if (player.Inventory.Dashes > 1) {
                player.Dashes = 1;
            } else {
                player.RefillDash();
            }
            player.RefillStamina();
            player.Speed = Vector2.Zero;
            int num = Math.Sign(player.X - X);
            if (num == 0) {
                num = -1;
            }
            LightOptionBadelineDummy badeline = new LightOptionBadelineDummy(Position, !finalBoost);
            Scene.Add(badeline);
            player.Facing = (Facings) (-num);
            badeline.Sprite.Scale.X = num;
            Vector2 playerFrom = player.Position;
            Vector2 playerTo = Position + new Vector2(num * 4, -3f);
            Vector2 badelineFrom = badeline.Position;
            Vector2 badelineTo = Position + new Vector2(-num * 4, 3f);
            for (float p = 0f; (double) p < 1.0; p += Engine.DeltaTime / 0.2f) {
                Vector2 vector = Vector2.Lerp(playerFrom, playerTo, p);
                if (player.Scene != null) {
                    player.MoveToX(vector.X);
                }
                if (player.Scene != null) {
                    player.MoveToY(vector.Y);
                }
                badeline.Position = Vector2.Lerp(badelineFrom, badelineTo, p);
                yield return null;
            }
            if (finalBoost) {
                Vector2 screenSpaceFocusPoint = new Vector2(Calc.Clamp(player.X - level.Camera.X, 120f, 200f), Calc.Clamp(player.Y - level.Camera.Y, 60f, 120f));
                Add(new Coroutine(level.ZoomTo(screenSpaceFocusPoint, 1.5f, 0.18f)));
                Engine.TimeRate = 0.5f;
            } else {
                Audio.Play("event:/char/badeline/booster_throw", Position);
            }
            badeline.Sprite.Play("boost");
            yield return 0.1f;
            if (!player.Dead) {
                player.MoveV(5f);
            }
            yield return 0.1f;
            if (finalBoost) {
                Scene.Add(new CS_CustomLaunch(player, this, room, spawnPoint, colorGrade, transitionType, wipeColor));
                player.Active = false;
                badeline.Active = false;
                Active = false;
                yield return null;
                player.Active = true;
                badeline.Active = true;
            }
            Add(Alarm.Create(Alarm.AlarmMode.Oneshot, delegate {
                if (player.Dashes < player.Inventory.Dashes) {
                    player.Dashes++;
                }
                Scene.Remove(badeline);
                level.Displacement.AddBurst(badeline.Position, 0.25f, 8f, 32f, 0.5f);
            }, 0.15f, start: true));
            level.Shake();
            holding = null;
            if (!finalBoost) {
                player.BadelineBoostLaunch(CenterX);
                Vector2 from = Position;
                Vector2 to = nodes[nodeIndex];
                float val = Vector2.Distance(from, to) / MoveSpeed;
                val = Math.Min(3f, val);
                stretch.Visible = true;
                stretch.Rotation = (to - from).Angle();
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineInOut, val, start: true);
                tween.OnUpdate = delegate (Tween t) {
                    Position = Vector2.Lerp(from, to, t.Eased);
                    stretch.Scale.X = (float) (1.0 + Calc.YoYo(t.Eased) * 2.0);
                    stretch.Scale.Y = (float) (1.0 - Calc.YoYo(t.Eased) * 0.75);
                    if ((double) t.Eased < 0.9f && Scene.OnInterval(0.03f)) {
                        TrailManager.Add(this, Player.TwoDashesHairColor, 0.5f, frozenUpdate: false, useRawDeltaTime: false);
                        level.ParticlesFG.Emit(BadelineBoost.P_Move, 1, Center, Vector2.One * 4f);
                    }
                };
                tween.OnComplete = delegate {
                    if (X >= level.Bounds.Right) {
                        RemoveSelf();
                    } else {
                        travelling = false;
                        stretch.Visible = false;
                        sprite.Visible = true;
                        Collidable = true;
                        Audio.Play("event:/char/badeline/booster_reappear", Position);
                    }
                };
                Add(tween);
                relocateSfx.Play("event:/char/badeline/booster_relocate");
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                level.DirectionalShake(-Vector2.UnitY);
                level.Displacement.AddBurst(Center, 0.4f, 8f, 32f, 0.5f);
            } else {
                FinalBoostSfx = Audio.Play("event:/new_content/char/badeline/booster_finalfinal_part2", Position);
                Engine.FreezeTimer = 0.1f;
                yield return null;
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
                level.Flash(Color.White * 0.5f, drawPlayerOver: true);
                level.DirectionalShake(-Vector2.UnitY, 0.6f);
                level.Displacement.AddBurst(Center, 0.6f, 8f, 64f, 0.5f);
                level.ResetZoom();
                player.SummitLaunch(X);
                Engine.TimeRate = 1f;
                Finish();
            }
            if (forceCameraUpdate) {
                player.ForceCameraUpdate = false;
            }
        }

        public override void Update() {
            if (sprite.Visible && Scene.OnInterval(0.05f)) {
                SceneAs<Level>().ParticlesBG.Emit(BadelineBoost.P_Ambience, 1, Center, Vector2.One * 3f);
            }
            if (holding != null) {
                holding.Speed = Vector2.Zero;
            }
            if (!travelling) {
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (entity != null) {
                    float num = Calc.ClampedMap((entity.Center - Position).Length(), 16f, 64f, 12f, 0f);
                    sprite.Position = Calc.Approach(sprite.Position, (entity.Center - Position).SafeNormalize() * num, 32f * Engine.DeltaTime);
                }
            }
            light.Visible = (bloom.Visible = sprite.Visible || stretch.Visible);
            base.Update();
        }

        private void Finish() {
            SceneAs<Level>().Displacement.AddBurst(Center, 0.5f, 24f, 96f, 0.4f);
            SceneAs<Level>().Particles.Emit(BadelineOldsite.P_Vanish, 12, Center, Vector2.One * 6f);
            SceneAs<Level>().CameraLockMode = Level.CameraLockModes.None;
            SceneAs<Level>().CameraOffset = new Vector2(0f, -16f);
            RemoveSelf();
        }
    }
}
