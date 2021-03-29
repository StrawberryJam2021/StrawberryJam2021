using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using Celeste;
using Celeste.Mod.StrawberryJam2021.Entities;
using FactoryHelper.Entities;
using Celeste.Mod;
using System.Reflection;
using FactoryHelper.Entities;
using FactoryHelper.Components;


namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/SwitchCrate")]
    [Tracked]
    public class SwitchCrate : Actor {

        public static ParticleType P_Smash = LightningBreakerBox.P_Smash;

        public static ParticleType P_Impact = TheoCrystal.P_Impact;

        public Vector2 Speed;

        public Holdable Hold;

        private Image sprite;

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

        float CurrentFloorTime = 0;

        bool IsMounted = false;

        Scene scene;

        private EntityID id;

        private string FlagName => GetFlagName(id);

        Type ConveyorType;

        public SwitchCrate(Vector2 position, EntityID id)
            : base(position) {

            EverestModule module = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "FactoryHelper");
            Assembly assem = module.GetType().Assembly;
            ConveyorType = assem.GetType("FactoryHelper.Components.ConveyorMover");
            Component ConveyorMoverInstance = (Component)Activator.CreateInstance(ConveyorType);
            Add(ConveyorMoverInstance);
            ConveyorType.GetField("OnMove").SetValue(ConveyorMoverInstance, new Action<float>(MoveOnConveyor));


            this.id = id;
            TimeToExplode = TimeToExplode * 60;
            previousPosition = position;
            base.Depth = 100;
            base.Collider = new Hitbox(18f, 18f, 0f, 0f);
            Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate"]));
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(18f, 18f, 0f, 0f);
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
            //Add(ConveyorMover = new ConveyorMover());
            //ConveyorMover.OnMove = MoveOnConveyor;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(base.Collider.Center, Color.White, 1f, 32, 64));
            base.Tag = Tags.TransitionUpdate;
            Add(new MirrorReflection());
        }

        public SwitchCrate(EntityData data, Vector2 offset, EntityID id) : this(data.Position + offset, id) {
        }

        public override void Added(Scene scene1) {
            base.Added(scene1);
            Level = SceneAs<Level>();
            scene = scene1;
        }

        private void MoveOnConveyor(float amount) {
            float accY = 800f;
            if (Math.Abs(Speed.Y) <= 30f) {
                accY *= 0.5f;
            }
            Speed.Y = Calc.Approach(Speed.Y, 300f, accY * Engine.DeltaTime); 
            Speed.X = Calc.Approach(Speed.X, amount, 200f * Engine.DeltaTime);
            MoveH((Speed.X + 36.3f)* Engine.DeltaTime, OnCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, OnCollideV);
        }

        public override void Update() {
            base.Update();
            if (!IsMounted) {
                if (swatTimer > 0f) {
                    swatTimer -= Engine.DeltaTime;
                }
                base.Depth = 100;
                if (Hold.IsHeld) {
                    prevLiftSpeed = Vector2.Zero;
                } else {
                    if (OnGround()) {
                        if (OnFloor()) {
                            CurrentFloorTime++;
                            if (CurrentFloorTime >= TimeToExplode && !dead) {
                                Die();
                                Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_2", Position);
                                SmashParticles(Vector2.UnitX);
                                SmashParticles(-Vector2.UnitX);
                            } else if (CurrentFloorTime == TimeToExplode / 6) {
                                Remove(sprite);
                                Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate2"]));
                            } else if (CurrentFloorTime == TimeToExplode / 6 * 2) {
                                Remove(sprite);
                                Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate3"]));
                            } else if (CurrentFloorTime == TimeToExplode / 6 * 3) {
                                Remove(sprite);
                                Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate4"]));
                            } else if (CurrentFloorTime == TimeToExplode / 6 * 4) {
                                Remove(sprite);
                                Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate5"]));
                            } else if (CurrentFloorTime == TimeToExplode / 6 * 5) {
                                Remove(sprite);
                                Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/SwitchCrate/SwitchCrate6"]));
                            }
                        }
                        float target = ((!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f)));
                        Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                        Vector2 liftSpeed = base.LiftSpeed;
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
                    } else if (Hold.ShouldHaveGravity) {
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
                    previousPosition = base.ExactPosition;
                    MoveH(Speed.X * Engine.DeltaTime, onCollideH);
                    MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
                    if (base.Center.X > (float) Level.Bounds.Right) {
                        MoveH(32f * Engine.DeltaTime);
                        if (base.Left - 8f > (float) Level.Bounds.Right) {
                            RemoveSelf();
                        }
                    } else if (base.Left < (float) Level.Bounds.Left) {
                        base.Left = Level.Bounds.Left;
                        Speed.X *= -0.4f;
                    } else if (base.Top < (float) (Level.Bounds.Top - 4)) {
                        base.Top = Level.Bounds.Top + 4;
                        Speed.Y = 0f;
                    } else if (base.Bottom > (float) Level.Bounds.Bottom && SaveData.Instance.Assists.Invincible) {
                        base.Bottom = Level.Bounds.Bottom;
                        Speed.Y = -300f;
                        Audio.Play("event:/game/general/assist_screenbottom", Position);
                    } else if (base.Top > (float) Level.Bounds.Bottom) {
                        Die();
                    }
                    if (base.X < (float) (Level.Bounds.Left + 10)) {
                        MoveH(32f * Engine.DeltaTime);
                    }
                    Player entity = base.Scene.Tracker.GetEntity<Player>();
                    TempleGate templeGate = CollideFirst<TempleGate>();
                    if (templeGate != null && entity != null) {
                        templeGate.Collidable = false;
                        MoveH((float) (Math.Sign(entity.X - base.X) * 32) * Engine.DeltaTime);
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
        }
        private void SmashParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = base.CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int) (base.Height / 8f) * 4;
            } else if (dir == -Vector2.UnitX) {
                direction = (float) Math.PI;
                position = base.CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int) (base.Height / 8f) * 4;
            } else if (dir == Vector2.UnitY) {
                direction = (float) Math.PI / 2f;
                position = base.BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int) (base.Width / 8f) * 4;
            } else {
                direction = -(float) Math.PI / 2f;
                position = base.TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int) (base.Width / 8f) * 4;
            }
            num += 2;
            SceneAs<Level>().Particles.Emit(P_Smash, num, position, positionRange, direction);
        }

        public void ExplodeLaunch(Vector2 from) {
            if (!Hold.IsHeld) {
                Speed = (base.Center - from).SafeNormalize(120f);
                SlashFx.Burst(base.Center, Speed.Angle());
            }
        }

        public void Swat(HoldableCollider hc, int dir) {
            if (Hold.IsHeld && hitSeeker == null) {
                swatTimer = 0.1f;
                hitSeeker = hc;
                Hold.Holder.Swat(dir);
            }
        }
        public static Type GetModdedTypeByName(string module, string name) {
            var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == module);
            return mod?.GetType().Assembly.GetType(name);
        }

        public bool OnFloor(int downCheck = 1) {
            if (CollideCheckOutside<JumpThru>(Position + Vector2.UnitY * downCheck)) {
                return false;
            }
                if (scene.Tracker.Entities.ContainsKey(typeof(SolidTiles))) {
                    if (!CollideCheck<SolidTiles>(Position + Vector2.UnitY * downCheck)) {
                        return false;
                    }
                }
            
            return true;
        }
        public bool Dangerous(HoldableCollider holdableCollider) {
            if (!Hold.IsHeld && Speed != Vector2.Zero) {
                return hitSeeker != holdableCollider;
            }
            return false;
        }

        public void HitSeeker(Seeker seeker) {
            if (!Hold.IsHeld) {
                Speed = (base.Center - seeker.Center).SafeNormalize(120f);
            }
        }

        public void HitSpinner(Entity spinner) {
            if (!Hold.IsHeld && Speed.Length() < 0.01f && base.LiftSpeed.Length() < 0.01f && (previousPosition - base.ExactPosition).Length() < 0.01f && OnGround()) {
                int num = Math.Sign(base.X - spinner.X);
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
        public void Use() {
                SceneAs<Level>().Session.SetFlag(FlagName);
        }
        public static string GetFlagName(EntityID id) {
            return "battery_" + id.Key;
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

        private void ImpactParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f) {
                direction = (float) Math.PI;
                position = new Vector2(base.Right, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.X < 0f) {
                direction = 0f;
                position = new Vector2(base.Left, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.Y > 0f) {
                direction = -(float) Math.PI / 2f;
                position = new Vector2(base.X, base.Bottom);
                positionRange = Vector2.UnitX * 6f;
            } else {
                direction = (float) Math.PI / 2f;
                position = new Vector2(base.X, base.Top);
                positionRange = Vector2.UnitX * 6f;
            }
            Level.Particles.Emit(P_Impact, 12, position, positionRange, direction);
        }

        public override bool IsRiding(Solid solid) {
            if (Speed.Y == 0f) {
                return base.IsRiding(solid);
            }
            return false;
        }

        protected override void OnSquish(CollisionData data) {
            if (!TrySquishWiggle(data, 3, 3) && !SaveData.Instance.Assists.Invincible) {
                Die();
            }
        }

        private void OnPickup() {
            Speed = Vector2.Zero;
            AddTag(Tags.Persistent);
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
        }

        public void Die() {
            if (!dead) {
                dead = true;
                Remove(Hold);
                Audio.Play("event:/char/madeline/death", Position);
                Add(new DeathEffect(Color.Gray, base.Center - Position));
                sprite.Visible = false;
                base.Depth = -1000000;
                AllowPushing = false;
            }
        }

    }
}
