using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public abstract class OrientableEntity : Entity {
        #region Properties
        
        /// <summary>
        /// The orientation of the entity. The opposite direction will be used for static mover checks.
        /// </summary>
        public Orientations Orientation { get; }
        
        #endregion
        
        protected OrientableEntity(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset) {
            Orientation = orientation;

            ReadEntityData(data);
            AddComponents();
            
            // same depth as springs
            Depth = Depths.Above - 1;
        }

        protected virtual void AddComponents() {
            Add(new StaticMover {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = s => CollideCheck(s, Position - Orientation.Direction()),
                JumpThruChecker = jt => CollideCheck(jt, Position - Orientation.Direction()),
                OnEnable = () => Collidable = true,
                OnDisable = () => Collidable = false,
            });
        }

        protected abstract void ReadEntityData(EntityData data);
        
        /// <summary>
        /// The available orientations of an emitter, where the direction indicates which way the hazard travels.
        /// </summary>
        public enum Orientations {
            /// <summary>
            /// Indicates that the hazard fires from the emitter toward the top of the screen.
            /// </summary>
            Up,
            
            /// <summary>
            /// Indicates that the hazard fires from the emitter toward the bottom of the screen.
            /// </summary>
            Down,
            
            /// <summary>
            /// Indicates that the hazard fires from the emitter toward the left of the screen.
            /// </summary>
            Left,
            
            /// <summary>
            /// Indicates that the hazard fires from the emitter toward the right of the screen.
            /// </summary>
            Right,
        }
    }
    
    public static class OrientationsExtensions {
        public static Vector2 Direction(this OrientableEntity.Orientations orientation) => orientation switch {
            OrientableEntity.Orientations.Up => -Vector2.UnitY,
            OrientableEntity.Orientations.Down => Vector2.UnitY,
            OrientableEntity.Orientations.Left => -Vector2.UnitX,
            OrientableEntity.Orientations.Right => Vector2.UnitX,
            _ => Vector2.Zero
        };

        public static float Angle(this OrientableEntity.Orientations orientation) => orientation switch {
            OrientableEntity.Orientations.Up => 0f,
            OrientableEntity.Orientations.Down => (float) Math.PI,
            OrientableEntity.Orientations.Left => (float) -Math.PI / 2f,
            OrientableEntity.Orientations.Right => (float) Math.PI / 2f,
            _ => 0f
        };
    }
}