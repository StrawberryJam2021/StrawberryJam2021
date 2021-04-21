using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Performs the firing action for a pellet emitter, including pooling of shots.
    /// </summary>
    public abstract class PelletFiringComponent : Component {
        public int Count { get; set; }
        public PelletComponent.PelletComponentSettings Settings { get; set; }

        private Level level;
        
        protected PelletFiringComponent()
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

        public void Fire(Action<PelletComponent> action = null) {
            // TODO: delay extra shots
            for (int i = 0; i < Count; i++) {
                var shot = CreateShot();
                var comp = shot.Get<PelletComponent>();
                comp.Dead = false;
                comp.Settings = Settings;
                action?.Invoke(comp);
                shot.Position = Entity.Position + comp.Settings.Origin;
                level?.Add(shot);
            }
        }

        protected abstract Entity CreateShot();
        
        public class PelletComponent : Component {
            public PelletComponentSettings Settings;
            public bool Dead { get; set; }

            public PelletComponent() : base(true, false)
            {
            }

            public override void Update() {
                base.Update();
            
                // fast fail if the pooled shot is no longer alive
                if (Dead) return;

                var level = SceneAs<Level>();
                
                Entity.Position += Settings.Direction * Settings.Speed * Engine.DeltaTime;

                if (!level.IsInBounds(Entity) || Settings.CollideWithSolids && Entity.CollideCheck<Solid>(Entity.Position))
                    Destroy();
            }

            public void Destroy() {
                Dead = true;
                Entity.RemoveSelf();
            }
            
            public struct PelletComponentSettings {
                public bool CollideWithSolids;
                public Color Color;
                public Vector2 Direction;
                public Vector2 Origin;
                public float Speed;
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
    public class PelletFiringComponent<TShot> : PelletFiringComponent where TShot : Entity, new() {
        protected override Entity CreateShot() => Engine.Pooler.Create<TShot>();
    }
}