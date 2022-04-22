using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/BTController")]
    public class BTController : Entity {
        private readonly float timerate;

        public BTController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            timerate = data.Float("speed");
        }

        public override void Update() {
            base.Update();
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null) {
                if (player.Dashes == 0 || player.IsIntroState || player.JustRespawned) {
                    Engine.TimeRate = 1.0f;
                } else {
                    Engine.TimeRate = timerate;
                }
            } else {
                Engine.TimeRate = 1.0f;
            }
        }
    }
}
