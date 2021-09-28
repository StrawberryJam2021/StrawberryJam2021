using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    class PocketUmbrella : Actor {
        private float staminaCost;
        private Sprite sprite;
        private Player player;

        public bool destroyed = false, spawning = true;
        public Holdable Hold;
        private Level level;
        private SoundSource fallingSfx;

        private string musicLayer;

        static ParticleType P_Glow, P_Glide, P_GlideUp, P_Expand;

        public static void LoadParticles() {
            P_Glow = new ParticleType(Glider.P_Glow);
            P_Glow.Color = Calc.HexToColor("d34949");
            P_Glow.Color2 = Calc.HexToColor("615a5a");

            P_Glide = new ParticleType(Glider.P_Glide);
            P_Glide.Color = Calc.HexToColor("7a2222");
            P_Glide.Color2 = Calc.HexToColor("c75353");

            P_GlideUp = new ParticleType(Glider.P_GlideUp);

            P_Expand = new ParticleType(Glider.P_Expand);
        }

        public PocketUmbrella(Vector2 position, float cost, string musicLayer = "") : base(position) {
            staminaCost = cost;
            this.musicLayer = musicLayer;

            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("pocketUmbrella"));
            sprite.Visible = false;
            Collider = new Hitbox(8, 10, -4, -10);

            Add(Hold = new Holdable(0.3f));
            Hold.SlowFall = true;
            Hold.SlowRun = false;
            Hold.PickupCollider = new Hitbox(20, 22, -10, -16);
            Hold.OnPickup = new Action(onPickup);

            Add(fallingSfx = new SoundSource());
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        private void onPickup() {
            AddTag(Tags.Persistent);
            Depth = Depths.Player + 1;
            player = Hold.Holder;
            sprite.Visible = true;
            sprite.Play("spawn", true);
            sprite.OnChange = (_, _) => { spawning = false; };
        }

        public override void Render() {
            if (sprite.Visible) {
                sprite.DrawSimpleOutline();
            }
            base.Render();
        }

        public override void Update() {
            if (Hold.IsHeld && !player.OnSafeGround)
                Audio.SetMusicParam(musicLayer, 1);
            else
                Audio.SetMusicParam(musicLayer, 0);

            if (Scene.OnInterval(0.05f)) {
                level.Particles.Emit(P_Glow, 1, Center + Vector2.UnitY * -9f, new Vector2(10f, 4f));
            }

            bool climbUpdate = player.StateMachine.State == Player.StClimb;

            float target;
            if (Hold.IsHeld) {
                if (climbUpdate) {
                    target = Calc.ClampedMap(400 * (int) player.Facing, -300f, 300f, (float) Math.PI / 4.5f, -(float) Math.PI / 4.5f);
                } else if(Hold.Holder.OnGround(1)) {
                    target = Calc.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, (float) Math.PI / 4.5f, -(float) Math.PI / 4.5f);
                } else {
                    target = Calc.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, (float) Math.PI / 3f, -(float) Math.PI / 3f);
                }
            } else {
                target = 0f;
            }
            sprite.Rotation = Calc.Approach(sprite.Rotation, target, (float) Math.PI * Engine.DeltaTime);

            if (Hold.IsHeld && !Hold.Holder.OnGround(1) && (sprite.CurrentAnimationID == "fall" || sprite.CurrentAnimationID == "fallLoop")) {
                if (!fallingSfx.Playing) {
                    Audio.Play("event:/new_content/game/10_farewell/glider_engage", Position);
                    fallingSfx.Play("event:/new_content/game/10_farewell/glider_movement", null, 0f);
                }
                Vector2 speed = Hold.Holder.Speed;
                Vector2 vector = new Vector2(speed.X * 0.5f, (speed.Y < 0f) ? (speed.Y * 2f) : speed.Y);
                float value = Calc.Map(vector.Length(), 0f, 120f, 0f, 0.7f);
                fallingSfx.Param("glider_speed", value);
            } else {
                fallingSfx.Stop(true);
            }
            base.Update();
            if (!destroyed) {
                foreach (SeekerBarrier seekerBarrier in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                    seekerBarrier.Collidable = true;
                    bool flag = CollideCheck(seekerBarrier);
                    seekerBarrier.Collidable = false;
                    if (flag) {
                        destroyed = true;
                        Collidable = false;
                        if (Hold.IsHeld) {
                            Hold.Holder.Drop();
                            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                        }
                        Add(new Coroutine(DestroyAnimationRoutine(), true));
                        return;
                    }
                }
                if (Hold.IsHeld && Hold.Holder.Speed.Y > 20f || level.Wind.Y < 0f) {
                    if (level.OnInterval(0.04f)) {
                        if (level.Wind.Y < 0f) {
                            level.ParticlesBG.Emit(P_GlideUp, 1, Position - Vector2.UnitY * 20f, new Vector2(6f, 4f));
                        } else {
                            level.ParticlesBG.Emit(P_Glide, 1, Position - Vector2.UnitY * 10f, new Vector2(6f, 4f));
                        }
                    }
                    PlayOpen();
                    Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);

                } else if (!spawning) {
                    sprite.Play("held", false, false);

                }

                sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, Vector2.One.Y, Engine.DeltaTime * 2f);
                sprite.Scale.X = Calc.Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * Vector2.One.X, Engine.DeltaTime * 2f);

                if (Hold.IsHeld) {
                    if (!climbUpdate)
                        Hold.Holder.Stamina -= staminaCost * Engine.DeltaTime;
                    if (Hold.Holder.Stamina <= 0) {
                        Hold.Holder.Drop();
                    }
                    return;
                }
                if (!destroyed) {
                    Collidable = false;
                    Hold.Active = false;
                    destroyed = true;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    Add(new Coroutine(DestroyAnimationRoutine()));
                }
                return;
            }
        }

        private void PlayOpen() {
            if (sprite.CurrentAnimationID != "fall" && sprite.CurrentAnimationID != "fallLoop" && !spawning) {
                sprite.Play("fall", false, false);
                sprite.Scale = new Vector2(1.5f, 0.6f);
                level.Particles.Emit(P_Expand, 16, Center + (Vector2.UnitY * -12f).Rotate(sprite.Rotation), new Vector2(8f, 3f), -1.5707964f + sprite.Rotation);
                if (Hold.IsHeld) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                }
            }
        }

        public IEnumerator DestroyAnimationRoutine() {
            sprite.Play("death", true);
            Depth = Depths.Player + 1;
            sprite.OnFinish = (_) => {
                sprite.Visible = false;
            };
            Vector2 offset = Vector2.Zero;
            if (player is not null) {
                offset = Position - player.Position + Vector2.UnitY * 2;
            } else {
                sprite.Visible = false;
            }
            while (sprite.Visible) {
                if (player is not null) {
                    Position = player.Position + offset;
                } else {
                    sprite.Visible = false;
                }
                yield return 0;
            }
            RemoveSelf();
            yield break;
        }
    }
}
