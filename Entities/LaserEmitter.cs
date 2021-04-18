using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits a flickering laser beam.
    /// </summary>
    /// <remarks>
    /// Configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>alpha</term><description>
    /// The base alpha value for the beam.
    /// Defaults to 0.4 (40%).
    /// </description></item>
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>color</term><description>
    /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>disableLasers</term><description>
    /// Whether or not colliding with this beam will disable all beams of the same color.
    /// Defaults to false.
    /// </description></item>
    /// <item><term>flicker</term><description>
    /// Whether or not the beam should flicker.
    /// Defaults to true, flickering 4 times per second.
    /// </description></item>
    /// <item><term>killPlayer</term><description>
    /// Whether or not colliding with the beam will kill the player.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>thickness</term><description>
    /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
    /// Defaults to 6 pixels.
    /// </description></item>
    /// <item><term>triggerZipMovers</term><description>
    /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
    /// Defaults to false.
    /// </description></item>
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
        /// The base alpha value for the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to 0.4 (40%).
        /// </remarks>
        public float Alpha { get; }
        
        /// <summary>
        /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool CollideWithSolids { get; }
        
        /// <summary>
        /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public Color Color { get; }
        
        /// <summary>
        /// Whether or not colliding with this beam will disable all beams of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableLasers { get; }
        
        /// <summary>
        /// Whether or not the beam should flicker.
        /// </summary>
        /// <remarks>
        /// Defaults to true, flickering 4 times per second.
        /// </remarks>
        public bool Flicker { get; }
        
        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; }
        
        /// <summary>
        /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
        /// </summary>
        /// <remarks>
        /// Defaults to 6 pixels.
        /// </remarks>
        public float Thickness { get; }
        
        /// <summary>
        /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool TriggerZipMovers { get; }

        #endregion
        
        #region Private Fields
        
        private const float triggerCooldown = 1f;
        
        private float triggerCooldownRemaining;
        private string hexColor;
        
        #endregion
        
        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Color = Calc.HexToColor(hexColor = data.Attr("color", "FF0000").ToLower());
            DisableLasers = data.Bool("disableLasers");
            Flicker = data.Bool("flicker", true);
            KillPlayer = data.Bool("killPlayer", true);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            TriggerZipMovers = data.Bool("triggerZipMovers");
        
            Add(new PlayerCollider(onPlayerCollide));
            
            Add(new LaserBeamComponent {
                Alpha = Alpha,
                CollideWithSolids = CollideWithSolids,
                Color = Color,
                Flicker = Flicker,
                Thickness = Thickness,
            });
            
            Collider = Get<LaserBeamComponent>().Collider;
            
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