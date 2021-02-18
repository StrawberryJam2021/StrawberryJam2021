/*
 * Stuff to ask Quantum Spaceman:
 * - tweaking throw multipliers
 * - should an up boost be possible?
 * - adjust gravity value
 * - adjust spring behaviour
 * - adjust platform sine frequency
 * 
 * Stuff that still needs doing:
 * - modifying player movement while held
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Collections;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/AntiGravJelly")]
    class AntiGravJelly : Actor {

        public bool canBoostUp { get; private set; }

        private bool bubble, destroyed = false;
        private float highFrictionTimer, noGravityTimer, downThrowMultiplier, diagThrowXMultiplier, diagThrowYMultiplier, gravity;
        private Vector2 speed, startPosition, prevLiftSpeed;
        private Collision onCollideH, onCollideV;
        private Sprite sprite;
        private Wiggler wiggler;
        private Holdable hold;
        private SineWave platformSine;
        private SoundSource risingSFX;
        private Level level;

        public AntiGravJelly(EntityData data, Vector2 offset) : this(data.Position + offset, data.Bool("bubble", false), data.Float("downThrowMultiplier", 1f),
            data.Float("diagThrowXMultiplier", 1f), data.Float("diagThrowYMultiplier", 1f), data.Float("gravity", -30), data.Bool("canBoostUp", false)) {
        }

        public AntiGravJelly(Vector2 position, bool bubble, float downThrowMultiplier, float diagThrowXMultiplier, float diagThrowYMultiplier, float gravity, bool canBoostUp) : base (position){
            this.bubble = bubble;
            this.downThrowMultiplier = downThrowMultiplier;
            this.diagThrowYMultiplier = diagThrowYMultiplier;
            this.diagThrowXMultiplier = diagThrowXMultiplier;
            this.gravity = gravity;
            startPosition = Position;
            this.canBoostUp = canBoostUp;

            Collider = new Hitbox(8, 10, -4, -10);
            onCollideH = new Collision(CollideHandlerH);
            onCollideV = new Collision(CollideHandlerV);
            Add(sprite = GFX.SpriteBank.Create("glider"));
            sprite.SetColor(Color.Red); // todo: custom sprite instead of flat recolor
            Add(wiggler = Wiggler.Create(0.25f, 4, null, false, false));
            Depth = Depths.Player - 5;
            Add(hold = new Holdable(0.3f));
            hold.PickupCollider = new Hitbox(20, 22, -10, -16);
            hold.SlowFall = true;
            hold.SlowRun = false;
            hold.OnPickup = new Action(PickupHandler);
            hold.OnRelease = new Action<Vector2>(ReleaseHandler);
            hold.SpeedGetter = SpeedGetter;
            hold.OnHitSpring = SpringHandler;
            Add(platformSine = new SineWave(0.3f, 0));
            platformSine.Randomize();
            Add(risingSFX = new SoundSource());
            Add(new WindMover(WindHandler));
        }

        public static void Load() {
            On.Celeste.Player.PickupCoroutine += OnPickupCoroutine;
        }

        public static void Unload() {
            On.Celeste.Player.PickupCoroutine -= OnPickupCoroutine;
        }

        private static IEnumerator OnPickupCoroutine(On.Celeste.Player.orig_PickupCoroutine orig, Player self) {
            
            //Logger.Log("SJ2021/AntiGravJelly", $"self.Holding.Entity.GetType() == Type.GetType(\"Celeste.Mod.StrawberryJam2021.Entities.AntiGravJelly\") = {self.Holding.Entity.GetType() == Type.GetType("Celeste.Mod.StrawberryJam2021.Entities.AntiGravJelly")}");
            //Logger.Log("SJ2021/AntiGravJelly", $"Holding.Entity.GetType() = {self.Holding.Entity.GetType()}, Type.GetType(\"Celeste.Mod.StrawberryJam2021.Entities.AntiGravJelly\") = {Type.GetType("Celeste.Mod.StrawberryJam2021.Entities.AntiGravJelly")}");
            if (!(self.Holding.Entity.GetType() == Type.GetType("Celeste.Mod.StrawberryJam2021.Entities.AntiGravJelly")))
                yield return orig(self);

            DynData<Player> self_gliderBoostTimer = new DynData<Player>(self);
            Func<float> get_self_gliderBoosterTimer = new Func<float>( () => { return self_gliderBoostTimer.Get<float>("gliderBoostTimer"); });
            Action<float> set_self_gliderBoosterTimer = new Action<float>((x) => self_gliderBoostTimer.Set("gliderBoostTimer", x));

            Vector2 self_gliderBoostDir = new DynData<Player>(self).Get<Vector2>("gliderBoostDir");

            DynData<Player> self_varJumpTimer = new DynData<Player>(self);
            Func<float> get_self_varJumpTimer = new Func<float>(() => { return self_varJumpTimer.Get<float>("varJumpTimer"); });
            Action<float> set_self_varJumpTimer = new Action<float>((x) => self_gliderBoostTimer.Set("varJumpTimer", x));

            Vector2 self_carryOffsetTarget = new Vector2(0f, -12f); // not the """correct""" way to do it but it never gets changed soo....why not
            DynData<Player> self_carryOffset = new DynData<Player>(self);

            Action<Vector2> set_self_carryOffset = new Action<Vector2>((x) => self_carryOffset.Set("carryOffset", x));

            bool self_onGround = new DynData<Player>(self).Get<bool>("onGround");
            bool self_holdCannotDuck = new DynData<Player>(self).Get<bool>("holdCannotDuck");

            self.Play("event:/char/madeline/crystaltheo_lift", null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            if (self.Holding != null && self.Holding.SlowFall && get_self_gliderBoosterTimer() - 0.16f > 0f && self_gliderBoostDir.Y > 0f || (self.Speed.Length() > 180f && self.Speed.Y <= 0f)) {
                Audio.Play("event:/new_content/game/10_farewell/glider_platform_dissipate", self.Position);
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
            tween.OnUpdate = delegate (Tween t)
            {
                set_self_carryOffset(curve.GetPoint(t.Eased));
            };
            self.Add(tween);
            yield return tween.Wait();
            self.Speed = oldSpeed;
            self.Speed.Y = Math.Max(self.Speed.Y, 0f);
            set_self_varJumpTimer(varJump);
            self.StateMachine.State = 0;
            if (self.Holding != null && self.Holding.SlowFall) {
                if (get_self_gliderBoosterTimer() > 0f && self_gliderBoostDir.Y > 0f) { // if can yeet and go down, do yeet
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    set_self_gliderBoosterTimer(0f);
                    self.Speed.Y = Math.Max(self.Speed.Y, 240f * self_gliderBoostDir.Y);
                } else if (get_self_gliderBoosterTimer() > 0f && self_gliderBoostDir.Y < 0 && ((AntiGravJelly)self.Holding.Entity).canBoostUp) { // if can yeet, go up *and* canboostup
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    set_self_gliderBoosterTimer(0f);
                    self.Speed.Y = Math.Min(self.Speed.Y, -240f * Math.Abs(self_gliderBoostDir.Y));
                } else if (self_gliderBoostDir.Y > 0f ) { // if too late for yeet and go down, set minimum down speed. TODO anti cheese here?
                    self.Speed.Y = Math.Max(self.Speed.Y, 105f);
                } else if (!((AntiGravJelly) self.Holding.Entity).canBoostUp && self_gliderBoostDir.Y < 0) {
                    self.Speed.Y = Math.Max(oldSpeed.Y, -105f);
                }
                if (self_onGround && Input.MoveY == 1f) {
                    //self.holdCannotDuck = true;
                    self_holdCannotDuck = true;
                }
            }
            yield break;
        }

        public override void Update() {
            if (level.OnInterval(1))
                //Logger.Log("SJ2021/antigravjelly", $"update() - pos: X {Position.X}, Y {Position.Y}");
            if (Scene.OnInterval(0.05f)) {
                // todo glow particles
            }

            // sprite rotation
            float targetAngle = 0;
            if (hold.IsHeld) {
                if (hold.Holder.OnGround(1)) {
                    targetAngle = Calc.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 0.6981317f, -0.6981317f);
                } else {
                    targetAngle = Calc.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 1.0471976f, -1.0471976f);
                }
            }
            sprite.Rotation = Calc.Approach(sprite.Rotation, targetAngle, (float)Math.PI * Engine.DeltaTime);

            // rising sfx handling
            if (hold.IsHeld && !hold.Holder.OnGround(1) && (sprite.CurrentAnimationID.Equals("fall") || sprite.CurrentAnimationID.Equals("fallLoop"))){
                // if held and player falling (rising lol)
                if (!risingSFX.Playing) {
                    Audio.Play("event:/new_content/game/10_farewell/glider_engage", Position);
                    risingSFX.Play("event:/new_content/game/10_farewell/glider_movement", null, 0);
                }
                Vector2 jellySpeed = hold.Holder.Speed;
                Vector2 vector = new Vector2(jellySpeed.X * 0.5f, (jellySpeed.Y > 0f) ? (jellySpeed.Y * 2) : jellySpeed.Y); // if moving down, double speed.y
                float value = Calc.Map(vector.Length(), 0, 120, 0, 0.7f); // shorten vector length to between 0 and 0.7??
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


                    if (OnGround(1)) { // todo: "onceiling". could abuse onGround(vector2, 1) for that?

                        // x correction for sliding off a corner
                        float correction = 0;
                        if (!OnGround(Position + Vector2.UnitX * 3f, 1)) {
                            correction = 20;
                        } else if (!OnGround(Position - Vector2.UnitX * 3f, 1)) {
                            correction = -20;
                        }
                        speed.X = Calc.Approach(speed.X, correction, 800f * Engine.DeltaTime);

                        Vector2 liftspeed = LiftSpeed;
                        if (liftspeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) { // todo what the hecc is this 
                            speed = liftspeed;
                            prevLiftSpeed = Vector2.Zero;
                            speed.Y = Math.Min(speed.Y * 0.6f, 0);

                            //bouncy
                            if (speed.X != 0 && speed.Y == 0) {
                                speed.Y = -60;
                            }
                            if (speed.Y < 0) {
                                noGravityTimer = 0.15f;
                            }
                        } else {
                            prevLiftSpeed = liftspeed;
                            // if liftspeed points up, and speed points up, cancel all up speed??
                            if (liftspeed.Y < 0 && speed.Y < 0) {
                                speed.Y = 0;
                            }
                        }

                    } // if(onGround)
                    else if (hold.ShouldHaveGravity) {
                        float num = 200f; // gravity coefficient
                        if (speed.Y <= 30f) // if moving down not too fast, halve coefficient
                            num *= 0.5f;

                        float xAxisFriction = (speed.Y < 0 || highFrictionTimer <= 0)? 40f : 10f; // if moving down or high friction, use higher friction value
                        speed.X = Calc.Approach(speed.X, 0f, xAxisFriction * Engine.DeltaTime);

                        if (noGravityTimer > 0) { // if no grav, dont do anything
                            noGravityTimer -= Engine.DeltaTime;
                        } else if (level.Wind.Y > 0f) { // if wind goes down, approach stillstand
                            speed.Y = Calc.Approach(speed.Y, 0f, num * Engine.DeltaTime);
                        } else { // else approach a speed of gravity
                            speed.Y = Calc.Approach(speed.Y, gravity, num * Engine.DeltaTime);
                        }
                    }
                    // execute move
                    MoveH(speed.X * Engine.DeltaTime, onCollideH, null);
                    MoveV(speed.Y * Engine.DeltaTime, onCollideV, null);

                    // collide with horizontal screen bounds
                    if(Left < level.Bounds.Left) {
                        Left = level.Bounds.Left;
                        onCollideH(new CollisionData { Direction = -Vector2.UnitX });
                    } else if (Right > level.Bounds.Right) { 
                        Right = level.Bounds.Right;
                        onCollideH(new CollisionData { Direction = Vector2.UnitX });
                    }

                    // remove if above upper level bound
                    if (Bottom < level.Bounds.Top - 16) {
                        RemoveSelf();
                        return;
                    }
                    hold.CheckAgainstColliders();
                } else { // if is bubble and not held
                    Position = startPosition + Vector2.UnitY * platformSine.Value * 1;
                }
                Vector2 one = Vector2.One;
                if (!hold.IsHeld) {
                    if (level.Wind.Y < 0f) { //if wind up
                        PlayOpen();
                    } else {
                        sprite.Play("idle", false, false);
                    }
                } // if (!hold.isheld)
                else if(hold.Holder.Speed.Y > 20f || level.Wind.Y < 0f) { // if held and falling fast or if held and wind up
                    if (level.OnInterval(0.04f)){
                        if (level.Wind.Y < 0) {
                            //todo particle
                            // this.level.ParticlesBG.Emit(Glider.P_GlideUp, 1, this.Position - Vector2.UnitY * 20f, new Vector2(6f, 4f));
                        } else {
                            //todo particle
                            // this.level.ParticlesBG.Emit(Glider.P_Glide, 1, this.Position - Vector2.UnitY * 10f, new Vector2(6f, 4f));
                        }
                    }
                    PlayOpen();

                    // set sprite scale values depending on fastfall or slowfall
                    if (Input.GliderMoveY.Value > 0) {
                        one.X = 0.7f;
                        one.Y = 1.4f;
                    } else if (Input.GliderMoveY.Value < 0) {
                        one.X = 1.2f;
                        one.Y = 0.8f;
                    }
                    Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
                } // if(hold.Holder.Speed.Y > 20f || level.Wind.Y < 0f)
                else { // just play the normal held animation
                    sprite.Play("held", false, false);
                }
                sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, one.Y, Engine.DeltaTime * 2f);
                sprite.Scale.X = Calc.Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * one.X, Engine.DeltaTime * 2f);
                return;
            } // if (!destroyed)
            Position += speed * Engine.DeltaTime; // continue moving in a straight line during despawn animation
        }

        private void PlayOpen() {
            if (!sprite.CurrentAnimationID.Equals("fall") && !sprite.CurrentAnimationID.Equals("fallLoop")) {
                sprite.Play("fall", false, false);
                sprite.Scale = new Vector2(1.5f, 0.6f);
                // todo particles
                // this.level.Particles.Emit(Glider.P_Expand, 16, base.Center + (Vector2.UnitY * -12f).Rotate(this.sprite.Rotation), new Vector2(8f, 3f), -1.5707964f + this.sprite.Rotation);
                if (hold.IsHeld) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                }
            }
        }

        private IEnumerator DestroyAnimationCoroutine() {
            Audio.Play("event:/new_content/game/10_farewell/glider_emancipate", Position);
            sprite.Play("death", false, false);
            yield return 1f;
            RemoveSelf();
            yield break;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Render() {
            if (level.OnInterval(1))
                //Logger.Log("SJ2021/antigravjelly", $"render() - pos: X {Position.X}, Y {Position.Y}");
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
            return new Vector2(-12 + i, (-5 + (int) Math.Round(Math.Sin(Scene.TimeActive + i * 0.4f) * 1.7999999523162842))); // TODO: adjust platform sine frequency
        }

        private void WindHandler(Vector2 windDirection) {
            if (!hold.IsHeld) {
                if (windDirection.X != 0)
                    MoveH(windDirection.X * 0.5f, null, null);
                if (windDirection.Y != 0)
                    MoveV(windDirection.Y, null, null);
            }
        }

        private bool SpringHandler(Spring spring) { //TODO: adjust spring hit speeds?
            if (!hold.IsHeld) {
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

        private Vector2 SpeedGetter() {
            // todo wtf even is a speed getter
            return speed;
        }

        private void ReleaseHandler(Vector2 force) {
            //Logger.Log("SJ2021/antigravjelly", $"ReleaseHandler(force {{X: {force.X}, Y: {force.Y}}}), Input.MoveY.Value = {Input.MoveY.Value}, diagmul ({diagThrowXMultiplier}, {diagThrowYMultiplier}), downmul {downThrowMultiplier}");
            if (force.X == 0f) {
                Audio.Play("event:/new_content/char/madeline/glider_drop", Position);
            }
            AllowPushing = true;
            RemoveTag(Tags.Persistent);
            bool dropped = false;
            if (force == Vector2.Zero) {
                // speed will be set to Vector2.Zero
                dropped = true;
            }
            if (Input.MoveY.Value == -1 && Input.MoveX.Value == 0) {
                force.X = 0;
                force.Y = downThrowMultiplier;
                dropped = true;
            }
            if (dropped) {
                Audio.Play("event:/new_content/char/madeline/glider_drop", Position);
            } else if(force.Y == 0) {
                force.Y = diagThrowYMultiplier;
                force.X = diagThrowXMultiplier * Math.Sign(force.X) ;
            }
            speed = force * 100; 
            wiggler.Start();

        }

        private void PickupHandler() {
            if (bubble) {
                for (int i = 0; i < 24; i++) {
                    // todo emit particles
                    //level.Particles.Emit();
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
            if (data.Hit is DashSwitch)
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * (float) Math.Sign(speed.X));
            string sfx = "event:/new_content/game/10_farewell/glider_wallbounce_" + ((speed.X < 0) ? "left":"right");
            Audio.Play(sfx, Position);
            speed.X *= -1;
            sprite.Scale = new Vector2(0.8f, 1.2f);
        }
        private void CollideHandlerV(CollisionData data) {
            if (Math.Abs(speed.Y) > 8) {
                sprite.Scale = new Vector2(1.2f, 0.8f);
                Audio.Play("event:/new_content/game/10_farewell/glider_land", Position);
            }
            if (speed.Y > 0) { // inverting speed check - speed.Y > 0 = moving downwards
                speed.Y *= -0.5f;
                return;
            }
            speed.Y = 0;
        }
    }
}
