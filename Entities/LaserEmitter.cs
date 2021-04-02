using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/LaserEmitterUp = LoadUp",
        "SJ2021/LaserEmitterDown = LoadDown",
        "SJ2021/LaserEmitterLeft = LoadLeft",
        "SJ2021/LaserEmitterRight = LoadRight")]
    public class LaserEmitter : Entity {
        #region Static Loader Methods
        
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Up);
        
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Down);
        
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Left);
        
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Right);
        
        #endregion
        
        #region Static Helper Methods

        private static Vector2 directionForOrientation(Orientations orientation) => orientation switch {
            Orientations.Up => Vector2.UnitY,
            Orientations.Down => -Vector2.UnitY,
            Orientations.Left => Vector2.UnitX,
            Orientations.Right => -Vector2.UnitX,
            _ => Vector2.Zero
        };

        private static float rotationForOrientation(Orientations orientation) => orientation switch {
            Orientations.Up => 0f,
            Orientations.Down => (float) Math.PI,
            Orientations.Left => (float) -Math.PI / 2f,
            Orientations.Right => (float) Math.PI / 2f,
            _ => 0f
        };

        #endregion
        
        #region Private Fields

        private const float DEFAULT_SIZE = 4f;

        private readonly Orientations orientation;
        private readonly StaticMover staticMover;
        private readonly Sprite emitterSprite;
        private readonly LaserKillZoneRect killZoneRect;
        private readonly Hitbox killbox;
        
        private Vector2 target;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset) {
            this.orientation = orientation;
            
            Depth = -8501;
            Collider = killbox = new Hitbox(DEFAULT_SIZE, DEFAULT_SIZE);

            void SpecifyName(float v) => killZoneRect.Multiplier = 1f + Math.Abs(v) / 2f;

            Add(killZoneRect = new LaserKillZoneRect(Color.Red),
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter"),
                staticMover = new StaticMover {
                    OnAttach = p => Depth = p.Depth + 1,
                    SolidChecker = s => CollideCheck(s, Position + directionForOrientation(orientation)),
                    JumpThruChecker = jt => CollideCheck(jt, Position + directionForOrientation(orientation)),
                    OnEnable = onEnable,
                    OnDisable = onDisable,
                },
                new PlayerCollider(onCollide),
                new SineWave(1f) {OnUpdate = SpecifyName}
            );

            emitterSprite.Rotation = rotationForOrientation(orientation);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            
            updateBeam();
        }

        private void updateBeam() {
            var level = SceneAs<Level>();

            bool killboxInBounds() =>
                killbox.AbsoluteLeft >= level.Bounds.Left &&
                killbox.AbsoluteRight <= level.Bounds.Right &&
                killbox.AbsoluteTop >= level.Bounds.Top &&
                killbox.AbsoluteBottom <= level.Bounds.Bottom;

            killbox.Width = 0;
            killbox.Height = 0;
            killbox.Center = Vector2.Zero;

            while (!CollideCheck<Solid>() && killboxInBounds()) {
                switch (orientation) {
                    case Orientations.Up:
                        killbox.Width = DEFAULT_SIZE;
                        killbox.Height++;
                        killbox.BottomCenter = Vector2.Zero;
                        target = killbox.TopCenter;
                        break;
                
                    case Orientations.Down:
                        killbox.Width = DEFAULT_SIZE;
                        killbox.Height++;
                        killbox.TopCenter = Vector2.Zero;
                        target = killbox.BottomCenter;
                        break;
                
                    case Orientations.Left:
                        killbox.Width++;
                        killbox.Height = DEFAULT_SIZE;
                        killbox.CenterRight = Vector2.Zero;
                        target = killbox.CenterLeft;
                        break;
                
                    case Orientations.Right:
                        killbox.Width++;
                        killbox.Height = DEFAULT_SIZE;
                        killbox.CenterLeft = Vector2.Zero;
                        target = killbox.CenterRight;
                        break;
                }
            }
        }

        private void onEnable() {
            Visible = Collidable = true;
        }

        private void onDisable() {
            Collidable = false;
        }
        
        private void onCollide(Player player) {
            if (SaveData.Instance.Assists.Invincible)
                return;
            
            player.Die(Vector2.Zero);
        }
        
        public override void Update() {
            base.Update();
            
            updateBeam();
        }

        public enum Orientations {
            Up,
            Down,
            Left,
            Right,
        }

        public class LaserKillZoneRect : Component {
            public Color Color;
            public float Alpha = 0.3f;
            public float Multiplier = 1f;
            
            public LaserKillZoneRect(Color color) : base(true, true) {
                Color = color;
            }

            public override void Render() {
                if (!(Entity is LaserEmitter laserEmitter && laserEmitter.Collider is Hitbox hitbox))
                    return;

                Draw.Rect(hitbox.Bounds, Color * (Alpha * Multiplier));

                float thickness = laserEmitter.orientation == Orientations.Left || laserEmitter.orientation == Orientations.Right
                    ? hitbox.Height / 3f
                    : hitbox.Width / 3f;

                Draw.Line(Entity.X, Entity.Y, Entity.X + laserEmitter.target.X, Entity.Y + laserEmitter.target.Y, Color * (Alpha * Multiplier), thickness);
            }
        }
    }
}