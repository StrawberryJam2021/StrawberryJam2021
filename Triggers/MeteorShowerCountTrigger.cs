using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Celeste.Mod.StrawberryJam2021.Effects;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/MeteorShowerCountTrigger")]
    public class MeteorShowerCountTrigger : Trigger {
        private int setAmnt;
        private bool onlyOnce;

        private bool triggered = false;

        public MeteorShowerCountTrigger(EntityData data, Vector2 offset) : base (data, offset) {
            setAmnt = data.Int("NumberOfMeteors", 1);
            onlyOnce = data.Bool("OnlyOnce", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            if (triggered && onlyOnce)
                return;

            Level level = Scene as Level;
            foreach (MeteorShower meteorShower in level.Background.GetEach<MeteorShower>()) {
                meteorShower.MeteorCount = setAmnt;
                triggered = true;
            }
        }
    }
}
