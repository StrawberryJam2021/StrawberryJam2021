using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

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
        public int LengthInTicks { get; }

        #endregion

        private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
        private string chargingAnimation => $"{animationPrefix}_charging";
        private string burstAnimation => $"{animationPrefix}_burst";
        private string idleAnimation => $"{animationPrefix}_idle";
        private Color renderColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);

        private readonly Sprite emitterSprite;
        private readonly Sprite beamSprite;
        private readonly Hitbox emitterHitbox;
        private readonly ColliderList colliderList;
        private LaserState laserState;
        private float laserFlicker;

        private const float chargeDelayFraction = 0.25f;
        private const float collisionDelaySeconds = 0.1f;

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
            LengthInTicks = data.Int("lengthInTicks", 2);

            var beamOffset = Orientation.Offset() * 4;

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
                                if (beat == 0 && state.CurrentTick.Index != CassetteIndex) {
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

            Collider = emitterHitbox = new Hitbox(12, 12).AlignedWithOrientation(Orientation);
            colliderList = new ColliderList(Get<LaserColliderComponent>().Collider, emitterHitbox);
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