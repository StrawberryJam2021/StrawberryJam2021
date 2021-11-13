using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    // This controller enables a music parameter when the player is off the ground for a certain amount of time.
    // The parameter is turned off when the player lands.
    [CustomEntity("SJ2021/AirTimeMusicController")]
    public class AirTimeMusicController : Entity {
        float airtimeThreshold = 0;
        string param;

        public AirTimeMusicController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            airtimeThreshold = data.Float("activationThreshold");
            param = data.Attr("musicParam", "");
        }

        float lastGroundTime = 0;

        private Player player;

        public override void Update() {
            base.Update();

            player ??= Scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            if (player.OnSafeGround)
                lastGroundTime = Scene.TimeActive;

            if (Scene.TimeActive - lastGroundTime > airtimeThreshold)
                Audio.SetMusicParam(param, 1);
            else
                Audio.SetMusicParam(param, 0);
        }
    }
}
