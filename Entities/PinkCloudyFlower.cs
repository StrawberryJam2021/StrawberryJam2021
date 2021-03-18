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
        private Cloud cloud;
        private Vector2? oldPosition;
        private Vector2 carryOffset;

        public PinkCloudyFlower(EntityData data, Vector2 offset) : base(data, offset) {
            Get<Sprite>().SetColor(Color.MediumOrchid);
            cloud = new Cloud(Position, false);
            carryOffset = new Vector2(0, -20);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(cloud);
        }

        public override void Update() {
            base.Update();

            if (cloud.HasPlayerRider()) {
                if (oldPosition == null) {
                    oldPosition = Position;
                }

                Position = cloud.Position - carryOffset;
            } else {
                if (oldPosition != null) {
                    Position = oldPosition.Value;
                    oldPosition = null;
                }
                
                cloud.Position = Position + carryOffset;
            }
        }

        public override void Render() {
            base.Render();
        }
    }
}