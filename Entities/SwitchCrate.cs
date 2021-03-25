using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/SwitchGate")]
    [Tracked]
    class SwitchCrate : Actor {

        public static ParticleType P_Impact;
        HoldableCollider HoldCollider;
        public Holdable Hold;
        public Vector2 Speed;

        public SwitchCrate(Vector2 Position) : base(Position) {
            base.Collider = new Hitbox(24f, 24f, 0f, 0f);
            Add(GFX.SpriteBank.Create("theo_crystal"));
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(24f, 24f, 0, 0f);
            Hold.OnPickup = OnPickUp;
            Hold.OnHitSeeker = HitSeeker;
            Hold.OnRelease = OnRelease;
        }
        private void OnPickUp() {
            Speed = Vector2.Zero;
            AddTag(Tags.Persistent);
        }
        public void HitSeeker(Seeker seeker) {
            if (!Hold.IsHeld) {
                Speed = (base.Center - seeker.Center).SafeNormalize(120f);
            }
        }

        private void OnRelease(Vector2 force) {
            RemoveTag(Tags.Persistent);
            if (force.X != 0f && force.Y == 0f) {
                force.Y = -0.4f;
            }
            Speed = force * 200f;
        }

        private void OnCollideH(CollisionData data) {
            if (data.Hit is DashSwitch) {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }
            Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
            if (Math.Abs(Speed.X) > 100f) {
                ImpactParticles(data.Direction);
            }
            Speed.X *= -0.4f;
        }

        private void OnCollideV(CollisionData data) {
            if (data.Hit is DashSwitch) {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }
            if (Speed.Y > 160f) {
                ImpactParticles(data.Direction);
            }
            if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch)) {
                Speed.Y *= -0.6f;
            } else {
                Speed.Y = 0f;
            }
        }
        public override void Update() {
            base.Update();
            MoveH(Speed.X * Engine.DeltaTime, OnCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, OnCollideV);
        }

        private void ImpactParticles(Vector2 dir) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f) {
                direction = (float) Math.PI;
                position = new Vector2(base.Right, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.X < 0f) {
                direction = 0f;
                position = new Vector2(base.Left, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            } else if (dir.Y > 0f) {
                direction = -(float) Math.PI / 2f;
                position = new Vector2(base.X, base.Bottom);
                positionRange = Vector2.UnitX * 6f;
            } else {
                direction = (float) Math.PI / 2f;
                position = new Vector2(base.X, base.Top);
                positionRange = Vector2.UnitX * 6f;
            }
            SceneAs<Level>().Particles.Emit(P_Impact, 12, position, positionRange, direction);
        }

    }
}
