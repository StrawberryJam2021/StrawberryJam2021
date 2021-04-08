using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/PinkCloudyFlower")]
    class PinkCloudyFlower : Glider {
        private JumpThru platform;
        private Vector2 carryOffset;
        private Holdable hold;
        private bool waiting, returning;
        private float speed;

        public PinkCloudyFlower(EntityData data, Vector2 offset) : base(data, offset) {
            Get<Sprite>().SetColor(Color.MediumOrchid);
            carryOffset = new Vector2(-16, -20);
            hold = Get<Holdable>();
            platform = new JumpThru(Position + carryOffset, 32, false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(platform);
        }

        public override void Update() {
            base.Update();

            if (platform.GetPlayerRider() == null)
            {
                hold.Active = true;
            }

            platformUpdate();
        }

        private void platformUpdate() {
            if (waiting) {
                Player playerRider = platform.GetPlayerRider();
                if (playerRider != null && playerRider.Speed.Y >= 0f) {
                    //this.canRumble = true;
                    hold.Active = false;
                    speed = 180f;
                    waiting = false;
                    Audio.Play("event:/game/04_cliffside/cloud_blue_boost", Position);
                    return;
                }
                platform.MoveTo(Position + carryOffset);
            } else if (returning) {
                speed = Calc.Approach(speed, 180f, 600f * Engine.DeltaTime);
                platform.MoveTowardsY(Position.Y + carryOffset.Y, speed * Engine.DeltaTime);
                if (platform.ExactPosition.Y == Position.Y + carryOffset.Y) {
                    returning = false;
                    waiting = true;
                    speed = 0f;
                    return;
                }
            } else {
                if (platform.Y >= Position.Y + carryOffset.Y) {
                    speed -= 1200f * Engine.DeltaTime;
                } else {
                    speed += 1200f * Engine.DeltaTime;
                    if (speed >= -100f) {
                        Player playerRider2 = platform.GetPlayerRider();
                        if (playerRider2 != null && playerRider2.Speed.Y >= 0f) {
                            playerRider2.Speed.Y = -200f;
                            hold.Active = false;
                        }
                        returning = true;
                    }
                }
                float num = speed;
                if (num < 0f) {
                    num = -220f;
                }
                platform.MoveV(speed * Engine.DeltaTime, num);
            }
        }

        public override void Render() {
            base.Render();
        }
    }
}