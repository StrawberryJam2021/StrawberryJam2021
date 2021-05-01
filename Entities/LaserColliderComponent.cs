using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Calculates the collider for the beam part of a laser emitter.
    /// </summary>
    /// <remarks>
    /// The size of the laser hitbox is calculated using a binary search algorithm for performance.
    /// </remarks>
    public class LaserColliderComponent : Component {
        public float Thickness { get; set; }
        public bool CollideWithSolids { get; set; }
        public Hitbox Collider { get; } = new Hitbox(0, 0);

        public LaserColliderComponent() : base(true, false) {
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
            updateBeam();
        }

        public override void Update() {
            base.Update();
            updateBeam();
        }
        
        private void resizeHitbox(float size) {
            if (!(Entity is OrientableEntity orientableEntity)) return;
            
            switch (orientableEntity.Orientation) {
                case OrientableEntity.Orientations.Up:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.BottomCenter = Vector2.Zero;
                    break;
                
                case OrientableEntity.Orientations.Down:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.TopCenter = Vector2.Zero;
                    break;
                
                case OrientableEntity.Orientations.Left:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterRight = Vector2.Zero;
                    break;
                
                case OrientableEntity.Orientations.Right:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterLeft = Vector2.Zero;
                    break;
            }
        }
        
        private void updateBeam() {
            if (!(Entity is OrientableEntity orientableEntity)) return;
            var level = SceneAs<Level>();

            float high = orientableEntity.Orientation switch {
                OrientableEntity.Orientations.Up => orientableEntity.Position.Y - level.Bounds.Top,
                OrientableEntity.Orientations.Down => level.Bounds.Bottom - orientableEntity.Position.Y,
                OrientableEntity.Orientations.Left => orientableEntity.Position.X - level.Bounds.Left,
                OrientableEntity.Orientations.Right => level.Bounds.Right - orientableEntity.Position.X,
                _ => 0
            };

            int low = 0, safety = 1000;

            // first check if the laser hits the edge of the screen
            resizeHitbox(high);
            if (!CollideWithSolids || !orientableEntity.CollideCheck<Solid>()) return;
            
            // perform a binary search to hit the nearest solid
            while (safety-- > 0) {
                int pivot = (int) (low + (high - low) / 2f);
                resizeHitbox(pivot);
                if (pivot == low)
                    break;
                if (orientableEntity.CollideCheck<Solid>()) {
                    high = pivot;
                } else {
                    low = pivot;
                }
            }
        }
    }
}