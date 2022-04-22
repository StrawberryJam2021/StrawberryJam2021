using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/UpsidedownSpring")]
    public class UpsidedownSpring : Spring {

        public readonly float strength, xAxisFriction;
        private readonly StaticMover staticMover;
        private readonly Sprite sprite;
        private readonly Wiggler wiggler;

        public UpsidedownSpring(Vector2 position, float strength, float xAxisFriction) : base(position, Orientations.Floor, false) {

            this.strength = strength;
            this.xAxisFriction = xAxisFriction;

            // extract components
            staticMover = Get<StaticMover>();
            sprite = Get<Sprite>();
            wiggler = Get<Wiggler>();

            // remove components that need removing
            Remove(Get<PlayerCollider>());
            Remove(Get<PufferCollider>());
            Remove(Get<HoldableCollider>());

            // replace them with ones we need
            Add(new HoldableCollider(onHoldable, null));

            sprite.Position = sprite.Position + Vector2.UnitY * 7;
            sprite.Rotation = (float) Math.PI;
            Collider = new Hitbox(16f, 6f, -8f, 7f);
            staticMover.SolidChecker = solid => CollideCheck(solid, position - Vector2.UnitY);
            staticMover.JumpThruChecker = _ => false;
        }

        public UpsidedownSpring(EntityData data, Vector2 offset) : this(data.Position + offset, data.Float("strength", 1), data.Float("xAxisFriction", 0.5f)) {

        }

        private void BounceAnimate() {
            Audio.Play("event:/game/general/spring", BottomCenter);
            staticMover.TriggerPlatform();
            sprite.Play("bounce", true, false);
            wiggler.Start();
        }

        private void onHoldable(Holdable holdable) {
            if (holdable.Entity is SkyLantern) {
                holdable.HitSpring(this);
                BounceAnimate();
            }
        }

    }
}
