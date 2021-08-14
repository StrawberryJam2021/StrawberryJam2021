using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/AirTimeMusicController")]
    public class AirTimeMusicController : Entity {
        float airtimeThreshold = 0;

        public AirTimeMusicController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            airtimeThreshold = data.Float("activationThreshold");
        }

        float lastGroundTime = 0;

        public override void Update() {
            base.Update();
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player.OnSafeGround)
                lastGroundTime = Scene.TimeActive;

            if (Scene.TimeActive - lastGroundTime > airtimeThreshold) {
                // turn on music parameter
            } else {
                // turn off music parameter
            }
        }
    }
}
