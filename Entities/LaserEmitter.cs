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
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.<br/>
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
    public class LaserEmitter : OrientableEntity {
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
        /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public Color Color { get; private set; }
        
        /// <summary>
        /// Whether or not the beam should flicker.
        /// </summary>
        /// <remarks>
        /// Defaults to true, flickering 4 times per second.
        /// </remarks>
        public bool Flicker { get; private set; }
        
        /// <summary>
        /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
        /// </summary>
        /// <remarks>
        /// Defaults to 6 pixels.
        /// </remarks>
        public float Thickness { get; private set; }
        
        /// <summary>
        /// The base alpha value for the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to 0.4 (40%).
        /// </remarks>
        public float Alpha { get; private set; }

        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; private set; }
        
        /// <summary>
        /// Whether or not colliding with this beam will disable all beams of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableLasers { get; private set; }
        
        /// <summary>
        /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool TriggerZipMovers { get; private set; }
        
        /// <summary>
        /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool CollideWithSolids { get; private set; }

        #endregion
        
        #region Private Fields
        
        private const float triggerCooldown = 1f;
        
        private float triggerCooldownRemaining;
        private string hexColor;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            Collider = Get<LaserBeamComponent>().Collider;
        }

        protected override void ReadEntityData(EntityData data) {
            hexColor = data.Attr("color", "FF0000").ToLower();
            Color = Calc.HexToColor(hexColor);
            Flicker = data.Bool("flicker", true);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            KillPlayer = data.Bool("killPlayer", true);
            DisableLasers = data.Bool("disableLasers");
            TriggerZipMovers = data.Bool("triggerZipMovers");
            CollideWithSolids = data.Bool("collideWithSolids", true);
        }
        
        protected override void AddComponents() {
            base.AddComponents();
            
            Add(new PlayerCollider(onPlayerCollide));
            
            // laser beam should appear underneath everything else
            Add(new LaserBeamComponent {
                Color = Color,
                Thickness = Thickness,
                Alpha = Alpha,
                Flicker = Flicker,
                CollideWithSolids = CollideWithSolids
            });
            
            Sprite emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
            emitterSprite.Rotation = Orientation.Angle();
            
            Add(emitterSprite);
        }

        public override void Update() {
            base.Update();

            if (triggerCooldownRemaining > 0)
                triggerCooldownRemaining -= triggerCooldown * Engine.DeltaTime;
        }

        private void onPlayerCollide(Player player) {
            var level = player.SceneAs<Level>();

            if (triggerCooldownRemaining <= 0) {
                triggerCooldownRemaining = triggerCooldown;
                
                if (DisableLasers) {
                    level.Entities.With<LaserEmitter>(emitter => {
                        if (emitter.Color == Color)
                            emitter.Collidable = false;
                    });
                }

                if (TriggerZipMovers) {
                    string syncFlagCode = $"ZipMoverSync:{hexColor}";
                    level.Session.SetFlag(syncFlagCode);
                }
            }

            if (KillPlayer) {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction);
            }
        }
    }
}