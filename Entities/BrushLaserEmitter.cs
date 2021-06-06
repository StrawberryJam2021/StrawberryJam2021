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
        private Color renderColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);
        private Vector2 beamOffset => Orientation.Offset() * beamOffsetMultiplier;

        private readonly Sprite emitterSprite;
        private readonly Sprite beamSprite;
        private readonly Collider emitterHitbox;
        private readonly Hitbox laserHitbox;
        private readonly ColliderList colliderList;
        private LaserState laserState;
        private float laserFlicker;

        private const float chargeDelayFraction = 0.25f;
        private const float collisionDelaySeconds = 0.1f;
        private const int beamOffsetMultiplier = 4;

        private static readonly ParticleType beamParticle = new ParticleType(ParticleTypes.Dust) {
            SpeedMin = 15f,
            SpeedMax = 30f,
            LifeMin = 0.2f,
            LifeMax = 0.4f,
        };

        private static ParticleType collideParticle = ZipMover.P_Sparks;
        private static ParticleType chargeParticle = ZipMover.P_Sparks;

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

                laserState = value;
                switch (value) {
                    case LaserState.Idle:
                    case LaserState.Precharge:
                        emitterSprite.Play(idleAnimation);
                        beamSprite.Visible = false;
                        Collider = emitterHitbox;
                        break;

                    case LaserState.Charging:
                        emitterSprite.Play(chargingAnimation);
                        beamSprite.Visible = false;
                        Collider = emitterHitbox;
                        Add(new Coroutine(effectSequence()));
                        break;

                    case LaserState.Burst:
                        emitterSprite.Play(burstAnimation);
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
                new SineWave(8f) {OnUpdate = v => laserFlicker = v},
                new PlayerCollider(onPlayerCollide),
                new LaserColliderComponent {CollideWithSolids = CollideWithSolids, Thickness = 12, Offset = beamOffset},
                beamSprite,
                emitterSprite
            );

            Collider = emitterHitbox = new Circle(6);
            emitterHitbox.Position += Orientation.Offset() * 2f;
            colliderList = new ColliderList(laserHitbox = Get<LaserColliderComponent>().Collider, emitterHitbox);
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
            }

            emitterSprite.Render();
        }

        private IEnumerator effectSequence() {
            while (true) {
                var level = SceneAs<Level>();
                var color = CassetteListener.ColorFromCassetteIndex(CassetteIndex);

                switch (State) {
                    default:
                    case LaserState.Idle:
                    case LaserState.Precharge:
                        yield break;

                    case LaserState.Charging:
                        break;

                    case LaserState.Burst:
                        break;

                    case LaserState.Firing:
                        int length = (int)Orientation.LengthOfHitbox(laserHitbox) - beamOffsetMultiplier;
                        var offset = Orientation.Offset();
                        float angle = Orientation.Angle();
                        var startPos = Position + beamOffset * 2;

                        for (int i = 0; i < length; i += 3) {
                            level.ParticlesBG.Emit(beamParticle, startPos + offset * i, color, angle);
                            level.ParticlesBG.Emit(beamParticle, startPos + offset * i, color, angle + (float)Math.PI);
                        }

                        yield return 0.1f;
                        break;
                }

                yield return null;
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