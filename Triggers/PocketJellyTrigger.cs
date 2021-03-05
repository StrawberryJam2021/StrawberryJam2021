using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/PocketJellyTrigger")]
    class PocketJellyTrigger : Trigger {
        private PocketJellyController Manager;
        private bool Enable = true;

        public PocketJellyTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("Enable", true);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Manager = scene.Tracker.GetEntity<PocketJellyController>();
            if (Manager == null) {
                var m = new PocketJellyController();
                scene.Add(m);
                Manager = m;
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (Enable) {
                Manager.Enable(player);
            } else {
                Manager.Disable();
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
        }


    }
}
