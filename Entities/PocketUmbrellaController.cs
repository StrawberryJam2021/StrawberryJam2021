using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketUmbrellaController")]
    [Tracked(false)]
    class PocketUmbrellaController : Entity {

        private Vector2 spawnOffset;
        private float holdDelay;
        private bool prevGrab;
        public bool Enabled { get; set; }
        public float StaminaCost { get; set; }
        public float Cooldown { get; set; }

        public string MusicParam { get; set; }

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
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        }

        public static void Unload() {
            On.Celeste.Player.Drop -= Player_Drop;
            On.Celeste.Player.Throw -= Player_Throw;
            IL.Monocle.Engine.Update -= Engine_Update;
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        }

        private static void Player_NormalUpdate(ILContext il) {
            ILCursor cursor1 = new ILCursor(il);
            if (cursor1.TryGotoNext(MoveType.After,
                    instr => instr.MatchLdfld<Player>("Stamina"),
                    instr => instr.MatchLdcR4(0),
                    instr => instr.MatchBleUn(out _),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchCallvirt<Player>("get_Holding"))) {
                ILCursor cursor0 = cursor1.Clone();
                if (cursor1.TryGotoNext(MoveType.After,
                        instr => instr.MatchLdfld<Player>("Stamina"),
                        instr => instr.MatchLdcR4(0),
                        instr => instr.MatchBleUn(out _),
                        instr => instr.MatchLdarg(0),
                        instr => instr.MatchCallvirt<Player>("get_Holding"))) {
                    ModForPocketUmbrella(cursor0);
                    ModForPocketUmbrella(cursor1);
                }
            }
        }

        private static void ModForPocketUmbrella(ILCursor cursor) {
            cursor.EmitDelegate<Func<Holdable, bool>>((h) => h == null ? false : !(h.Entity is not null && h.Entity is PocketUmbrella));
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
                if (shouldGrabWall(player)) {
                    player.StateMachine.State = Player.StClimb;
                } else if (grab_check()) {
                    if (player.Holding == null && exclusiveGrabCollide(player)) {
                        if (trySpawnJelly(out PocketUmbrella umbrella, player)) {
                            Scene.Add(umbrella);
                        }
                    }
                }
            }
        }

        private bool shouldGrabWall(Player player) {
            // this essentially emulates the checks that Player.NormalUpdate makes to figure out if the player should transition to grab state or not
            // except for the Holding == null part as well as all stamina and ducking checks because the player *must* be unducked and have stamina to hold the umbrella.
            return player.Holding?.Entity is PocketUmbrella && player.StateMachine.State == Player.StNormal &&
                    Input.GrabCheck && player.Speed.Y > 0f && !(Math.Sign(player.Speed.X) == -(int) player.Facing) &&
                    // all the above checks are common for the two individual situations where the game decides to switch to StGrab, the or'd checks below are the unique checks.
                    (
                        (player.ClimbCheck((int) player.Facing, 0) && !SaveData.Instance.Assists.NoGrabbing) ||
                        !SaveData.Instance.Assists.NoGrabbing && Input.MoveY < 1f && weirdCheck(player)
                    );

        }

        //this is called that bc I honestly have no clue what it does or why it does it but hey its in the original code so its here too.
        private bool weirdCheck(Player player) {
            for (int i = 1; i <= 2; i++) {
                if (!player.CollideCheck<Solid>(player.Position + Vector2.UnitY * -i) && player.ClimbCheck((int) player.Facing, -i)) {
                    player.MoveVExact(-i, null, null);
                    return true;
                }
            }

            return false;
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
                    if (controller.grab_check()) {
                        if (player.Holding == null && controller.exclusiveGrabCollide(player)) {
                            if (controller.trySpawnJelly(out PocketUmbrella umbrella, player)) {
                                controller.Scene.Add(umbrella);
                            }
                        }
                    }
                }
            }
        }

        private bool grab_check() {
            bool result = Input.GrabCheck && !prevGrab;
            prevGrab = Input.GrabCheck;
            return result;
        }

        private bool trySpawnJelly(out PocketUmbrella umbrella, Player player) {
            umbrella = new PocketUmbrella(player.Position + spawnOffset, StaminaCost, MusicParam);
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
            return player.Stamina > 20 && !player.Ducking && !player.ClimbCheck((int) player.Facing, 0) && playerStateCheck(player);
        }

        private bool playerStateCheck(Player player) {
            return player.StateMachine.State is Player.StNormal or Player.StDash or Player.StLaunch;
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
