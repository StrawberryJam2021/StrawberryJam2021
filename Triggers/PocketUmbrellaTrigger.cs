using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/PocketUmbrellaTrigger")]
    class PocketUmbrellaTrigger : Trigger {
        private bool Enable = true, revertOnLeave = false, prevVal;
        private float staminaCost, prevCost;

        public PocketUmbrellaTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("enabled", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
            staminaCost = data.Float("staminaCost", 100 / 2.2f);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void OnEnter(Player player) {
            Logger.Log("SJ2021/PUC", "enter");
            base.OnEnter(player);
            if (!PocketUmbrellaController.instantiated) {
                Scene.Add(new PocketUmbrellaController());
            }
            prevVal = PocketUmbrellaController.Instance.Enabled;
            prevCost = PocketUmbrellaController.Instance.StaminaCost;
            if (Enable) {
                Logger.Log("SJ2021/PUC", "enable");
                PocketUmbrellaController.Instance.Enable();
                PocketUmbrellaController.Instance.setCost(staminaCost);
                PocketUmbrellaController.Instance.player = player;
            } else {
                Logger.Log("SJ2021/PUC", "disable");
                PocketUmbrellaController.Instance.Disable();
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (revertOnLeave) {
                if (prevVal) {
                    PocketUmbrellaController.Instance.Enable();
                } else {
                    PocketUmbrellaController.Instance.Disable();
                }
                PocketUmbrellaController.Instance.setCost(prevCost);
            }
        }


    }
}
