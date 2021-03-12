using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste.Mod.Entities;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketJellyController")]
    [Tracked(false)]
    class PocketJellyController : Entity {

        private Player player;
        private bool enabled = false;
        private Glider glider;
        private float maxHoldDelay, holdDelay, staminaCost;
        private FieldInfo gliderDestroyed_FI, playerMinHoldTime_FI;
        private MethodInfo pickup_MI, coroutine_MI;

        public PocketJellyController() {
            AddTag(Tags.Global);
            maxHoldDelay = 0.1f;
            gliderDestroyed_FI = typeof(Glider).GetField("destroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            playerMinHoldTime_FI =  typeof(Player).GetField("minHoldTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            pickup_MI = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);
            coroutine_MI = typeof(Glider).GetMethod("DestroyAnimationRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            maxHoldDelay = 0.1f;
            staminaCost = 45.454544f;
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
            if (holdDelay > 0) {
                holdDelay -= Engine.DeltaTime;
            }

            if (enabled) {
                if (Input.GrabCheck) {
                    if (player?.Holding == null && holdDelay <= 0 && player.Stamina > 0 && exclusiveGrabCollide()) {
                        glider = new Glider(player.Position, false, false);
                        Scene.Add(glider);
                        pickup_MI.Invoke(player, new object[] { glider.Hold });
                    } else if (player?.Holding == glider?.Hold && player.Stamina > 0) {
                        player.Stamina -= staminaCost * Engine.DeltaTime;
                    } else if (player?.Holding == glider?.Hold && player?.Holding != null && player.Stamina <= 0) {
                        dropJelly();
                    }
                } else {
                    if (player?.Holding == glider?.Hold && player?.Holding != null) {
                        dropJelly();
                    }
                }
            }
        }

        private void dropJelly() {
            player.Drop();
            holdDelay = maxHoldDelay;
            glider.Add(new Coroutine((System.Collections.IEnumerator) coroutine_MI.Invoke(glider, new object[] { })));
            gliderDestroyed_FI.SetValue(glider, true);
            glider = null;
        }

        private bool exclusiveGrabCollide() {
            if (player?.Scene?.Tracker?.GetComponents<Holdable>() == null) {
                return false;
            }
            foreach (Component component in player?.Scene?.Tracker.GetComponents<Holdable>()) {
                Holdable holdable = (Holdable) component;
                if (holdable.Check(player)) {
                    return false;
                }
            }
            return true;
        }
    }
}
