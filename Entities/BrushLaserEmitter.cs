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
        // public Color? Color { get; }
        public bool KillPlayer { get; }
        public int CassetteIndex { get; }
        public int LengthInTicks { get; }

        #endregion

        private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
        private string chargingAnimation => $"{animationPrefix}_charging";
        private string burstAnimation => $"{animationPrefix}_burst";
        private string firingAnimation => $"{animationPrefix}_firing";
        private string idleAnimation => $"{animationPrefix}_idle";

        private readonly Sprite emitterSprite;
        private readonly Sprite beamSprite;
        private readonly Hitbox laserHitbox;
        private readonly Hitbox emitterHitbox;
        private readonly ColliderList colliderList;
        private LaserState laserState;
        private Color renderColor;
        private float laserFlicker;
        private int ticksRemaining;

        private void setChargingAnimationSpeed(float totalRunTime) {
            var animation = emitterSprite.Animations[chargingAnimation];
            animation.Delay = totalRunTime / animation.Frames.Length;
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

                    // case LaserState.Firing:
                    //     // emitterSprite.Play(firingAnimation);
                    //     Collider = colliderList;
                    //     break;
                }
            }
        }

        public BrushLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            CollideWithSolids = data.Bool("collideWithSolids", true);
            // string colorString = data.Attr("color");
            // Color = string.IsNullOrEmpty(colorString) ? null : Calc.HexToColor(colorString);
            KillPlayer = data.Bool("killPlayer", true);
            // Texture = data.Attr("texture", "a");

            // string indices = data.Attr("cassetteIndices", "0");
            // CassetteIndices = indices
            //     .Split(',')
            //     .Select(s => int.TryParse(s, out int i) ? Calc.Clamp(i, 0, 3) : 0)
            //     .ToArray();
            //
            // string ticks = data.Attr("ticks", "0");
            // Ticks = ticks
            //     .Split(',')
            //     .Select(s => int.TryParse(s, out int i) ? Math.Max(i, 0) : 0)
            //     .ToArray();

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
                    OnTick = state => {
                        // bool validTick(CassetteListener.CassetteTick tick) =>
                        //     (CassetteIndices.FirstOrDefault() == -1 || CassetteIndices.Contains(tick.Index)) &&
                        //     (Ticks.FirstOrDefault() == -1 || Ticks.Contains(tick.Offset));

                        if (ticksRemaining > 0) {
                            if (--ticksRemaining == 0)
                                State = LaserState.Idle;
                            return;
                        }

                        if (State == LaserState.Idle && state.CurrentTick.Index != CassetteIndex &&
                            state.NextTick.Index == CassetteIndex) {
                            // setChargingAnimationDelay(state.TickLength);
                            // State = LaserState.Charging;
                            // renderColor = CassetteListener.ColorFromCassetteIndex(state.NextTick.Index);
                            State = LaserState.Precharge;
                        } else if (State == LaserState.Charging && state.CurrentTick.Index == CassetteIndex) {
                            State = LaserState.Burst;
                            ticksRemaining = LengthInTicks;
                            // renderColor = CassetteListener.ColorFromCassetteIndex(state.CurrentTick.Index);
                        } else
                            State = LaserState.Idle;
                    },
                    OnBeat = state => {
                        if (State == LaserState.Precharge) {
                            const float chargeDelay = 0.35f;
                            float beat = state.Beat % state.BeatsPerTick;
                            if (beat / state.BeatsPerTick >= chargeDelay) {
                                setChargingAnimationSpeed(state.TickLength * (1 - chargeDelay));
                                State = LaserState.Charging;
                            }
                        } else if (State == LaserState.Burst) {
                            const float enableDelay = 0.1f;
                            float beat = state.Beat % state.BeatsPerTick;
                            if (beat / state.BeatsPerTick >= enableDelay) {
                                State = LaserState.Firing;
                            }
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
            laserHitbox = Get<LaserColliderComponent>().Collider;
            colliderList = new ColliderList(laserHitbox, emitterHitbox);
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
            const float turnOffFraction = 0.8f;

            // if (State != LaserState.Idle) {
            //     float alpha = State == LaserState.Charging ? 0.2f + laserFlicker * 0.1f : 0.6f;
            //     var color = renderColor;//State == LaserState.Burst ? Color.White : renderColor * alpha;
            //
            //     if (State != LaserState.Charging || (float)emitterSprite.CurrentAnimationFrame / emitterSprite.CurrentAnimationTotalFrames < turnOffFraction)
            //         Draw.Rect(X + laserHitbox.Left, Y + laserHitbox.Top, laserHitbox.Width, laserHitbox.Height, color);
            // }

            base.Render();
        }

        public override void Update() {
            base.Update();

            // if (State == LaserState.Firing && emitterSprite.CurrentAnimationID == firingAnimation)
            //     Collider = colliderList;
            // if (State == LaserState.Burst && !emitterSprite.Animating)
            //     State = LaserState.Firing;
        }

        public enum LaserState {
            /// <summary>
            /// The laser is currently off.
            /// Collision = off.
            /// </summary>
            Idle,

            Precharge,

            /// <summary>
            /// The laser is playing the charge animation.
            /// Starts on the tick before the laser fires, and lasts for the entire tick.
            /// The telegraph beam should be displayed.
            /// Collision = off.
            /// </summary>
            Charging,

            /// <summary>
            /// The laser is playing the burst animation.
            /// Starts on the tick the laser fires.
            /// Collision = off.
            /// </summary>
            Burst,

            /// <summary>
            /// The laser is looping the firing animation.
            /// Starts after the burst animation completes.
            /// Collision = on.
            /// </summary>
            Firing,
        }
    }
}