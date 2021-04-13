using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits pellets that will kill the player on contact.
    /// </summary>
    /// <remarks>
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.<br/>
    /// Configurable values from Ahorn:<br/>
    /// <list type="bullet">
    /// <item><description>"collideWithSolids" =&gt; <see cref="CollideWithSolids"/></description></item>
    /// <item><description>"frequency" =&gt; <see cref="Frequency"/></description></item>
    /// <item><description>"offset" =&gt; <see cref="Offset"/></description></item>
    /// <item><description>"pelletColor" =&gt; <see cref="PelletColor"/></description></item>
    /// <item><description>"pelletCount" =&gt; <see cref="PelletCount"/></description></item>
    /// <item><description>"pelletSpeed" =&gt; <see cref="PelletSpeed"/></description></item>
    /// </list>
    /// </remarks>
    [CustomEntity("SJ2021/PelletEmitterUp = LoadUp",
        "SJ2021/PelletEmitterDown = LoadDown",
        "SJ2021/PelletEmitterLeft = LoadLeft",
        "SJ2021/PelletEmitterRight = LoadRight")]
    public class PelletEmitter : OrientableEntity {
        #region Static Loader Methods
        
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Up);
        
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Down);
        
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Left);
        
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Right);
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Whether or not pellets will be blocked by <see cref="Solid"/>s.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool CollideWithSolids { get; private set; }

        /// <summary>
        /// How often pellets should be fired, in seconds.
        /// </summary>
        /// <remarks>
        /// Defaults to 2 seconds.
        /// Setting a <see cref="Frequency"/> of 0 will disable autofire.
        /// </remarks>
        public virtual float Frequency { get; private set; }
        
        /// <summary>
        /// The number of seconds to delay firing.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 (no offset).
        /// Ignored if <see cref="Frequency"/> is 0.
        /// </remarks>
        public float Offset { get; private set; }
        
        /// <summary>
        /// The <see cref="Microsoft.Xna.Framework.Color"/> used to render the pellets.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public virtual Color PelletColor { get; private set; }
        
        /// <summary>
        /// The number of pellets that should be fired at once.
        /// </summary>
        /// <remarks>
        /// Defaults to 1.
        /// </remarks>
        public int PelletCount { get; private set; }
        
        /// <summary>
        /// The number of units per second that the pellet should travel.
        /// </summary>
        /// <remarks>
        /// Defaults to 100.
        /// </remarks>
        public float PelletSpeed { get; private set; }

        #endregion
        
        #region Private Fields

        private const float shotOriginOffset = 8f;
        private float timer = 2f;
        
        #endregion
        
        protected PelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
        }
        
        protected override void ReadEntityData(EntityData data) {
            PelletSpeed = data.Float("pelletSpeed", 100f);
            PelletColor = data.HexColor("pelletColor", Color.Red);
            PelletCount = data.Int("pelletCount", 1);
            Frequency = data.Float("frequency", 2f);
            Offset = data.Float("offset");
            CollideWithSolids = data.Bool("collideWithSolids", true);
        }

        protected override void AddComponents() {
            base.AddComponents();
            
            Add(new PelletFiringComponent<PelletShot> {
                GetShotDirection = () => Orientation.Direction(),
                GetShotOrigin = () => Orientation.Direction() * shotOriginOffset,
                CollideWithSolids = CollideWithSolids,
                PelletColor = PelletColor,
                PelletCount = PelletCount,
                PelletSpeed = PelletSpeed
            });
            
            Sprite emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
            emitterSprite.Rotation = Orientation.Angle();
            
            Add(emitterSprite);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            timer = Offset;
        }

        public override void Update() {
            base.Update();

            if (Frequency > 0) {
                timer -= Engine.DeltaTime;
                if (timer <= 0) {
                    Get<PelletFiringComponent>().Fire();
                    timer += Frequency;
                }
            }
        }
        
        [Pooled]
        [Tracked]
        public class PelletShot : Entity {
            #region Private Fields

            private Sprite sprite;
            private PelletFiringComponent.PelletComponent pelletComponent;

            #endregion
            
            public PelletShot()
                : base(Vector2.Zero) {
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Depth = Depths.Top;

                Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
                Add(new PlayerCollider(onPlayerCollide));
                Add(pelletComponent = new PelletFiringComponent.PelletComponent());
            }

            public override void Render() {
                var position = sprite.Position;
                
                // render black outline
                sprite.Color = Color.Black;
                sprite.Position = position + Vector2.UnitX;
                sprite.Render();
                sprite.Position = position - Vector2.UnitX;
                sprite.Render();
                sprite.Position = position + Vector2.UnitY;
                sprite.Render();
                sprite.Position = position - Vector2.UnitY;
                sprite.Render();
                sprite.Color = pelletComponent.Color;
                sprite.Position = position;
                
                base.Render();
            }

            private void onPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}