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

        #region Properties

        public Orientations Orientation { get; }
        public Color Color { get; }
        public float FlickerFrequency { get; }
        public float FlickerIntensity { get; }
        public float Thickness { get; }
        public float Alpha { get; }

        #endregion
        
        #region Private Fields

        private readonly StaticMover staticMover;
        private readonly Sprite emitterSprite;
        private readonly LaserKillZoneRect killZoneRect;
        private readonly Hitbox killbox;
        
        private Vector2 target;
        private float alphaMultiplier = 1f;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset) {
            Orientation = orientation;
            
            Color = data.HexColor("color", Color.Red);
            FlickerFrequency = Math.Max(data.Float("flickerFrequency", 4f), 0f);
            FlickerIntensity = Math.Max(data.Float("flickerIntensity", 8f), 1f);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            
            Depth = -8501;
            Collider = killbox = new Hitbox(Thickness, Thickness);

            Add(killZoneRect = new LaserKillZoneRect(),
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter"),
                staticMover = new StaticMover {
                    OnAttach = p => Depth = p.Depth + 1,
                    SolidChecker = s => CollideCheck(s, Position + directionForOrientation(orientation)),
                    JumpThruChecker = jt => CollideCheck(jt, Position + directionForOrientation(orientation)),
                    OnEnable = () => Collidable = true,
                    OnDisable = () => Collidable = false,
                },
                new PlayerCollider(player => player.Die(Vector2.Zero))
            );

            if (FlickerFrequency > 0 && FlickerIntensity >= 1)
                Add(new SineWave(FlickerFrequency) {OnUpdate = v => alphaMultiplier = 1f + v / FlickerIntensity});

            emitterSprite.Rotation = rotationForOrientation(orientation);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            
            updateBeam();
        }
        
        public override void Update() {
            base.Update();
            
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
                switch (Orientation) {
                    case Orientations.Up:
                        killbox.Width = Thickness;
                        killbox.Height++;
                        killbox.BottomCenter = Vector2.Zero;
                        target = killbox.TopCenter;
                        break;
                
                    case Orientations.Down:
                        killbox.Width = Thickness;
                        killbox.Height++;
                        killbox.TopCenter = Vector2.Zero;
                        target = killbox.BottomCenter;
                        break;
                
                    case Orientations.Left:
                        killbox.Width++;
                        killbox.Height = Thickness;
                        killbox.CenterRight = Vector2.Zero;
                        target = killbox.CenterLeft;
                        break;
                
                    case Orientations.Right:
                        killbox.Width++;
                        killbox.Height = Thickness;
                        killbox.CenterLeft = Vector2.Zero;
                        target = killbox.CenterRight;
                        break;
                }
            }
        }

        public enum Orientations {
            Up,
            Down,
            Left,
            Right,
        }

        public class LaserKillZoneRect : Component {
            public LaserKillZoneRect() : base(true, true) {
            }

            public override void Render() {
                if (!Entity.Collidable || !(Entity is LaserEmitter {Collider: Hitbox hitbox} laserEmitter))
                    return;

                var color = laserEmitter.Color * (laserEmitter.Alpha * laserEmitter.alphaMultiplier);
                
                Draw.Rect(hitbox.Bounds, color);

                float thickness = laserEmitter.Orientation == Orientations.Left || laserEmitter.Orientation == Orientations.Right
                    ? hitbox.Height / 3f
                    : hitbox.Width / 3f;

                Draw.Line(Entity.X, Entity.Y, Entity.X + laserEmitter.target.X, Entity.Y + laserEmitter.target.Y, color, thickness);
            }
        }
    }
}