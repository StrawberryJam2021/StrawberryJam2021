using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/TheoKillBoxTrigger")]
    public class TheoKillBoxTrigger : Trigger {
        public TheoKillBoxTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void Update() {
            if (!SaveData.Instance.Assists.Invincible) {
                foreach (TheoCrystal crystal in CollideAll<TheoCrystal>()) {
                    crystal.Die();
                }
            }
        }
    }
}
