using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Base class for an entity that that has an orientation.
    /// </summary>
    /// <remarks>
    /// Automatically applies a <see cref="StaticMover"/> to the backside of the entity.
    /// </remarks>
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

            // same depth as springs
            Depth = Depths.Above - 1;

            Add(new StaticMover {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = s => Collide.CheckPoint(s, Position - Orientation.Direction()),
                JumpThruChecker = jt => Collide.CheckPoint(jt, Position - Orientation.Direction()),
                OnEnable = () => Collidable = true,
                OnDisable = () => Collidable = false,
            });
        }

        /// <summary>
        /// The available orientations of an <see cref="OrientableEntity"/>, where the direction indicates the "front" of the entity.
        /// The opposite direction is used for checking <see cref="StaticMover"/>s.
        /// </summary>
        public enum Orientations {
            /// <summary>
            /// Indicates that the entity points toward the top of the screen.
            /// </summary>
            Up,

            /// <summary>
            /// Indicates that the entity points toward the bottom of the screen.
            /// </summary>
            Down,

            /// <summary>
            /// Indicates that the entity points toward the left of the screen.
            /// </summary>
            Left,

            /// <summary>
            /// Indicates that the entity points toward the right of the screen.
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

        public static Vector2 Offset(this OrientableEntity.Orientations orientation) => orientation switch {
            OrientableEntity.Orientations.Up => Vector2.UnitX,
            OrientableEntity.Orientations.Down => Vector2.UnitX,
            OrientableEntity.Orientations.Left => Vector2.UnitY,
            OrientableEntity.Orientations.Right => Vector2.UnitY,
            _ => Vector2.Zero
        };

        public static Hitbox AlignedWithOrientation(this Hitbox hitbox, OrientableEntity.Orientations orientation)
        {
            switch (orientation)
            {
                case OrientableEntity.Orientations.Up:
                    hitbox.BottomCenter = Vector2.Zero;
                    break;
                case OrientableEntity.Orientations.Down:
                    hitbox.TopCenter = Vector2.Zero;
                    break;
                case OrientableEntity.Orientations.Left:
                    hitbox.CenterRight = Vector2.Zero;
                    break;
                case OrientableEntity.Orientations.Right:
                    hitbox.CenterLeft = Vector2.Zero;
                    break;
            }

            return hitbox;
        }
    }
}