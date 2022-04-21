using System;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Collections;
using MonoMod.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/AntiGravJelly")]
    class SkyLantern : Actor {

        public bool canBoostUp { get; private set; }

        public float[] riseSpeeds { get; private set; } // downhold, upwind + uphold, uphold, upwind, neutral

        private bool bubble, destroyed;
        private float highFrictionTimer, noGravityTimer, downThrowMultiplier, diagThrowXMultiplier, diagThrowYMultiplier, gravity, lastDroppedTime;
        private Vector2 speed, startPosition, prevLiftSpeed;
        private Collision onCollideH, onCollideV;
        private Sprite sprite;
        private Wiggler wiggler;
        private Holdable hold;
        private SineWave platformSine;
        private SoundSource risingSFX;
        private Level level;
        private VertexLight light;
        private static ParticleType particleGlow, particleExpand, particleGlide, particlePlatform, particleGlideDown;

        public SkyLantern(EntityData data, Vector2 offset) : this(data.Position + offset, data.Bool("bubble", false), data.Float("downThrowMultiplier", 1.8f),
            data.Float("diagThrowXMultiplier", 1.6f), data.Float("diagThrowYMultiplier", 1.8f), data.Float("gravity", -30), data.Bool("canBoostUp", true), data.Attr("riseSpeeds", "-24.0, -176.0, -120.0, -80.0, -40.0")) {
        }

        public SkyLantern(Vector2 position, bool bubble, float downThrowMultiplier, float diagThrowXMultiplier, float diagThrowYMultiplier, float gravity, bool canBoostUp, string riseSpeeds) : base(position) {
            this.bubble = bubble;
            this.downThrowMultiplier = downThrowMultiplier;
            this.diagThrowYMultiplier = diagThrowYMultiplier;
            this.diagThrowXMultiplier = diagThrowXMultiplier;
            this.gravity = gravity;
            startPosition = Position;
            this.canBoostUp = canBoostUp;

            string[] speeds = riseSpeeds.Split(',');
            this.riseSpeeds = new float[speeds.Length];

            for (int i = 0; i < speeds.Length; i++) {
                this.riseSpeeds[i] = float.Parse(speeds[i]);
            }

            lastDroppedTime = -2;

            Collider = new Hitbox(8, 10, -4, -10);
            onCollideH = new Collision(CollideHandlerH);
            onCollideV = new Collision(CollideHandlerV);
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("skyLantern"));
            Add(wiggler = Wiggler.Create(0.25f, 4, null, false, false));
            Depth = Depths.Player - 5;
            Add(hold = new Holdable(0.3f));
            hold.PickupCollider = new Hitbox(20, 22, -10, -16);
            hold.SlowFall = true;
            hold.SlowRun = false;
            hold.OnPickup = new Action(PickupHandler);
            hold.OnRelease = new Action<Vector2>(ReleaseHandler);
            hold.SpeedGetter = () => { return speed; };
            hold.OnHitSpring = SpringHandler;
            Add(platformSine = new SineWave(0.3f, 0));
            platformSine.Randomize();
            Add(risingSFX = new SoundSource());
            Add(new WindMover(WindHandler));
            Add(light = new VertexLight(new Vector2(0, -15), Color.Orange, 1f, 32, 64));
            InitiateParticles();
        }

        public static void Load() {
            On.Celeste.Player.PickupCoroutine += OnPickupCoroutine;
            IL.Celeste.Player.NormalUpdate += patchPlayerNormalUpdate;
            On.Celeste.Player.Throw += patchPlayerThrow;
        }

        public static void Unload() {
            On.Celeste.Player.PickupCoroutine -= OnPickupCoroutine;
            IL.Celeste.Player.NormalUpdate -= patchPlayerNormalUpdate;
            On.Celeste.Player.Throw -= patchPlayerThrow;
        }
        private void InitiateParticles() {
            if (particleGlideDown == null)
                particleGlideDown = new ParticleType {
                    Acceleration = Vector2.UnitY * 60,
                    SpeedMin = 30f,
                    SpeedMax = 40f,
                    Direction = -1.5707964f,
                    DirectionRange = 1.5707964f,
                    LifeMin = 0.6f,
                    LifeMax = 1.2f,
                    ColorMode = ParticleType.ColorModes.Blink,
                    FadeMode = ParticleType.FadeModes.Late,
                    Color = Calc.HexToColor("951a00"),
                    Color2 = Calc.HexToColor("b94f00"),
                    Source = GFX.Game["particles/rect"],
                    Size = 0.5f,
                    SizeRange = 0.2f,
                    RotationMode = ParticleType.RotationModes.Random
                };
            if (particleGlide == null)
                particleGlide = new ParticleType(particleGlideDown) {
                    Acceleration = Vector2.UnitY * -10f,
                    SpeedMin = 50f,
                    SpeedMax = 60f
                };
            if (particlePlatform == null)
                particlePlatform = new ParticleType {
                    Acceleration = Vector2.UnitY * -60f,
                    SpeedMin = 5f,
                    SpeedMax = 20f,
                    Direction = 1 / 2 * (float) Math.PI,
                    LifeMin = 0.6f,
                    LifeMax = 1.4f,
                    FadeMode = ParticleType.FadeModes.Late,
                    Size = 1f
                };
            if (particleGlow == null)
                particleGlow = new ParticleType {
                    SpeedMin = 8f,
                    SpeedMax = 16f,
                    DirectionRange = (float) Math.PI * 2,
                    LifeMin = 0.4f,
                    LifeMax = 0.8f,
                    Size = 1f,
                    FadeMode = ParticleType.FadeModes.Late,
                    Color = Calc.HexToColor("daa600"),
                    Color2 = Calc.HexToColor("da8200"),
                    ColorMode = ParticleType.ColorModes.Blink
                };
            if (particleExpand == null)
                particleExpand = new ParticleType(particleGlow) {
                    SpeedMin = 40f,
                    SpeedMax = 80f,
                    SpeedMultiplier = 0.2f,
                    LifeMin = 0.6f,
                    LifeMax = 1.2f,
                    DirectionRange = 3 / 4 * (float) Math.PI
                };
        }


        private static IEnumerator OnPickupCoroutine(On.Celeste.Player.orig_PickupCoroutine orig, Player self) {

            if (self.Holding.Entity is not SkyLantern jelly) {
                IEnumerator origEnum = orig(self);
                while (origEnum.MoveNext()) yield return origEnum.Current;
                yield break;
            }

            Vector2 self_carryOffsetTarget = new Vector2(0f, -12f); // not the """correct""" way to do it but it never gets changed soo....why not
            DynData<Player> dyndata_player = new DynData<Player>(self);

            Func<float> get_self_gliderBoosterTimer = new Func<float>(() => { return dyndata_player.Get<float>("gliderBoostTimer"); });
            Action<float> set_self_gliderBoosterTimer = new Action<float>((x) => dyndata_player.Set("gliderBoostTimer", x));

            Vector2 self_gliderBoostDir = new DynData<Player>(self).Get<Vector2>("gliderBoostDir");

            Func<float> get_self_varJumpTimer = new Func<float>(() => { return dyndata_player.Get<float>("varJumpTimer"); });
            Action<float> set_self_varJumpTimer = new Action<float>((x) => dyndata_player.Set("varJumpTimer", x));

            Action<Vector2> set_self_carryOffset = new Action<Vector2>((x) => dyndata_player.Set("carryOffset", x));

            bool self_onGround = dyndata_player.Get<bool>("onGround");
            bool self_holdCannotDuck = dyndata_player.Get<bool>("holdCannotDuck");

            self.Play(SFX.char_mad_crystaltheo_lift, null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            if (self.Holding != null && self.Holding.SlowFall && get_self_gliderBoosterTimer() - 0.16f > 0f && self_gliderBoostDir.Y > 0f || (self.Speed.Length() > 180f && self.Speed.Y <= 0f)) {
                Audio.Play(SFX.game_10_glider_platform_dissipate, self.Position);
            }
            Vector2 oldSpeed = self.Speed;
            float varJump = get_self_varJumpTimer();
            self.Speed = Vector2.Zero;
            Vector2 vector = self.Holding.Entity.Position - self.Position;
            Vector2 carryOffsetTarget = self_carryOffsetTarget;
            Vector2 control = new Vector2(vector.X + (float) (Math.Sign(vector.X) * 2), self_carryOffsetTarget.Y - 2f);
            SimpleCurve curve = new SimpleCurve(vector, carryOffsetTarget, control);
            set_self_carryOffset(vector);
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, true);
            tween.OnUpdate = delegate (Tween t) {
                set_self_carryOffset(curve.GetPoint(t.Eased));
            };
            self.Add(tween);
            yield return tween.Wait();
            self.Speed = oldSpeed;
            set_self_varJumpTimer(varJump);
            self.StateMachine.State = 0;
            if (self.Holding != null && self.Holding.SlowFall) {
                if (get_self_gliderBoosterTimer() > 0f && self_gliderBoostDir.Y > 0f) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    set_self_gliderBoosterTimer(0f);
                    self.Speed.Y = Math.Max(self.Speed.Y, 240f * self_gliderBoostDir.Y);
                } else if (get_self_gliderBoosterTimer() > 0f && self_gliderBoostDir.Y < 0 && ((SkyLantern) self.Holding.Entity).canBoostUp) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    set_self_gliderBoosterTimer(0f);
                    self.Speed.Y = Math.Min(self.Speed.Y, -240f * Math.Abs(self_gliderBoostDir.Y));
                } else if (self.Speed.Y > 0f && (get_self_gliderBoosterTimer() <= 0)) {
                    float pickupTimeDiff = self.Scene.TimeActive - jelly.lastDroppedTime;
                    if (pickupTimeDiff < 1.5f) {
                        Logger.Log("SJ2021/AntiGravJelly", $"Anticheese, pickup time diff: {self.Scene.TimeActive - jelly.lastDroppedTime}");
                        self.Speed.Y = -self.Speed.Y * 1.2f;
                    } else {
                        self.Speed.Y = Math.Max(self.Speed.Y, -105f);
                    }
                } else {
                    self.Speed.Y = self.Speed.Y / 2;
                }
                if (self_onGround && Input.MoveY.Value == 1f) {
                    self_holdCannotDuck = true;
                }
            }
            yield break;
        }

        private static void patchPlayerThrow(On.Celeste.Player.orig_Throw orig, Player self) {
            if (self.Holding?.Entity is SkyLantern && Input.MoveY.Value == 1 && Input.MoveX.Value != 0) {
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                self.Holding.Release(Vector2.UnitX * (float) self.Facing);
                self.Speed.X = self.Speed.X + 80f * (float) -(float) self.Facing;
                self.Play(SFX.char_mad_crystaltheo_throw, null, 0f);
                self.Sprite.Play("throw", false, false);
                self.Holding = null;
                return;
            } else if (self.Holding?.Entity is SkyLantern && Input.MoveY.Value == -1) {
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                self.Holding.Release(Vector2.UnitX * (float) self.Facing);
                self.Play(SFX.char_mad_crystaltheo_throw, null, 0f);
                self.Sprite.Play("throw", false, false);
                self.Holding = null;
                return;
            }
            orig(self);
        }

        private static void patchPlayerNormalUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(120f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is SkyLantern jelly) {
                        if (player.SceneAs<Level>().Wind.Y > 0)
                            return jelly.riseSpeeds[0] + 40;
                        return jelly.riseSpeeds[0];
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-32f))) {
                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is SkyLantern jelly) {
                        return jelly.riseSpeeds[1];
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(24f))) {
                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is SkyLantern jelly) {
                        return jelly.riseSpeeds[2];
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0f))) {
                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is SkyLantern jelly) {
                        return jelly.riseSpeeds[3];
                    }
                    return speed;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(40f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((speed, player) => {

                    if (player?.Holding?.Entity is SkyLantern jelly) {
                        return jelly.riseSpeeds[4];
                    }
                    return speed;
                });
            }
        }

        public override void Update() {
            if (Scene.OnInterval(0.05f)) {
                level.Particles.Emit(particleGlow, 1, Center + Vector2.UnitY * -9f, new Vector2(10f, 4f));
            }
            float targetAngle = 0;
            if (hold.IsHeld) {
                if (hold.Holder.OnGround(1)) {
                    targetAngle = Calc.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 0.6981317f, -0.6981317f);
                } else {
                    targetAngle = Calc.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 1.0471976f, -1.0471976f);
                }
            }
            sprite.Rotation = Calc.Approach(sprite.Rotation, targetAngle, (float) Math.PI * Engine.DeltaTime);
            if (hold.IsHeld && !hold.Holder.OnGround(1) && (sprite.CurrentAnimationID.Equals("fall") || sprite.CurrentAnimationID.Equals("fallLoop"))) {
                if (!risingSFX.Playing) {
                    Audio.Play(SFX.game_10_glider_engage, Position);
                    risingSFX.Play(SFX.game_10_glider_movement, null, 0);
                }
                Vector2 jellySpeed = hold.Holder.Speed;
                Vector2 vector = new Vector2(jellySpeed.X * 0.5f, (jellySpeed.Y > 0f) ? (jellySpeed.Y * 2) : jellySpeed.Y);
                float value = Calc.Map(vector.Length(), 0, 120, 0, 0.7f);
                risingSFX.Param("glider_speed", value);
            } else {
                risingSFX.Stop(true);
            }
            base.Update();
            if (!destroyed) {
                foreach (SeekerBarrier seekerBarrier in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                    seekerBarrier.Collidable = true;
                    bool collision = CollideCheck(seekerBarrier);
                    seekerBarrier.Collidable = false;
                    if (collision) {
                        destroyed = true;
                        Collidable = false;
                        if (hold.IsHeld) {
                            Vector2 newSpeed = hold.Holder.Speed;
                            hold.Holder.Drop();
                            speed = newSpeed * 1f / 3;
                            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                        }
                        Add(new Coroutine(DestroyAnimationCoroutine(), true));
                        return;
                    }
                }
                if (hold.IsHeld) {
                    prevLiftSpeed = Vector2.Zero;
                } else if (!bubble) {
                    if (highFrictionTimer > 0f)
                        highFrictionTimer -= Engine.DeltaTime;
                    if (OnGround(-1)) {
                        float correction = 0;
                        if (!OnGround(Position + Vector2.UnitX * 3f, -1)) {
                            correction = 20;
                        } else if (!OnGround(Position - Vector2.UnitX * 3f, -1)) {
                            correction = -20;
                        }
                        speed.X = Calc.Approach(speed.X, correction, 800f * Engine.DeltaTime);
                        Vector2 liftspeed = LiftSpeed;
                        if (liftspeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) {
                            speed = liftspeed;
                            prevLiftSpeed = Vector2.Zero;
                            speed.Y = Math.Min(speed.Y * 0.6f, 0);
                            if (speed.X != 0 && speed.Y == 0) {
                                speed.Y = -60;
                            }
                            if (speed.Y < 0) {
                                noGravityTimer = 0.15f;
                            }
                        } else {
                            prevLiftSpeed = liftspeed;
                            if (liftspeed.Y < 0 && speed.Y < 0) {
                                speed.Y = 0;
                            }
                        }
                    } else if (hold.ShouldHaveGravity) {
                        float num = 200f;
                        if (speed.Y <= 30f)
                            num *= 0.5f;
                        float xAxisFriction = (speed.Y < 0 || highFrictionTimer <= 0) ? 40f : 10f;
                        speed.X = Calc.Approach(speed.X, 0f, xAxisFriction * Engine.DeltaTime);
                        if (noGravityTimer > 0) {
                            noGravityTimer -= Engine.DeltaTime;
                        } else if (level.Wind.Y > 0f) {
                            speed.Y = Calc.Approach(speed.Y, 0f, num * Engine.DeltaTime);
                        } else {
                            speed.Y = Calc.Approach(speed.Y, gravity, num * Engine.DeltaTime);
                        }
                    }
                    MoveH(speed.X * Engine.DeltaTime, onCollideH, null);
                    MoveV(speed.Y * Engine.DeltaTime, onCollideV, null);
                    if (Left < level.Bounds.Left) {
                        Left = level.Bounds.Left;
                        onCollideH(new CollisionData { Direction = -Vector2.UnitX });
                    } else if (Right > level.Bounds.Right) {
                        Right = level.Bounds.Right;
                        onCollideH(new CollisionData { Direction = Vector2.UnitX });
                    }
                    if (Bottom < level.Bounds.Top - 32) {
                        RemoveSelf();
                        return;
                    }
                    hold.CheckAgainstColliders();
                } else {
                    Position = startPosition + Vector2.UnitY * platformSine.Value * 1;
                }
                Vector2 one = Vector2.One;
                if (!hold.IsHeld) {
                    if (level.Wind.Y < 0f) {
                        PlayOpen();
                    } else {
                        sprite.Play("idle", false, false);
                    }
                } else if (hold.Holder.Speed.Y < -20f || level.Wind.Y < 0f) {
                    if (level.OnInterval(0.04f)) {
                        if (level.Wind.Y > 0) {
                            level.ParticlesBG.Emit(particleGlideDown, 1, Position - Vector2.UnitY * 20f, new Vector2(6f, 4f));
                        } else {
                            level.ParticlesBG.Emit(particleGlide, 1, Position - Vector2.UnitY * 10f, new Vector2(6f, 4f));
                        }
                    }
                    PlayOpen();
                    if (Input.GliderMoveY.Value > 0) {
                        one.X = 0.7f;
                        one.Y = 1.4f;
                    } else if (Input.GliderMoveY.Value < 0) {
                        one.X = 1.2f;
                        one.Y = 0.8f;
                    }
                    Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
                } else {
                    sprite.Play("held", false, false);
                }
                sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, one.Y, Engine.DeltaTime * 2f);
                sprite.Scale.X = Calc.Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * one.X, Engine.DeltaTime * 2f);
                return;
            }
            Position += speed * Engine.DeltaTime;
        }

        private void PlayOpen() {
            if (!sprite.CurrentAnimationID.Equals("fall") && !sprite.CurrentAnimationID.Equals("fallLoop")) {
                sprite.Play("fall", false, false);
                sprite.Scale = new Vector2(1.5f, 0.6f);
                level.Particles.Emit(particleExpand, 16, Center + (Vector2.UnitY * -12f).Rotate(sprite.Rotation), new Vector2(8f, 3f), -1 / 2 * (float) Math.PI + sprite.Rotation);
                if (hold.IsHeld) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                }
            }
        }

        private IEnumerator DestroyAnimationCoroutine() {
            Audio.Play(SFX.game_10_glider_emancipate, Position);
            sprite.Play("death", false, false);
            var LightTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, 1f);
            LightTween.Start(true);
            LightTween.OnUpdate = delegate (Tween t) { light.Alpha = t.Eased; };
            Add(LightTween);
            yield return 1f;
            RemoveSelf();
            yield break;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Render() {
            if (!destroyed)
                sprite.DrawSimpleOutline();
            base.Render();
            if (bubble) {
                for (int i = 0; i < 24; i++) {
                    Draw.Point(Position + PlatformAdd(i), PlatformColor(i));
                }
            }
        }

        private Color PlatformColor(int i) {
            if (i <= 1 || i >= 22) {
                return Color.White * 0.4f;
            }
            return Color.White * 0.8f;
        }

        private Vector2 PlatformAdd(int i) {
            return new Vector2(-12 + i, (-5 + (int) Math.Round(Math.Sin(Scene.TimeActive + i * 0.4f) * 1.8)));
        }

        private void WindHandler(Vector2 windDirection) {
            if (!hold.IsHeld) {
                if (windDirection.X != 0)
                    MoveH(windDirection.X * 0.5f, null, null);
                if (windDirection.Y != 0)
                    MoveV(windDirection.Y, null, null);
            }
        }

        private bool SpringHandler(Spring spring) {
            if (!hold.IsHeld) {
                if (spring is UpsidedownSpring udspring) {
                    speed.X = speed.X * udspring.xAxisFriction;
                    speed.Y = 160f * udspring.strength;
                    noGravityTimer = 0.15f;
                    wiggler.Start();
                    return true;
                }
                if (spring.Orientation == Spring.Orientations.Floor && speed.Y >= 0f) {
                    speed.X = speed.X * 0.5f;
                    speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    wiggler.Start();
                    return true;
                }
                if (spring.Orientation == Spring.Orientations.WallLeft && speed.X <= 0f) {
                    MoveTowardsY(spring.CenterY + 5f, 4f, null);
                    speed.X = 160f;
                    speed.Y = 80f;
                    noGravityTimer = 0.1f;
                    wiggler.Start();
                    return true;
                }
                if (spring.Orientation == Spring.Orientations.WallRight && speed.X >= 0f) {
                    MoveTowardsY(spring.CenterY + 5f, 4f, null);
                    speed.X = -160f;
                    speed.Y = 80f;
                    noGravityTimer = 0.1f;
                    wiggler.Start();
                    return true;
                }
            }
            return false;
        }

        protected override void OnSquish(CollisionData data) {
            if (!TrySquishWiggle(data, 3, 3)) {
                RemoveSelf();
            }
        }

        private void ReleaseHandler(Vector2 force) {
            if (force.X == 0f) {
                Audio.Play(SFX.char_mad_glider_drop, Position);
            }
            AllowPushing = true;
            RemoveTag(Tags.Persistent);
            bool dropped = false;
            if (force == Vector2.Zero) {
                // speed will be set to Vector2.Zero later
                dropped = true;
            }
            if (Input.MoveY.Value == -1 && force.X != 0) {
                force.X = 0;
                force.Y = downThrowMultiplier;
                dropped = true;
            }
            if (dropped) {
                lastDroppedTime = Scene.TimeActive;
                Audio.Play(SFX.char_mad_glider_drop, Position);
            } else if (force.Y == 0) {
                force.Y = diagThrowYMultiplier;
                force.X = diagThrowXMultiplier * Math.Sign(force.X);
            }
            speed = force * 100;
            wiggler.Start();
        }

        private void PickupHandler() {
            if (bubble) {
                for (int i = 0; i < 24; i++) {
                    level.Particles.Emit(particlePlatform, Position + PlatformAdd(i), PlatformColor(i));
                }
            }
            AllowPushing = false;
            speed = Vector2.Zero;
            AddTag(Tags.Persistent);
            highFrictionTimer = 0.5f;
            bubble = false;
            wiggler.Start();
        }

        private void CollideHandlerH(CollisionData data) {
            if (data.Hit is DashSwitch dashswitch)
                dashswitch.OnDashCollide(null, Vector2.UnitX * (float) Math.Sign(speed.X));
            string sfx = "event:/new_content/game/10_farewell/glider_wallbounce_" + ((speed.X < 0) ? "left" : "right");
            Audio.Play(sfx, Position);
            speed.X *= -1;
            sprite.Scale = new Vector2(0.8f, 1.2f);
        }

        private void CollideHandlerV(CollisionData data) {
            if (Math.Abs(speed.Y) > 8) {
                sprite.Scale = new Vector2(1.2f, 0.8f);
                Audio.Play(SFX.game_10_glider_land, Position); 
            }
            if (speed.Y > 0) {
                speed.Y *= -0.5f;
                return;
            }
            speed.Y = 0;
        }
    }
}
