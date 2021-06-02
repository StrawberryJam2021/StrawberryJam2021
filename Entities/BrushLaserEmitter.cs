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
        public Color? Color { get; }
        public bool KillPlayer { get; }
        public string Texture { get; }
        public int[] CassetteIndices { get; }
        public int[] Ticks { get; }
        public int LengthInTicks { get; }

        #endregion

        private readonly Sprite emitterSprite;
        private readonly Hitbox laserHitbox;
        private readonly Hitbox emitterHitbox;
        private readonly ColliderList colliderList;
        private LaserState laserState;
        private Color renderColor;
        private float laserFlicker;
        private int ticksRemaining;

        private void setChargingAnimationDelay(float tickLength) {
            var animation = emitterSprite.Animations[$"charging_{Texture}"];
            animation.Delay = tickLength / animation.Frames.Length;
        }

        public LaserState State {
            get => laserState;
            set {
                if (laserState == value)
                    return;

                laserState = value;
                switch (value) {
                    case LaserState.Idle:
                        emitterSprite.Play($"idle_{Texture}");
                        Collider = emitterHitbox;
                        break;

                    case LaserState.Charging:
                        emitterSprite.Play($"charging_{Texture}");
                        Collider = emitterHitbox;
                        break;

                    case LaserState.Firing:
                        emitterSprite.Play($"firing_{Texture}");
                        Collider = colliderList;
                        break;
                }
            }
        }

        public BrushLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            CollideWithSolids = data.Bool("collideWithSolids", true);
            string colorString = data.Attr("color");
            Color = string.IsNullOrEmpty(colorString) ? null : Calc.HexToColor(colorString);
            KillPlayer = data.Bool("killPlayer", true);
            Texture = data.Attr("texture", "a");

            string indices = data.Attr("cassetteIndices", "0");
            CassetteIndices = indices
                .Split(',')
                .Select(s => int.TryParse(s, out int i) ? Calc.Clamp(i, 0, 3) : 0)
                .ToArray();

            string ticks = data.Attr("ticks", "0");
            Ticks = ticks
                .Split(',')
                .Select(s => int.TryParse(s, out int i) ? Math.Max(i, 0) : 0)
                .ToArray();

            LengthInTicks = data.Int("lengthInTicks", 2);

            emitterSprite = StrawberryJam2021Module.SpriteBank.Create("brushLaserEmitter");
            emitterSprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up ? new Vector2(-1, 1) : Vector2.One;
            emitterSprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down ? (float)Math.PI / 2f : 0f;
            emitterSprite.Position = Vector2.Zero;

            Add(new CassetteListener {
                    OnTick = state => {
                        bool validTick(CassetteListener.CassetteTick tick) =>
                            (CassetteIndices.FirstOrDefault() == -1 || CassetteIndices.Contains(tick.Index)) &&
                            (Ticks.FirstOrDefault() == -1 || Ticks.Contains(tick.Offset));

                        if (ticksRemaining > 0) {
                            if (--ticksRemaining == 0)
                                State = LaserState.Idle;
                            return;
                        }

                        if (validTick(state.NextTick)) {
                            setChargingAnimationDelay(state.TickLength);
                            State = LaserState.Charging;
                            renderColor = Color ?? CassetteListener.ColorFromCassetteIndex(state.NextTick.Index);
                        }
                        else if (validTick(state.CurrentTick)) {
                            State = LaserState.Firing;
                            ticksRemaining = LengthInTicks;
                            renderColor = Color ?? CassetteListener.ColorFromCassetteIndex(state.CurrentTick.Index);
                        } else
                            State = LaserState.Idle;
                    }
                },
                new SineWave(8f) {OnUpdate = v => laserFlicker = v},
                new PlayerCollider(onPlayerCollide),
                new LaserColliderComponent {CollideWithSolids = CollideWithSolids, Thickness = 12},
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
            if (State == LaserState.Charging || State == LaserState.Firing) {
                var alpha = State == LaserState.Charging ? 0.2f + laserFlicker * 0.1f : 0.6f;
                var color = renderColor * alpha;
                Draw.Rect(X + laserHitbox.Left, Y + laserHitbox.Top, laserHitbox.Width, laserHitbox.Height, color);
            }

            base.Render();
        }

        public enum LaserState {
            Idle,
            Charging,
            Firing,
        }
    }
}