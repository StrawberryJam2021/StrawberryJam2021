using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/UpsidedownSpring")]
    class UpsidedownSpring : Spring {

        public readonly float strength, xAxisFriction;

        public UpsidedownSpring(Vector2 position, float strength, float xAxisFriction) : base(position, Spring.Orientations.Floor, false) {

            this.strength = strength;
            this.xAxisFriction = xAxisFriction;

            // remove components that need removing
            Remove(Get<PlayerCollider>());
            Remove(Get<PufferCollider>());
            Remove(Get<HoldableCollider>());

            // replace them with ones we need
            Add(new HoldableCollider(new Action<Holdable>(onHoldable), null));

            sprite.Position = sprite.Position + Vector2.UnitY * 7;
            sprite.Rotation = (float) Math.PI;
            Collider = new Hitbox(16f, 6f, -8f, 7f);
            staticMover.SolidChecker = (Solid solid) => CollideCheck(solid, position - Vector2.UnitY);
            staticMover.JumpThruChecker = (JumpThru jt) => { return false; };
        }

        public UpsidedownSpring(EntityData data, Vector2 offset) : this(data.Position + offset, data.Float("strength", 1), data.Float("xAxisFriction", 0.5f)) {

        }

        private void onHoldable(Holdable holdable) {
            if (holdable.Entity is SkyLantern) {
                holdable.HitSpring(this);
                BounceAnimate();
            }
        }

    }
}
