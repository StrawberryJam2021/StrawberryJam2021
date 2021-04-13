using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Calculates and renders the beam part of a laser emitter.
    /// </summary>
    /// <remarks>
    /// Opacity of the beam edges is calculated as <see cref="Alpha"/> times <see cref="alphaMultiplier"/>, where
    /// the multiplier represents an optional flickering based on a <see cref="SineWave"/>.
    /// The centre third of the beam is twice that opacity.<br/>
    /// The size of the laser hitbox is calculated using a binary search algorithm for performance.
    /// </remarks>
    public class LaserBeamComponent : SineWave {
        public Color Color { get; set; }
        public float Alpha { get; set; }
        public float Thickness { get; set; }
        public bool CollideWithSolids { get; set; }
        public bool Flicker { get; set; }
        
        public Hitbox Collider { get; } = new Hitbox(0, 0);

        private const float flickerFrequency = 4f;
        private const float flickerRange = 4f;
        
        private Vector2 target;
        private float alphaMultiplier = 1f;
        
        public LaserBeamComponent() : base(flickerFrequency) {
            Visible = true;
            OnUpdate = v => alphaMultiplier = 1 - (v + 1f) * 0.5f / flickerRange;
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
            updateBeam();
        }

        public override void Render() {
            if (!Entity.Collidable || !(Entity is OrientableEntity orientableEntity))
                return;

            var color = Color * Alpha * (Flicker ? alphaMultiplier : 1f);
                
            Draw.Rect(Collider.Bounds, color);

            float thickness = orientableEntity.Orientation == OrientableEntity.Orientations.Left || orientableEntity.Orientation == OrientableEntity.Orientations.Right
                ? Collider.Height / 3f
                : Collider.Width / 3f;

            Draw.Line(Entity.X, Entity.Y, Entity.X + target.X, Entity.Y + target.Y, color, thickness);
        }

        public override void Update() {
            base.Update();
            
            if (Entity.Collidable)
                updateBeam();
        }
        
        private void resizeKillbox(float size) {
            if (!(Entity is OrientableEntity hazardEmitter)) return;
            
            switch (hazardEmitter.Orientation) {
                case OrientableEntity.Orientations.Up:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.BottomCenter = Vector2.Zero;
                    target = Collider.TopCenter;
                    break;
                
                case OrientableEntity.Orientations.Down:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.TopCenter = Vector2.Zero;
                    target = Collider.BottomCenter;
                    break;
                
                case OrientableEntity.Orientations.Left:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterRight = Vector2.Zero;
                    target = Collider.CenterLeft;
                    break;
                
                case OrientableEntity.Orientations.Right:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterLeft = Vector2.Zero;
                    target = Collider.CenterRight;
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
            resizeKillbox(high);
            if (!CollideWithSolids || !orientableEntity.CollideCheck<Solid>()) return;
            
            // perform a binary search to hit the nearest solid
            while (safety-- > 0) {
                int pivot = (int) (low + (high - low) / 2f);
                resizeKillbox(pivot);
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