using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Linq;
using System.Reflection;


namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/SwitchCrate")]
    public class SwitchCrate : Actor {

        public static ParticleType P_Smash = LightningBreakerBox.P_Smash;

        public static ParticleType P_Impact = TheoCrystal.P_Impact;

        public Vector2 Speed;

        public Holdable Hold;

        private bool dead;

        private Level Level;

        private Collision onCollideH;

        private Collision onCollideV;

        private float noGravityTimer;

        private Vector2 prevLiftSpeed;

        private Vector2 previousPosition;

        private HoldableCollider hitSeeker;

        private float swatTimer;

        float TimeToExplode = 2;

        bool IsMounted = false;

        private EntityID id;

        private DynData<Player> playerDynData;

        private Player player;

        private string FlagName => GetFlagName(id);

        public static Type ConveyorType;

        static Type FloorBoosterType;

        Sprite sprite;

        bool IsHeld = false;

        bool DepleteOnJumpThru = false;

        public SwitchCrate(Vector2 position, EntityID id)
            : base(position) {
            Component ConveyorMoverInstance = (Component) Activator.CreateInstance(ConveyorType);
            Add(ConveyorMoverInstance);
            ConveyorType.GetField("OnMove").SetValue(ConveyorMoverInstance, new Action<float>(MoveOnConveyor));

            this.id = id;
            previousPosition = position;
            Depth = Depths.Pickups;
            Collider = new Hitbox(8f, 11f, -4f, -2f);

            sprite = new Sprite(GFX.Game, "objects/StrawberryJam2021/SwitchCrate/");
            float t = 1f / 6f;
            sprite.Add("idle", "idle", t);
            sprite.Rate = 1/ TimeToExplode;
            Add(sprite);
            sprite.Play("idle");


            sprite.CenterOrigin();
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(20f, 19f, -10f, -10f);
            Hold.SlowFall = false;
            Hold.SlowRun = true;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.DangerousCheck = Dangerous;
            Hold.OnHitSeeker = HitSeeker;
            Hold.OnSwat = Swat;
            Hold.OnHitSpring = HitSpring;
            Hold.OnHitSpinner = HitSpinner;
            Hold.SpeedGetter = () => Speed;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            Hold.OnCarry = pos => Position = pos + new Vector2(0f, -8f);
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
            Tag = Tags.TransitionUpdate;
            Add(new MirrorReflection());

            sprite.OnFinish = delegate
            {
                Die();
            };
        }

        public SwitchCrate(EntityData data, Vector2 offset, EntityID id) : this(data.Position + offset, id) {
            TimeToExplode = data.Float("TimeToExplode");
            DepleteOnJumpThru = data.Bool("DepleteOnJumpThru");
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Level = SceneAs<Level>();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            player = base.Scene.Tracker.GetEntity<Player>();
        }
        void Restartanim() {
            sprite.Play("idle", true);
        }

        public override void Update() {
            base.Update();
            if (!IsMounted) {
                if (swatTimer > 0f) {
                    swatTimer -= Engine.DeltaTime;
                }
                Depth = 100;
                if (Hold.IsHeld) {
                    prevLiftSpeed = Vector2.Zero;
                } else {
                    if (OnGround()) {
                        if (!OnSolidTile()) {
                            Restartanim();
                        }
                        float target = ((!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f)));
                        Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                        Vector2 liftSpeed = LiftSpeed;
                        if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) {
                            Speed = prevLiftSpeed;
                            prevLiftSpeed = Vector2.Zero;
                            Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                            if (Speed.X != 0f && Speed.Y == 0f) {
                                Speed.Y = -60f;
                            }
                            if (Speed.Y < 0f) {
                                noGravityTimer = 0.15f;
                            }
                        } else {
                            prevLiftSpeed = liftSpeed;
                            if (liftSpeed.Y < 0f && Speed.Y < 0f) {
                                Speed.Y = 0f;
                            }
                        }
                    } else { 
                        Restartanim();
                        if (Hold.ShouldHaveGravity) {
                            float num = 800f;
                            if (Math.Abs(Speed.Y) <= 30f) {
                                num *= 0.5f;
                            }
                            float num2 = 350f;
                            if (Speed.Y < 0f) {
                                num2 *= 0.5f;
                            }
                            Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
                            if (noGravityTimer > 0f) {
                                noGravityTimer -= Engine.DeltaTime;
                            } else {
                                Speed.Y = Calc.Approach(Speed.Y, 200f, num * Engine.DeltaTime);
                            }
                        }
                    }
                    previousPosition = ExactPosition;
                    MoveH(Speed.X * Engine.DeltaTime, onCollideH);
                    MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
                    if (base.Center.X > (float) Level.Bounds.Right) {
                        MoveH(32f * Engine.DeltaTime);
                        if (Left - 8f > (float) Level.Bounds.Right) {
                            RemoveSelf();
                        }
                    } else if (Left < (float) Level.Bounds.Left) {
                        Left = Level.Bounds.Left;
                        Speed.X *= -0.4f;
                    } else if (Top < (float) (Level.Bounds.Top - 4)) {
                        Top = Level.Bounds.Top + 4;
                        Speed.Y = 0f;
                    } else if (Bottom > (float) Level.Bounds.Bottom && SaveData.Instance.Assists.Invincible) {
                        Bottom = Level.Bounds.Bottom;
                        Speed.Y = -300f;
                        Audio.Play("event:/game/general/assist_screenbottom", Position);
                    } else if (Top > (float) Level.Bounds.Bottom) {
                        Die();
                    }
                    if (X < (float) (Level.Bounds.Left + 10)) {
                        MoveH(32f * Engine.DeltaTime);
                    }
                    TempleGate templeGate = CollideFirst<TempleGate>();
                    if (templeGate != null && player != null) {
                        templeGate.Collidable = false;
                        MoveH((float) (Math.Sign(player.X - X) * 32) * Engine.DeltaTime);
                        templeGate.Collidable = true;
                    }
                }
                if (!dead) {
                    Hold.CheckAgainstColliders();
                }
                if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold)) {
                    hitSeeker = null;
                }
            }
            if(IsHeld) {
                Restartanim();
            }
        }

        private void OnPickup() {
            Speed = Vector2.Zero;
            AddTag(Tags.Persistent);
            IsHeld = true;
        }

        private void OnRelease(Vector2 force) {
            RemoveTag(Tags.Persistent);
            if (force.X != 0f && force.Y == 0f) {
                force.Y = -0.4f;
            }
            Speed = force * 200f;
            if (Speed != Vector2.Zero) {
                noGravityTimer = 0.1f;
            }
            IsHeld = false;
        }

        private void OnCollideH(CollisionData data) {
            if (data.Hit is DashSwitch) {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }
            if (data.Hit is SwitchCrateHolder) {
                SwitchCrateHolder batterySwitch = data.Hit as SwitchCrateHolder;
                batterySwitch.Hit(this, Vector2.UnitX * Math.Sign(Speed.X));
            }
            if (Math.Abs(Speed.X) > 100f) {
                ImpactParticles(data.Direction);
            }
            Speed.X *= -0.4f;
        }

        private void OnCollideV(CollisionData data) {
            if (data.Hit is DashSwitch) {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }
            if (data.Hit is SwitchCrateHolder) {
                SwitchCrateHolder batterySwitch = data.Hit as SwitchCrateHolder;
                batterySwitch.Hit(this, Vector2.UnitY * (float) Math.Sign(Speed.Y));
            }
            if (Speed.Y > 160f) {
                ImpactParticles(data.Direction);
            }
            if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch)) {
                Speed.Y *= -0.6f;
            } else {
                Speed.Y = 0f;
            }
        }

        public bool OnSolidTile(int downCheck = 1) {
            if(Collide.Check(this, Scene.Tracker.Entities[FloorBoosterType], Position + Vector2.UnitY * downCheck)) {
                return false;
            }
            if (CollideCheckOutside<JumpThru>(Position + Vector2.UnitY * downCheck)) {
                if (DepleteOnJumpThru) {
                    return true;
                }
                return false;
            }
            if (!CollideCheck<SolidTiles>(Position + Vector2.UnitY * downCheck)) {
                    return false;
            }


            return true;
        }

        public void HitSeeker(Seeker seeker) {
            if (!Hold.IsHeld) {
                Speed = (Center - seeker.Center).SafeNormalize(120f);
            }
        }

        public void HitSpinner(Entity spinner) {
            if (!Hold.IsHeld && Speed.Length() < 0.01f && LiftSpeed.Length() < 0.01f && (previousPosition - ExactPosition).Length() < 0.01f && OnGround()) {
                int num = Math.Sign(X - spinner.X);
                if (num == 0) {
                    num = 1;
                }
                Speed.X = (float) num * 120f;
                Speed.Y = -30f;
            }
        }

        public bool HitSpring(Spring spring) {
            if (!Hold.IsHeld) {
                if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f) {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }
                if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f) {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }
                if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f) {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }
            }
            return false;
        }

        protected override void OnSquish(CollisionData data) {
            if (!TrySquishWiggle(data, 3, 3) && !SaveData.Instance.Assists.Invincible) {
                Die();
            }
        }

        public override bool IsRiding(Solid solid) {
            if (Speed.Y == 0f) {
                return base.IsRiding(solid);
            }
            return false;
        }

        public void Die() {
            if (!dead) {
                dead = true;
                Remove(Hold);
                Audio.Play("event:/char/madeline/death", Position);
                Add(new DeathEffect(Color.Gray, base.Center - Position));
                sprite.Visible = false;
                Depth = -1000000;
                AllowPushing = false;
            }
        }

        public bool Dangerous(HoldableCollider holdableCollider) {
            if (!Hold.IsHeld && Speed != Vector2.Zero) {
                return hitSeeker != holdableCollider;
            }
            return false;
        }

        public void Swat(HoldableCollider hc, int dir) {
            if (Hold.IsHeld && hitSeeker == null) {
                swatTimer = 0.1f;
                hitSeeker = hc;
                Hold.Holder.Swat(dir);
            }
        }

        public void Use() {
            SceneAs<Level>().Session.SetFlag(FlagName);
        }

        public static string GetFlagName(EntityID id) {
            return "battery_" + id.Key;
        }

        private void MoveOnConveyor(float amount) {
            float accY = 800f;
            if (Math.Abs(Speed.Y) <= 30f) {
                accY *= 0.5f;
            }
            Speed.Y = Calc.Approach(Speed.Y, 300f, accY * Engine.DeltaTime);
            Speed.X = Calc.Approach(Speed.X, amount, 200f * Engine.DeltaTime);
            MoveH((Speed.X + 36.3f) * Engine.DeltaTime, OnCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, OnCollideV);
        }

        private void SmashParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int) (Height / 8f) * 4;
            } else if (dir == -Vector2.UnitX) {
                direction = (float) Math.PI;
                position = CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int) (Height / 8f) * 4;
            } else if (dir == Vector2.UnitY) {
                direction = (float) Math.PI / 2f;
                position = BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int) (Width / 8f) * 4;
            } else {
                direction = -(float) Math.PI / 2f;
                position = TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int) (Width / 8f) * 4;
            }
            num += 2;
            SceneAs<Level>().Particles.Emit(P_Smash, num, position, positionRange, direction);
        }

        private void ImpactParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f) {
                direction = (float) Math.PI;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.X < 0f) {
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.Y > 0f) {
                direction = -(float) Math.PI / 2f;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
            } else {
                direction = (float) Math.PI / 2f;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
            }
            Level.Particles.Emit(P_Impact, 12, position, positionRange, direction);
        }

        public static void LoadTypes() {
            EverestModule module = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "FactoryHelper");
            Assembly assem = module.GetType().Assembly;
            ConveyorType = assem.GetType("FactoryHelper.Components.ConveyorMover");

            EverestModule module2 = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "VortexHelper");
            Assembly assem2 = module2.GetType().Assembly;
            FloorBoosterType = assem2.GetType("Celeste.Mod.VortexHelper.Entities.FloorBooster");
        }

    }
}
