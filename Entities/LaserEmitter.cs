using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits a flickering laser beam.
    /// </summary>
    /// <remarks>
    /// Will kill the player by default, but its functionality can be changed.<br/>
    /// Has four available orientations, indicated by the <see cref="Orientations"/> enum.<br/>
    /// Configurable values from Ahorn:<br/>
    /// <list type="bullet">
    /// <item><description>"color" =&gt; <see cref="Color"/></description></item>
    /// <item><description>"alpha" =&gt; <see cref="Alpha"/></description></item>
    /// <item><description>"flicker" =&gt; <see cref="Flicker"/></description></item>
    /// <item><description>"thickness" =&gt; <see cref="Thickness"/></description></item>
    /// <item><description>"killPlayer" =&gt; <see cref="KillPlayer"/></description></item>
    /// </list>
    /// </remarks>
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

        /// <summary>
        /// The orientation of the emitter, where the direction indicates which way the beam travels.
        /// </summary>
        public Orientations Orientation { get; }
        
        /// <summary>
        /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public Color Color { get; }
        
        /// <summary>
        /// Whether or not the beam should flicker.
        /// </summary>
        /// <remarks>
        /// Defaults to true, flickering 4 times per second.
        /// </remarks>
        public bool Flicker { get; }
        
        /// <summary>
        /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
        /// </summary>
        public float Thickness { get; }
        
        /// <summary>
        /// The base alpha value for the beam.
        /// </summary>
        public float Alpha { get; }

        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; }
        
        #endregion
        
        #region Private Fields
        
        private const float flickerFrequency = 4f;
        private const float flickerRange = 4f;

        private readonly StaticMover staticMover;
        private readonly Sprite emitterSprite;
        private readonly LaserKillZoneRect killZoneRect;
        private readonly Hitbox killbox;
        
        private Vector2 target;
        private float currentAlpha;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset) {
            Orientation = orientation;
            
            // read properties from EntityData
            Color = data.HexColor("color", Color.Red);
            Flicker = data.Bool("flicker", true);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            currentAlpha = Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            KillPlayer = data.Bool("killPlayer", true);
            
            // same depth as springs
            Depth = Depths.Above - 1;
            
            // create collider killbox
            Collider = killbox = new Hitbox(Thickness, Thickness);

            // add main components
            Add(killZoneRect = new LaserKillZoneRect(),
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter"),
                staticMover = new StaticMover {
                    OnAttach = p => Depth = p.Depth + 1,
                    SolidChecker = s => CollideCheck(s, Position + directionForOrientation(orientation)),
                    JumpThruChecker = jt => CollideCheck(jt, Position + directionForOrientation(orientation)),
                    OnEnable = () => Collidable = true,
                    OnDisable = () => Collidable = false,
                },
                new PlayerCollider(onPlayerCollide)
            );

            // add a SineWave if flickering is enabled
            if (Flicker)
                Add(new SineWave(flickerFrequency) {OnUpdate = v => currentAlpha = Alpha - (v + 1f) * 0.5f * Alpha / flickerRange});

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

        private void onPlayerCollide(Player player) {
            if (!KillPlayer) return;

            Vector2 direction;
            if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                direction = player.Position.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
            else
                direction = player.Position.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;
            
            player.Die(direction);
        }
        
        private void updateBeam() {
            var level = SceneAs<Level>();

            bool killboxInBounds() =>
                killbox.AbsoluteLeft >= level.Bounds.Left &&
                killbox.AbsoluteRight <= level.Bounds.Right &&
                killbox.AbsoluteTop >= level.Bounds.Top &&
                killbox.AbsoluteBottom <= level.Bounds.Bottom;

            // default killbox to empty centred on the emitter
            killbox.Width = 0;
            killbox.Height = 0;
            killbox.Center = Vector2.Zero;

            // increase size of the killbox until we collide with a Solid
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

        /// <summary>
        /// The available orientations of an emitter, where the direction indicates which way the beam travels.
        /// </summary>
        public enum Orientations {
            /// <summary>
            /// Indicates that the beam fires from the emitter toward the top of the screen.
            /// </summary>
            Up,
            
            /// <summary>
            /// Indicates that the beam fires from the emitter toward the bottom of the screen.
            /// </summary>
            Down,
            
            /// <summary>
            /// Indicates that the beam fires from the emitter toward the left of the screen.
            /// </summary>
            Left,
            
            /// <summary>
            /// Indicates that the beam fires from the emitter toward the right of the screen.
            /// </summary>
            Right,
        }

        /// <summary>
        /// Renders the beam part of the laser emitter.
        /// </summary>
        /// <remarks>
        /// Opacity of the beam edges is calculated as <see cref="LaserEmitter.Alpha"/> times <see cref="LaserEmitter.alphaMultiplier"/>.
        /// The centre third of the beam is twice that opacity.
        /// </remarks>
        public class LaserKillZoneRect : Component {
            public LaserKillZoneRect() : base(true, true) {
            }

            public override void Render() {
                if (!Entity.Collidable || !(Entity is LaserEmitter {Collider: Hitbox hitbox} laserEmitter))
                    return;

                var color = laserEmitter.Color * laserEmitter.currentAlpha;
                
                Draw.Rect(hitbox.Bounds, color);

                float thickness = laserEmitter.Orientation == Orientations.Left || laserEmitter.Orientation == Orientations.Right
                    ? hitbox.Height / 3f
                    : hitbox.Width / 3f;

                Draw.Line(Entity.X, Entity.Y, Entity.X + laserEmitter.target.X, Entity.Y + laserEmitter.target.Y, color, thickness);
            }
        }
    }
}