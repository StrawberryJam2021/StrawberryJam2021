using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that automatically emits pellets that will kill the player on contact.
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.
    /// </summary>
    /// <remarks>
    /// Emitter configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>frequency</term><description>
    /// How often pellets should be fired, in seconds. Defaults to 2 seconds.
    /// </description></item>
    /// <item><term>offset</term><description>
    /// The number of seconds to delay firing.
    /// Defaults to 0 (fires immediately).
    /// </description></item>
    /// <item><term>count</term><description>
    /// The number of pellets that should be fired at once.
    /// Defaults to 1.
    /// </description></item>
    /// </list>
    /// Pellet configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not pellets will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>pelletColor</term><description>
    /// The <see cref="Microsoft.Xna.Framework.Color"/> used to render the pellets.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>pelletSpeed</term><description>
    /// The number of units per second that the pellet should travel.
    /// Defaults to 100.
    /// </description></item>
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
        
        public float Frequency { get; set; }
        public float Offset { get; set; }
        public int Count { get; set; }
        public bool CollideWithSolids { get; set; }
        public Color Color { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 Origin { get; set; }
        public float Speed { get; set; }
        
        private float timer = 2f;
        
        protected PelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            const float shotOriginOffset = 8f;
            Frequency = data.Float("frequency", 2f);
            Offset = data.Float("offset");
            Count = data.Int("count", 1);
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Color = data.HexColor("pelletColor", Color.Red);
            Speed = data.Float("pelletSpeed", 100f);
            Direction = Orientation.Direction();
            Origin = Orientation.Direction() * shotOriginOffset;

            Sprite emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
            emitterSprite.Rotation = Orientation.Angle();

            Add(emitterSprite, new LedgeBlocker());
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
                    Fire();
                    timer += Frequency;
                }
            }
        }

        public void Fire(Action<PelletShot> action = null) {
            // TODO: delay extra shots
            for (int i = 0; i < Count; i++) {
                var shot = Engine.Pooler.Create<PelletShot>().Init(this);
                action?.Invoke(shot);
                Scene.Add(shot);
            }
        }
        
        [Pooled]
        [Tracked]
        public class PelletShot : Entity {
            public bool Dead { get; set; }
            public Vector2 Speed { get; set; }
            public Color Color { get; set; }
            public bool CollideWithSolids { get; set; }
            
            private readonly Sprite sprite;

            public PelletShot()
                : base(Vector2.Zero) {
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Depth = Depths.Top;

                Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
                Add(new PlayerCollider(onPlayerCollide));
            }

            public PelletShot Init(PelletEmitter emitter) {
                Dead = false;
                Speed = emitter.Direction * emitter.Speed;
                Color = emitter.Color;
                Position = emitter.Position + emitter.Origin;
                CollideWithSolids = emitter.CollideWithSolids;
                return this;
            }
            
            public void Destroy() {
                Dead = true;
                RemoveSelf();
            }

            public override void Update() {
                base.Update();
            
                // fast fail if the pooled shot is no longer alive
                if (Dead) return;

                var level = SceneAs<Level>();
                
                Position += Speed * Engine.DeltaTime;

                if (!level.IsInBounds(this) || CollideWithSolids && CollideCheck<Solid>(Position))
                    Destroy();
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
                sprite.Color = Color;
                sprite.Position = position;
                
                base.Render();
            }

            private void onPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}