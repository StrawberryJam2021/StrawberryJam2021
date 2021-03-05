using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketJellyController")]
    [Tracked(false)]
    class PocketJellyController : Entity {

        private Player player;
        private bool enabled = false;
        private Glider glider;
        private System.Collections.IEnumerator coroutine;

        public PocketJellyController() {
            AddTag(Tags.Global);
        }

        public void Enable(Player player) {
            this.player = player;
            enabled = true;
        }

        public void Disable() {
            enabled = false;
        }

        public override void Update() {
            base.Update();
            if (enabled && Input.GrabCheck && player?.Holding == null) {
                glider = new Glider(player.Position, false, false);
                Scene.Add(glider);
            } else if (enabled && !Input.GrabCheck && glider != null && coroutine == null) {
                player.Drop();
                coroutine = (System.Collections.IEnumerator) glider.GetType().GetMethod("DestroyAnimationRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(glider, new object[] { });
            } else if (enabled && coroutine != null) {
                glider.Add(new Coroutine(coroutine, true));
                glider.GetType().GetField("destroyed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glider, true);
                coroutine = null;
                glider = null;
            }
        }
    }
}
