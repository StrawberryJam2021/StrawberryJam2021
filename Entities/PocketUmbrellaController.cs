using Monocle;
using Celeste.Mod.Entities;
using System.Reflection;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketUmbrellaController")]
    [Tracked(false)]
    class PocketUmbrellaController : Entity {

        public Player player;
        private Vector2 spawnOffset;
        private float holdDelay;
        public static FieldInfo gliderDestroyed_FI;
        public static MethodInfo pickup_MI, destroy_coroutine_MI, wallJumpCheck_MI;
        public bool Enabled { get; set; }
        public float StaminaCost { get; set; }
        public float Cooldown { get; set; }

        public PocketUmbrellaController() : this (0, false) {
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
            On.Celeste.Player.Added += Player_Added;


            gliderDestroyed_FI = typeof(Glider).GetField("destroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            pickup_MI = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);
            destroy_coroutine_MI = typeof(Glider).GetMethod("DestroyAnimationRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            wallJumpCheck_MI = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Unload() {
            On.Celeste.Player.Drop -= Player_Drop;
            On.Celeste.Player.Throw -= Player_Throw;
            On.Celeste.Player.Added -= Player_Added;
        }

        private static void Player_Throw(On.Celeste.Player.orig_Throw orig, Player self) {
            if (self.Holding?.Entity is  not PocketUmbrella) {
                orig(self);
                return;
            }
            checkDrop(self);

            if (Input.MoveY.Value == 1) {
                self.Drop();
            } else {
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                self.Holding.Release(Vector2.UnitX * (float) self.Facing);
                //self.Speed.X = this.Speed.X + 80f * (float) (-(float) this.Facing);
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

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene) {
            orig(self, scene);
            PocketUmbrellaController controller = self.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (controller != null) {
                controller.player = self;
            }
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

            if (Enabled && player?.Dead == false) {
                if (grabCheck()) {
                    if (player?.Holding == null && exclusiveGrabCollide()) {
                        if (trySpawnJelly(out PocketUmbrella umbrella)) {
                            Scene.Add(umbrella);
                            pickup_MI.Invoke(player, new object[] { umbrella.Hold });
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

        private bool trySpawnJelly(out PocketUmbrella umbrella) {
            umbrella = new PocketUmbrella(player.Position + spawnOffset, false, false, StaminaCost);
            if (!checkSpawnCondition()) {
                return false;
            }
            foreach (Entity entity in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                if (umbrella.Collider.Collide(entity.Collider)) {
                    return false;
                }
            }
            return true;
        }

        private bool checkSpawnCondition() {
            return player.Stamina > 20 && !player.Ducking && !wallJumpCheck(1) && !wallJumpCheck(-1) && playerStateCheck() && !player.OnGround(1);
        }

        private bool playerStateCheck() {
            return player.StateMachine.State is Player.StNormal or Player.StDash or Player.StLaunch;
        }

        private bool wallJumpCheck(int dir) {
            return (bool) wallJumpCheck_MI.Invoke(player, new object[] { dir });
        }

        private void dropUmbrella(PocketUmbrella umbrella) {
            holdDelay = Cooldown;
            gliderDestroyed_FI.SetValue(umbrella, true);
            umbrella.Collidable = false;
            umbrella.Hold.Active = false;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            umbrella.Add(new Coroutine((System.Collections.IEnumerator) destroy_coroutine_MI.Invoke(umbrella, new object[] { })));
        }

        private bool exclusiveGrabCollide() {
            if (player?.Scene?.Tracker?.GetComponents<Holdable>().Count == 0) {
                return true;
            }
            List<Component> components = player?.Scene?.Tracker?.GetComponents<Holdable>();
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
