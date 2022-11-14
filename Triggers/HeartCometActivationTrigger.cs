using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Celeste.Mod.StrawberryJam2021.Effects;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/HeartCometActivationTrigger")]
    public class HeartCometActivationTrigger : Trigger {
        public HeartCometActivationTrigger(EntityData data, Vector2 offset) : base(data, offset) {}

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            Level level = Scene as Level;
            foreach (HeartComet heartComet in level.Background.GetEach<HeartComet>()) {
                heartComet.Activated = true;
            }
        }
    }
}
