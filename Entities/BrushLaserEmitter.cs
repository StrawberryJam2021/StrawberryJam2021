using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/BrushLaserEmitterUp = LoadUp",
        "SJ2021/BrushLaserEmitterDown = LoadDown",
        "SJ2021/BrushLaserEmitterLeft = LoadLeft",
        "SJ2021/BrushLaserEmitterRight = LoadRight")]
    public class BrushLaserEmitter : OrientableEntity {
        #region Static Loader Methods

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Up);

        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Down);

        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Left);

        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Right);

        #endregion

        #region Ahorn Properties

        public bool CollideWithSolids { get; }
        public bool KillPlayer { get; }
        public int CassetteIndex { get; }
        public bool HalfLength { get; }

        #endregion

        private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
        private string chargingAnimation => $"{animationPrefix}_charging";
        private string burstAnimation => $"{animationPrefix}_burst";
        private string idleAnimation => $"{animationPrefix}_idle";
        private string cooldownAnimation => $"{animationPrefix}_cooldown";
        private Vector2 beamOffset => Orientation.Offset() * beamOffsetMultiplier;
        private Color telegraphColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);

        private readonly Sprite emitterSprite;
        private readonly Sprite beamSprite;
        private readonly Collider emitterHitbox;
        private readonly Hitbox laserHitbox;
        private readonly ColliderList colliderList;
        private LaserState laserState;

        private const float chargeDelayFraction = 0.25f;
        private const float collisionDelaySeconds = 0.1f;
        private const int beamOffsetMultiplier = 4;
        private const int beamThickness = 12;
        private const float mediumRumbleEffectRange = 8f * 12;
        private const float strongRumbleEffectRange = 8f * 8;

        private static readonly ParticleType blueCooldownParticle = new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/blob"],
            Color = Calc.HexToColor("42bfe8"),
            Color2 = Calc.HexToColor("7550e8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.5f,
            LifeMax = 0.8f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 5.712389f,
            DirectionRange = 1.17453292f,
            SpeedMin = 40f,
            SpeedMax = 100f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        private static readonly ParticleType pinkCooldownParticle = new ParticleType(blueCooldownParticle) {
            Color = Calc.HexToColor("e84292"),
            Color2 = Calc.HexToColor("9c2a70"),
        };

        private static readonly ParticleType blueImpactParticle = new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/fire"],
            Color = Calc.HexToColor("ffffff"),
            Color2 = Calc.HexToColor("73efe8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.3f,
            LifeMax = 0.5f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 4.712389f,
            DirectionRange = 3.14159f,
            SpeedMin = 10f,
            SpeedMax = 80f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        private static readonly ParticleType pinkImpactParticle = new ParticleType(blueImpactParticle) {
            Color2 = Calc.HexToColor("ef73bf"),
        };

        private void setAnimationSpeed(string key, float totalRunTime) {
            if (emitterSprite.Animations.TryGetValue(key, out var emitterAnimation))
                emitterAnimation.Delay = totalRunTime / emitterAnimation.Frames.Length;
            if (beamSprite.Animations.TryGetValue(key, out var beamAnimation))
                beamAnimation.Delay = totalRunTime / beamAnimation.Frames.Length;
        }

        public LaserState State {
            get => laserState;
            set {
                if (laserState == value)
                    return;

                var oldState = laserState;
                laserState = value;

                switch (value) {
                    case LaserState.Idle:
                    case LaserState.Precharge:
                        if (value == LaserState.Idle && oldState == LaserState.Firing) {
                            emitCooldownParticles();
                            emitterSprite.Play(cooldownAnimation);
                        } else {
                            emitterSprite.Play(idleAnimation);
                        }

                        beamSprite.Visible = false;
                        Collider = emitterHitbox;
                        break;

                    case LaserState.Charging:
                        emitterSprite.Play(chargingAnimation);
                        beamSprite.Visible = false;
                        Collider = emitterHitbox;
                        Add(new Coroutine(impactParticlesSequence()));
                        break;

                    case LaserState.Burst:
                        emitterSprite.Play(burstAnimation);
                        playNearbyEffects();
                        beamSprite.Visible = true;
                        beamSprite.Play(burstAnimation);
                        Collider = emitterHitbox;
                        break;

                    case LaserState.Firing:
                        beamSprite.Visible = true;
                        Collider = colliderList;
                        break;
                }
            }
        }

        private void playNearbyEffects() {
            if (Scene.Tracker.Entities.ContainsKey(typeof(Player)) && Scene.Tracker.GetEntity<Player>() is { } player) {
                float distanceSquared = (player.Position - Position).LengthSquared();
                if (distanceSquared <= strongRumbleEffectRange * strongRumbleEffectRange) {
                    SceneAs<Level>().Shake(0.2f);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                } else if (distanceSquared <= mediumRumbleEffectRange * mediumRumbleEffectRange) {
                    SceneAs<Level>().Shake(0.1f);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                } else {
                    Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
                }
            }
        }

        public BrushLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            CollideWithSolids = data.Bool("collideWithSolids", true);
            KillPlayer = data.Bool("killPlayer", true);
            CassetteIndex = data.Int("cassetteIndex", 0);
            HalfLength = data.Bool("halfLength");

            emitterSprite = StrawberryJam2021Module.SpriteBank.Create("brushLaserEmitter");
            emitterSprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up ? new Vector2(-1, 1) : Vector2.One;
            emitterSprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down ? (float)Math.PI / 2f : 0f;
            emitterSprite.Position = Vector2.Zero;
            emitterSprite.Play(idleAnimation);

            beamSprite = StrawberryJam2021Module.SpriteBank.Create("brushLaserBeam");
            beamSprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up ? new Vector2(-1, 1) : Vector2.One;
            beamSprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down ? (float)Math.PI / 2f : 0f;
            beamSprite.Position = beamOffset;
            beamSprite.Visible = false;

            Add(new CassetteListener {
                    OnBeat = state => {
                        int beat = state.Beat % state.BeatsPerTick;
                        float beatFraction = (float)beat / state.BeatsPerTick;

                        switch (State) {
                            case LaserState.Idle:
                                if (beat == 0 && state.CurrentTick.Index != CassetteIndex && state.NextTick.Index == CassetteIndex) {
                                    State = LaserState.Precharge;
                                }
                                break;

                            case LaserState.Precharge:
                                if (beatFraction >= chargeDelayFraction) {
                                    setAnimationSpeed(chargingAnimation, state.TickLength * (1 - chargeDelayFraction));
                                    State = LaserState.Charging;
                                }
                                break;

                            case LaserState.Charging:
                                if (beat == 0 && state.CurrentTick.Index == CassetteIndex) {
                                    State = LaserState.Burst;
                                }
                                break;

                            case LaserState.Burst:
                                if (beatFraction * state.TickLength >= collisionDelaySeconds) {
                                    State = LaserState.Firing;
                                }
                                break;

                            case LaserState.Firing:
                                if (beat == 0 && (state.CurrentTick.Index != CassetteIndex || HalfLength)) {
                                    State = LaserState.Idle;
                                }
                                break;
                        }
                    }
                },
                new PlayerCollider(onPlayerCollide),
                new LaserColliderComponent {CollideWithSolids = CollideWithSolids, Thickness = beamThickness, Offset = beamOffset},
                new LedgeBlocker(_ => KillPlayer),
                beamSprite,
                emitterSprite
            );

            var laserCollider = Get<LaserColliderComponent>();
            Collider = emitterHitbox = new Circle(6);
            emitterHitbox.Position += Orientation.Offset() * 2f;
            colliderList = new ColliderList(laserHitbox = laserCollider.Collider, emitterHitbox);

            Get<StaticMover>().OnMove = v => {
                Position += v;
                laserCollider.UpdateBeam();
            };
        }

        private void onPlayerCollide(Player player) {
            if (KillPlayer) {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction);
            }
        }

        public override void Render() {
            if (beamSprite.Visible) {
                var frame = beamSprite.GetFrame(beamSprite.CurrentAnimationID, beamSprite.CurrentAnimationFrame);
                var offset = Orientation.Offset() * frame.Width;
                float length = Math.Abs(Orientation switch {
                    Orientations.Up => beamSprite.Y - laserHitbox.Top,
                    Orientations.Down => beamSprite.Y - laserHitbox.Bottom,
                    Orientations.Left => beamSprite.X - laserHitbox.Left,
                    Orientations.Right => beamSprite.X - laserHitbox.Right,
                    _ => 0,
                });

                int count = (int) Math.Ceiling(length / frame.Width);
                int remainder = (int) length % frame.Width;

                for (int i = 0; i < count; i++) {
                    var position = Position + beamSprite.Position + (i * offset);
                    int width = i == count - 1 && remainder != 0 ? remainder : frame.Width;
                    frame.Draw(position, beamSprite.Origin, beamSprite.Color, beamSprite.Scale, beamSprite.Rotation , new Rectangle(0, 0, width, frame.Height));
                }
            } else if (State == LaserState.Charging) {
                float animationProgress = (float)emitterSprite.CurrentAnimationFrame / emitterSprite.CurrentAnimationTotalFrames;
                int lerped = (int)Calc.LerpClamp(0, beamThickness, Ease.QuintOut(animationProgress));
                int thickness = Math.Min(lerped + 2, beamThickness);
                thickness -= thickness % 2;

                var rect = Orientation == Orientations.Up || Orientation == Orientations.Down
                    ? new Rectangle((int) (X + laserHitbox.CenterX) - thickness / 2, (int) (Y + laserHitbox.Top), thickness, (int) laserHitbox.Height)
                    : new Rectangle((int) (X + laserHitbox.Left), (int) (Y + laserHitbox.CenterY) - thickness / 2, (int) laserHitbox.Width, thickness);

                Draw.Rect(rect, telegraphColor * 0.3f);
            }

            emitterSprite.Render();
        }

        private void emitCooldownParticles() {
            var level = SceneAs<Level>();
            int length = (int)Orientation.LengthOfHitbox(laserHitbox) - beamOffsetMultiplier;
            var offset = Orientation.Offset();
            float angle = Orientation.Angle() - (float)Math.PI / 2f;
            var startPos = Position + beamOffset * 2;
            var particle = CassetteIndex == 0 ? blueCooldownParticle : pinkCooldownParticle;

            for (int i = 0; i < length; i += Calc.Random.Next(8, 16)) {
                level.ParticlesBG.Emit(particle, 3, startPos + offset * i, Vector2.Zero, angle);
            }
        }

        private IEnumerator impactParticlesSequence() {
            var level = SceneAs<Level>();
            var particle = CassetteIndex == 0 ? blueImpactParticle : pinkImpactParticle;
            var offset = Orientation == Orientations.Up || Orientation == Orientations.Down ? Vector2.UnitX : Vector2.UnitY;
            float angle = Orientation.Angle() + (float)Math.PI / 2f;
            var laserCollider = Get<LaserColliderComponent>();

            while (true) {
                if (State == LaserState.Idle || State == LaserState.Precharge)
                    yield break;

                if (State == LaserState.Charging || laserCollider.CollidedWithScreenBounds) {
                    yield return null;
                    continue;
                }

                int thickness = (int) Orientation.ThicknessOfHitbox(laserHitbox);
                var startPos = new Vector2(Orientation == Orientations.Right ? laserHitbox.Right + X : laserHitbox.Left + X,
                    Orientation == Orientations.Down ? laserHitbox.Bottom + Y: laserHitbox.Top + Y);

                const int particleCount = 3;
                level.ParticlesFG.Emit(particle, particleCount, startPos, Vector2.Zero, angle);
                level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness / 2, Vector2.Zero, angle);
                level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness, Vector2.Zero, angle);

                yield return 0.1f;
            }
        }

        public enum LaserState {
            /// <summary>
            /// The laser is currently off.
            /// Collision = off.
            /// </summary>
            Idle,

            /// <summary>
            /// The laser is preparing to play the charge animation.
            /// It will wait a fraction of a tick before moving to the <see cref="Charging"/> state.
            /// <see cref="BrushLaserEmitter.chargeDelayFraction"/>
            /// Collision = off.
            /// </summary>
            Precharge,

            /// <summary>
            /// The laser is playing the charge animation.
            /// Starts shortly after the tick prior to the laser firing, and lasts for the rest of the tick.
            /// The telegraph beam should be displayed.
            /// Collision = off.
            /// </summary>
            Charging,

            /// <summary>
            /// The laser is playing the burst animation.
            /// Collision = off.
            /// </summary>
            Burst,

            /// <summary>
            /// The laser is firing.
            /// Starts shortly after the cassette swap.
            /// <see cref="BrushLaserEmitter.collisionDelaySeconds"/>
            /// Collision = on.
            /// </summary>
            Firing,
        }
    }
}