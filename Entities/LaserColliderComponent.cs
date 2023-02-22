using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

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
        public Vector2 Offset { get; set; }
        public bool CollidedWithScreenBounds { get; private set; }

        public LaserColliderComponent() : this(Vector2.Zero) {
        }

        public LaserColliderComponent(Vector2 offset) : base(true, false) {
            Offset = offset;
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
            UpdateBeam(true);
        }

        public override void EntityAwake() {
            base.EntityAwake();
            UpdateBeam(true);
        }

        public override void Update() {
            base.Update();
            UpdateBeam();
        }

        private void resizeHitbox(float size) {
            if (!(Entity is OrientableEntity orientableEntity)) return;

            switch (orientableEntity.Orientation) {
                case OrientableEntity.Orientations.Up:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.BottomCenter = Offset;
                    break;

                case OrientableEntity.Orientations.Down:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.TopCenter = Offset;
                    break;

                case OrientableEntity.Orientations.Left:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterRight = Offset;
                    break;

                case OrientableEntity.Orientations.Right:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterLeft = Offset;
                    break;
            }
        }

        public void UpdateBeam(bool fromEntityAdded = false) {
            if (!(Entity is OrientableEntity orientableEntity)) return;
            var level = SceneAs<Level>();

            float high = orientableEntity.Orientation switch {
                OrientableEntity.Orientations.Up => orientableEntity.Position.Y + Offset.Y - level.Bounds.Top,
                OrientableEntity.Orientations.Down => level.Bounds.Bottom - orientableEntity.Position.Y - Offset.Y,
                OrientableEntity.Orientations.Left => orientableEntity.Position.X + Offset.X - level.Bounds.Left,
                OrientableEntity.Orientations.Right => level.Bounds.Right - orientableEntity.Position.X - Offset.X,
                _ => 0
            };

            int low = 0, safety = 1000;

            // force non-collidable invisible barriers to be collidable if our entity was just added
            List<Entity> barriers = null;
            if (fromEntityAdded && CollideWithSolids) {
                barriers = new List<Entity>();
                barriers.AddRange(level.Tracker.GetEntities<InvisibleBarrier>().Where(ib => !ib.Collidable));
                barriers.ForEach(ib => ib.Collidable = true);
            }

            // first check if the laser hits the edge of the screen
            resizeHitbox(high);
            CollidedWithScreenBounds = !CollideWithSolids || !solidCollideCheck();
            if (!CollidedWithScreenBounds) {
                // perform a binary search to hit the nearest solid
                while (safety-- > 0) {
                    int pivot = (int) (low + (high - low) / 2f);
                    resizeHitbox(pivot);
                    if (pivot == low)
                        break;
                    if (solidCollideCheck()) {
                        high = pivot;
                    } else {
                        low = pivot;
                    }
                }
            }

            // reset collidable for those we modified
            barriers?.ForEach(ib => ib.Collidable = false);
        }

        private bool solidCollideCheck() {
            var oldCollider = Entity.Collider;
            Entity.Collider = Collider;
            bool didCollide = Entity.CollideCheck<Solid>();
            Entity.Collider = oldCollider;
            return didCollide;
        }
    }
}