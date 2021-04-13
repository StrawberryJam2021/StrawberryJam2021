using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class PelletFiringComponent : Component {
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
            var shot = Engine.Pooler.Create<PelletShot>().Init(this, direction * PelletSpeed, PelletColor);
            shot.Position = Entity.Position + origin;
            level?.Add(shot);
        }
        
        [Pooled]
        [Tracked]
        public class PelletShot : Entity {
            #region Private Fields

            private Sprite sprite;
            private PelletFiringComponent component;
            private Vector2 speed;
            private Color color;
            private bool dead;

            #endregion
            
            public PelletShot()
                : base(Vector2.Zero) {
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Depth = Depths.Top;

                Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
                Add(new PlayerCollider(onPlayerCollide));
            }

            public PelletShot Init(PelletFiringComponent component, Vector2 speed, Color color) {
                this.component = component;
                this.speed = speed;
                this.color = color;
                dead = false;
                return this;
            }
            
            public void Destroy() {
                component = null;
                dead = true;
                RemoveSelf();
            }

            public override void Update() {
                base.Update();

                // fast fail if the pooled shot is no longer alive or if we have no emitter assigned
                if (dead || component == null) return;

                var level = SceneAs<Level>();
                
                Position += speed * Engine.DeltaTime;

                if (!level.IsInBounds(this) || component.CollideWithSolids && CollideCheck<Solid>(Position))
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
                sprite.Color = color;
                sprite.Position = position;
                
                base.Render();
            }

            private void onPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}