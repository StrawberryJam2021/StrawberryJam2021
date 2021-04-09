using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

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
    /// <item><description>"disableLasers" =&gt; <see cref="DisableLasers"/></description></item>
    /// <item><description>"triggerZipMovers" =&gt; <see cref="TriggerZipMovers"/></description></item>
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
        /// <remarks>
        /// Defaults to 6 pixels.
        /// </remarks>
        public float Thickness { get; }
        
        /// <summary>
        /// The base alpha value for the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to 0.4 (40%).
        /// </remarks>
        public float Alpha { get; }

        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; }
        
        /// <summary>
        /// Whether or not colliding with this beam will disable all beams of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableLasers { get; }
        
        /// <summary>
        /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool TriggerZipMovers { get; }

        #endregion
        
        #region Private Fields
        
        private const float flickerFrequency = 4f;
        private const float flickerRange = 4f;
        private const float triggerCooldown = 1f;

        private readonly StaticMover staticMover;
        private readonly Sprite emitterSprite;
        private readonly LaserKillZoneRect killZoneRect;
        private readonly Hitbox killbox;
        
        private Vector2 target;
        private float currentAlpha;
        private float triggerCooldownRemaining;

        private readonly string hexColor;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset) {
            Orientation = orientation;
            
            // read properties from EntityData
            hexColor = data.Attr("color", "FF0000").ToLower();
            Color = Calc.HexToColor(hexColor);
            Flicker = data.Bool("flicker", true);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            currentAlpha = Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            KillPlayer = data.Bool("killPlayer", true);
            DisableLasers = data.Bool("disableLasers");
            TriggerZipMovers = data.Bool("triggerZipMovers");
            
            // same depth as springs
            Depth = Depths.Above - 1;
            
            // create collider killbox
            Collider = killbox = new Hitbox(Thickness, Thickness);

            // add main components
            Add(killZoneRect = new LaserKillZoneRect(),
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter"),
                staticMover = new StaticMover {
                    OnAttach = p => Depth = p.Depth + 1,
                    SolidChecker = s => CollideCheck(s, Position + orientation.Offset()),
                    JumpThruChecker = jt => CollideCheck(jt, Position + orientation.Offset()),
                    OnEnable = () => Collidable = true,
                    OnDisable = () => Collidable = false,
                },
                new PlayerCollider(onPlayerCollide)
            );

            // add a SineWave if flickering is enabled
            if (Flicker)
                Add(new SineWave(flickerFrequency) {OnUpdate = v => currentAlpha = Alpha - (v + 1f) * 0.5f * Alpha / flickerRange});

            emitterSprite.Rotation = orientation.Angle();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            
            updateBeam();
        }
        
        public override void Update() {
            base.Update();

            if (triggerCooldownRemaining > 0)
                triggerCooldownRemaining -= triggerCooldown * Engine.DeltaTime;
            
            updateBeam();
        }

        public void Disable() {
            // TODO: nice animations
            killZoneRect.Visible = false;
            Collidable = false;
        }

        private void onPlayerCollide(Player player) {
            var level = player.SceneAs<Level>();

            if (triggerCooldownRemaining <= 0) {
                triggerCooldownRemaining = triggerCooldown;
                
                if (DisableLasers) {
                    var emitters = level.Entities.OfType<LaserEmitter>().Where(e => e.hexColor == hexColor);
                    foreach (var emitter in emitters)
                        emitter.Disable();
                }

                if (TriggerZipMovers) {
                    string syncFlagCode = $"ZipMoverSync:{hexColor}";
                    level.Session.SetFlag(syncFlagCode);
                }
            }

            if (KillPlayer) {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Position.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Position.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction);
            }
        }

        private void resizeKillbox(float size) {
            switch (Orientation) {
                case Orientations.Up:
                    killbox.Width = Thickness;
                    killbox.Height = size;
                    killbox.BottomCenter = Vector2.Zero;
                    target = killbox.TopCenter;
                    break;
                
                case Orientations.Down:
                    killbox.Width = Thickness;
                    killbox.Height = size;
                    killbox.TopCenter = Vector2.Zero;
                    target = killbox.BottomCenter;
                    break;
                
                case Orientations.Left:
                    killbox.Width = size;
                    killbox.Height = Thickness;
                    killbox.CenterRight = Vector2.Zero;
                    target = killbox.CenterLeft;
                    break;
                
                case Orientations.Right:
                    killbox.Width = size;
                    killbox.Height = Thickness;
                    killbox.CenterLeft = Vector2.Zero;
                    target = killbox.CenterRight;
                    break;
            }
        }
        
        private void updateBeam() {
            var level = SceneAs<Level>();

            float high = Orientation switch {
                Orientations.Up => Position.Y - level.Bounds.Top,
                Orientations.Down => level.Bounds.Bottom - Position.Y,
                Orientations.Left => Position.X - level.Bounds.Left,
                Orientations.Right => level.Bounds.Right - Position.X,
                _ => 0
            };

            int low = 0, safety = 1000;

            // first check if the laser hits the edge of the screen
            resizeKillbox(high);
            if (!CollideCheck<Solid>()) return;
            
            // perform a binary search to hit the nearest solid
            while (safety-- > 0) {
                int pivot = (int) (low + (high - low) / 2f);
                resizeKillbox(pivot);
                if (pivot == low)
                    break;
                if (CollideCheck<Solid>()) {
                    high = pivot;
                } else {
                    low = pivot;
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
    
    public static class OrientationsExtensions {
        public static Vector2 Offset(this LaserEmitter.Orientations orientation) => orientation switch {
            LaserEmitter.Orientations.Up => Vector2.UnitY,
            LaserEmitter.Orientations.Down => -Vector2.UnitY,
            LaserEmitter.Orientations.Left => Vector2.UnitX,
            LaserEmitter.Orientations.Right => -Vector2.UnitX,
            _ => Vector2.Zero
        };

        public static float Angle(this LaserEmitter.Orientations orientation) => orientation switch {
            LaserEmitter.Orientations.Up => 0f,
            LaserEmitter.Orientations.Down => (float) Math.PI,
            LaserEmitter.Orientations.Left => (float) -Math.PI / 2f,
            LaserEmitter.Orientations.Right => (float) Math.PI / 2f,
            _ => 0f
        };
    }
}