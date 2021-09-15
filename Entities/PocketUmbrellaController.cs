using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketUmbrellaController")]
    [Tracked(false)]
    class PocketUmbrellaController : Entity {

        private Vector2 spawnOffset;
        private float holdDelay;
        public static MethodInfo wallJumpCheck_MI;
        public bool Enabled { get; set; }
        public float StaminaCost { get; set; }
        public float Cooldown { get; set; }

        public string MusicLayer { get; set; }

        public PocketUmbrellaController() : this(0, false) {
        }

        public PocketUmbrellaController(float cost, bool enabled, float cooldown = 0.2f) {
            AddTag(Tags.Global);
            StaminaCost = cost;
            Enabled = enabled;
            Cooldown = cooldown;
            spawnOffset = new Vector2(0f, -12f);
        }

        public static void Load() {
            On.Celeste.Player.Drop += Player_Drop;
            On.Celeste.Player.Throw += Player_Throw;
            IL.Monocle.Engine.Update += Engine_Update;

            wallJumpCheck_MI = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Unload() {
            On.Celeste.Player.Drop -= Player_Drop;
            On.Celeste.Player.Throw -= Player_Throw;
            IL.Monocle.Engine.Update -= Engine_Update;
        }

        private static void Engine_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // Thanks coloursofnoise!
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdsfld<Engine>("FreezeTimer"))) {
                cursor.EmitDelegate<Action>(frozen_update);
            }
        }

        private static void Player_Throw(On.Celeste.Player.orig_Throw orig, Player self) {
            if (self.Holding?.Entity is not PocketUmbrella) {
                orig(self);
                return;
            }
            checkDrop(self);

            if (Input.MoveY.Value == 1) {
                self.Drop();
            } else {
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                self.Holding.Release(Vector2.UnitX * (float) self.Facing);
                self.Play("event:/char/madeline/crystaltheo_throw", null, 0f);
                self.Sprite.Play("throw", false, false);
            }
            self.Holding = null;
        }

        private static void Player_Drop(On.Celeste.Player.orig_Drop orig, Player self) {
            if (self.Holding?.Entity is PocketUmbrella) {
                checkDrop(self);
            }
            orig(self);
        }

        private static void checkDrop(Player player) {
            PocketUmbrellaController controller = player.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (controller != null && controller.Enabled) {
                controller.dropUmbrella(player.Holding.Entity as PocketUmbrella);
            }
        }

        public override void Update() {
            base.Update();
            if (holdDelay > 0) {
                holdDelay -= Engine.DeltaTime;
                return;
            }
            Player player = Engine.Scene.Tracker.GetEntity<Player>();
            if (player is null) {
                return;
            }
            if (Enabled && player.Dead == false) {
                if (grabCheck()) {
                    if (player.Holding == null && exclusiveGrabCollide(player)) {
                        if (trySpawnJelly(out PocketUmbrella umbrella, player)) {
                            Scene.Add(umbrella);
                        }
                    }
                }
            }
        }

        public static void frozen_update() {
            if (Engine.FreezeTimer <= 0) {
                return;
            }
            Player player = Engine.Scene.Tracker.GetEntity<Player>();
            if (player is null) {
                return;
            }
            foreach (PocketUmbrellaController controller in Engine.Scene.Tracker.GetEntities<PocketUmbrellaController>()) {
                if (controller.Enabled && player.Dead == false && player.StateMachine.State != Player.StDash && !Input.Dash.Check) {
                    if (controller.grabCheck()) {
                        if (player.Holding == null && controller.exclusiveGrabCollide(player)) {
                            if (controller.trySpawnJelly(out PocketUmbrella umbrella, player)) {
                                controller.Scene.Add(umbrella);
                            }
                        }
                    }
                }
            }
        }

        private bool grabCheck() {
            bool pressed = Input.Grab.Pressed;
            Input.Grab.ConsumePress();
            return pressed;
        }

        private bool trySpawnJelly(out PocketUmbrella umbrella, Player player) {
            umbrella = new PocketUmbrella(player.Position + spawnOffset, StaminaCost, MusicLayer);
            if (!checkSpawnCondition(player)) {
                return false;
            }
            foreach (Entity entity in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                if (umbrella.Collider.Collide(entity.Collider)) {
                    return false;
                }
            }
            return true;
        }

        private bool checkSpawnCondition(Player player) {
            return player.Stamina > 20 && !player.Ducking && !wallJumpCheck(1, player) && !wallJumpCheck(-1, player) && playerStateCheck(player);
        }

        private bool playerStateCheck(Player player) {
            return player.StateMachine.State is Player.StNormal or Player.StDash or Player.StLaunch;
        }

        private bool wallJumpCheck(int dir, Player player) {
            return (bool) wallJumpCheck_MI.Invoke(player, new object[] { dir });
        }

        private void dropUmbrella(PocketUmbrella umbrella) {
            holdDelay = Cooldown;
            umbrella.destroyed = true;
            umbrella.Collidable = false;
            umbrella.Hold.Active = false;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            umbrella.Add(new Coroutine(umbrella.DestroyAnimationRoutine()));
        }

        private bool exclusiveGrabCollide(Player player) {
            if (player.Scene?.Tracker?.GetComponents<Holdable>().Count == 0) {
                return true;
            }
            List<Component> components = player.Scene?.Tracker?.GetComponents<Holdable>();
            if (components is not null) {
                foreach (Component component in components) {
                    Holdable holdable = (Holdable) component;
                    if (holdable.Check(player)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
