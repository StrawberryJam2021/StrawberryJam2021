using Monocle;
using Celeste.Mod.Entities;
using System.Reflection;
using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PocketUmbrellaController")]
    [Tracked(false)]
    class PocketUmbrellaController : Entity {

        public Player player;
        private static bool _instantiated = false, isCelesteBeta;
        private Vector2 spawnOffset;
        private bool enabled = false;
        private float maxHoldDelay, holdDelay, staminaCost;
        public FieldInfo gliderDestroyed_FI;
        public MethodInfo pickup_MI, coroutine_MI, wallJumpCheck_MI;
        private static PocketUmbrellaController _Instance;
        public static bool instantiated {
            get => _instantiated && Engine.Scene.Entities.FindFirst<PocketUmbrellaController>() != null;
            private set => _instantiated = value;
        }

        public static PocketUmbrellaController Instance {
            get => _Instance;
            private set {
                if (value != null) {
                    _Instance = value;
                    instantiated = true;
                }
            }
        }
        public bool Enabled { get => enabled; private set => enabled = value; }
        public float StaminaCost { get => staminaCost; private set => staminaCost = value; }

        public PocketUmbrellaController() : this (0, false) {
        }

        public PocketUmbrellaController(float cost, bool enabled) {
            Logger.Log("SJ2021/PUC", "ctor");
            AddTag(Tags.Global);
            StaminaCost = cost;
            Enabled = enabled;
            maxHoldDelay = 0.1f;
            gliderDestroyed_FI = typeof(Glider).GetField("destroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            pickup_MI = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);
            coroutine_MI = typeof(Glider).GetMethod("DestroyAnimationRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            wallJumpCheck_MI = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            maxHoldDelay = 0.2f;
            spawnOffset = new Vector2(0f, -12f);
            if (_Instance == null) {
                Instance = this;
            } else {
                if (enabled) {
                    Instance.Enable();
                } else {
                    Instance.Disable();
                }
                Instance.setCost(cost);
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (Instance != this) {
                scene.Add(Instance);
                RemoveSelf();
            }
        }

        public static void Load() {
            On.Celeste.Player.Drop += Player_Drop;
            On.Celeste.Player.Throw += Player_Throw;
            On.Celeste.Player.Added += Player_Added;
            isCelesteBeta = Celeste.Instance.Version >= Version.Parse("1.3.3.0");
            Logger.Log("SJ2021/PUC", $"celeste version is {Celeste.Instance.Version}, min beta version is {Version.Parse("1.3.3.0")}, isCelesteBeta {isCelesteBeta}");
        }

        public static void Unload() {
            On.Celeste.Player.Drop -= Player_Drop;
            On.Celeste.Player.Throw -= Player_Throw;
            On.Celeste.Player.Added -= Player_Added;
        }

        private static void Player_Throw(On.Celeste.Player.orig_Throw orig, Player self) {
            checkDrop(self);
            orig(self);
        }

        private static void Player_Drop(On.Celeste.Player.orig_Drop orig, Player self) {
            checkDrop(self);
            orig(self);
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene) {
            orig(self, scene);
            if (!instantiated) {
                return;
            }
            Instance.player = self;
        }

        private static void checkDrop(Player player) {
            if (!instantiated) {
                return;
            }
            if (Instance.Enabled) {
                if (player?.Holding?.Entity is PocketUmbrella umbrella) {
                    Instance.dropUmbrella(umbrella);
                }
            }
        }

        public void Enable() {
            Enabled = true;
        }

        public void Disable() {
            Enabled = false;
        }

        public void setCost(float newcost) {
            StaminaCost = Math.Max(newcost, 0);
        }

        public override void Update() {
            base.Update();
            if (holdDelay > 0) {
                holdDelay -= Engine.DeltaTime;
            }

            if (Enabled) {
                if (grabCheck()) {
                    if (player?.Holding == null && exclusiveGrabCollide() && shouldSpawnJelly()) {
                        if (safelySpawnJelly(out PocketUmbrella umbrella)) {
                            Scene.Add(umbrella);
                            pickup_MI.Invoke(player, new object[] { umbrella.Hold });
                        }
                    }
                }
            }
        }

        private bool grabCheck() {
            if (isCelesteBeta) {
                return betaGrabCheck();
            }
            return Input.Grab.Check;
        }

        private bool betaGrabCheck() {
            return Input.GrabCheck;
        }

        private bool safelySpawnJelly(out PocketUmbrella umbrella) {
            umbrella = new PocketUmbrella(player.Position + spawnOffset, false, false, StaminaCost);
            foreach (Entity entity in Scene.Tracker.GetEntities<SeekerBarrier>()) {
                if (umbrella.Collider.Collide(entity.Collider)) {
                    return false;
                }
            }
            return true;
        }

        private bool shouldSpawnJelly() {
            return holdDelay <= 0 && player.Stamina > 20 && !player.Ducking && !wallJumpCheck(1) && !wallJumpCheck(-1) && playerStateCheck(); // && not near wall && not grounded (?)
        }

        private bool playerStateCheck() {
            int state = player.StateMachine.State;
            return state == 0 || state == 2 || state == 7;
        }

        private bool wallJumpCheck(int dir) {
            return (bool) wallJumpCheck_MI.Invoke(player, new object[] { dir });
        }

        private void dropUmbrella(PocketUmbrella umbrella) {
            Instance.holdDelay = Instance.maxHoldDelay;
            Instance.gliderDestroyed_FI.SetValue(umbrella, true);
            umbrella.Collidable = false;
            umbrella.Hold.Active = false;
            umbrella.Speed *= 1 / 3;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            umbrella.Add(new Coroutine((System.Collections.IEnumerator) Instance.coroutine_MI.Invoke(umbrella, new object[] { })));
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
