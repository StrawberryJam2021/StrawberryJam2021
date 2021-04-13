using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Performs the firing action for a pellet emitter, including pooling of shots.
    /// </summary>
    public abstract class PelletFiringComponent : Component {
        #region Properties

        public float PelletSpeed { get; set; }
        
        public Color PelletColor { get; set; }
        
        public int PelletCount { get; set; }
        
        public bool CollideWithSolids { get; set; }

        #endregion
        
        public Func<Vector2> GetShotOrigin;
        public Func<Vector2> GetShotDirection;

        private Level level;
        
        public PelletFiringComponent()
            : base(true, false)
        {
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
            level = scene as Level;
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            level = null;
        }

        public void Fire() {
            var direction = GetShotDirection?.Invoke() ?? Vector2.Zero;
            var origin = GetShotOrigin?.Invoke() ?? Vector2.Zero;

            var shot = CreateShot();
            var comp = shot.Get<PelletComponent>();
            comp.Dead = false;
            comp.Speed = direction * PelletSpeed;
            comp.Color = PelletColor;
            comp.CollideWithSolids = CollideWithSolids;
            shot.Position = Entity.Position + origin;
            
            level?.Add(shot);
        }

        protected abstract Entity CreateShot();
        
        public class PelletComponent : Component {
            public Vector2 Speed { get; set; }
            public Color Color { get; set; }
            public bool CollideWithSolids { get; set; }
            public bool Dead { get; set; }

            public PelletComponent() : base(true, false)
            {
            }

            public override void Update() {
                base.Update();
            
                // fast fail if the pooled shot is no longer alive
                if (Dead) return;

                var level = SceneAs<Level>();
                
                Entity.Position += Speed * Engine.DeltaTime;

                if (!level.IsInBounds(Entity) || CollideWithSolids && Entity.CollideCheck<Solid>(Entity.Position))
                    Destroy();
            }

            public void Destroy() {
                Dead = true;
                Entity.RemoveSelf();
            }
        }
    }
    
    /// <summary>
    /// Generic version of <see cref="PelletFiringComponent"/> that allows mod developers to specify the pellet entity.
    /// </summary>
    /// <remarks>
    /// The pellet entity should have the <see cref="Pooled"/> and <see cref="Tracked"/> attributes, as well as
    /// an accompanying <see cref="PelletFiringComponent.PelletComponent"/>.
    /// </remarks>
    /// <typeparam name="TShot"></typeparam>
    public class PelletFiringComponent<TShot> : PelletFiringComponent where TShot : Entity, new() {
        protected override Entity CreateShot() => Engine.Pooler.Create<TShot>();
    }
}