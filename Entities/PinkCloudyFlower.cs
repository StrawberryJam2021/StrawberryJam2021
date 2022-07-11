using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/PinkCloudyFlower")]
    class PinkCloudyFlower : Glider {
        private JumpThru platform;
        private Vector2 carryOffset;
        private Holdable hold;
        private bool waiting, returning;
        private float speedY;
        private Action orig_onPickup;
        private Action<Vector2> orig_onRelease;

        public PinkCloudyFlower(EntityData data, Vector2 offset) : base(data, offset) {
            Get<Sprite>().SetColor(Color.MediumOrchid);
            carryOffset = new Vector2(-16, -20);
            hold = Get<Holdable>();
            orig_onPickup = hold.OnPickup;
            orig_onRelease = hold.OnRelease;

            hold.OnPickup = onPickup;
            hold.OnRelease = onRelease;
            platform = new JumpThru(Position + carryOffset, 32, false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(platform);
        }

        public override void Update() {
            base.Update();

            if (platform.GetPlayerRider() == null) {
                Collidable = true;
            }

            platformUpdate();
        }

        private void platformUpdate() {
            if (waiting) {
                Player playerRider = platform.GetPlayerRider();
                if (playerRider != null && playerRider.Speed.Y >= 0f) {
                    Collidable = false;
                    speedY = 180f;
                    waiting = false;
                    Audio.Play("event:/game/04_cliffside/cloud_blue_boost", Position);
                    platform.MoveV(speedY * Engine.DeltaTime);
                    platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * Engine.DeltaTime);
                    return;
                }
                platform.MoveTo(Position + carryOffset);
            } else if (returning) {
                speedY = Calc.Approach(speedY, 180f, 600f * Engine.DeltaTime);
                platform.MoveTowardsY(Position.Y + carryOffset.Y, speedY * Engine.DeltaTime);
                platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * Engine.DeltaTime);
                if (platform.ExactPosition.Y == Position.Y + carryOffset.Y) {
                    returning = false;
                    waiting = true;
                    speedY = 0f;
                    return;
                }
            } else {
                if (platform.Y >= Position.Y + carryOffset.Y) {
                    speedY -= 1200f * Engine.DeltaTime;
                } else {
                    speedY += 1200f * Engine.DeltaTime;
                    if (speedY >= -100f) {
                        Player playerRider2 = platform.GetPlayerRider();
                        if (playerRider2 != null && playerRider2.Speed.Y >= 0f) {
                            playerRider2.Speed.Y = -200f;
                            Collidable = false;
                        }
                        returning = true;
                    }
                }
                float liftSpeed = speedY;
                if (liftSpeed < 0f) {
                    liftSpeed = -220f;
                }
                platform.MoveV(speedY * Engine.DeltaTime, liftSpeed);
                platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * Engine.DeltaTime);
            }
        }

        private void onPickup() {
            platform.AddTag(Tags.Persistent);
            orig_onPickup();
        }

        private void onRelease(Vector2 force) {
            platform.RemoveTag(Tags.Persistent);
            orig_onRelease(force);
        }
    }
}