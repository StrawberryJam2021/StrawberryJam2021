using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/PocketUmbrellaTrigger")]
    class PocketUmbrellaTrigger : Trigger {
        private bool Enable = true, revertOnLeave = false, prevVal;
        private float staminaCost, prevCost, cooldown, prevCooldown;

        public PocketUmbrellaTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("enabled", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
            staminaCost = data.Float("staminaCost", 100 / 2.2f);
            cooldown = data.Float("cooldown", 0.2f);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void OnEnter(Player player) {
            //Logger.Log("SJ2021/PUC", "enter");
            base.OnEnter(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (controller == null) {
                Scene.Add(controller = new PocketUmbrellaController());
            }
            prevVal = controller.Enabled;
            prevCost = controller.StaminaCost;
            prevCooldown = controller.Cooldown;
            if (Enable) {
                //Logger.Log("SJ2021/PUC", "enable");
                controller.Enable();
                controller.setCost(staminaCost);
                controller.setCooldown(cooldown);
                controller.player = player;
            } else {
                //Logger.Log("SJ2021/PUC", "disable");
                Scene.Remove(controller);
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (revertOnLeave && controller != null) {
                controller.setCost(prevCost);
                controller.setCooldown(prevCooldown);
                if (prevVal) {
                    controller.Enable();
                } else {
                    Scene.Remove(controller);
                }
            }
        }


    }
}
