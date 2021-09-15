using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/PocketUmbrellaTrigger")]
    class PocketUmbrellaTrigger : Trigger {
        private bool Enable = true, revertOnLeave = false, prevVal;
        private float staminaCost, prevCost, cooldown, prevCooldown;

        private string musicLayer;

        public PocketUmbrellaTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("enabled", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
            staminaCost = data.Float("staminaCost", 100 / 2.2f);
            cooldown = data.Float("cooldown", 0.2f);
            musicLayer = data.Attr("musicParam", "");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (controller == null) {
                Scene.Add(controller = new PocketUmbrellaController());
            }
            prevVal = controller.Enabled;
            prevCost = controller.StaminaCost;
            prevCooldown = controller.Cooldown;
            if (Enable) {
                controller.Enabled = true;
                controller.StaminaCost = staminaCost;
                controller.Cooldown = cooldown;
                controller.MusicLayer = musicLayer;
            } else {
                Scene.Remove(controller);
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (revertOnLeave && controller != null) {
                controller.StaminaCost = prevCost;
                controller.Cooldown = prevCooldown;
                if (prevVal) {
                    controller.Enabled = true;
                } else {
                    Scene.Remove(controller);
                }
            }
        }


    }
}
