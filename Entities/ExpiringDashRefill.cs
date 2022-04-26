using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Reflection;


namespace Celeste.Mod.StrawberryJam2021.Entities {
    // At the moment this entity is heavily based around these assumption :
    // 1. that the player will have the prologue inventory
    // 2. that they can only hold 1 expiring dash at any given time
    [Tracked]
    [CustomEntity("SJ2021/ExpiringDashRefill")]
    public class ExpiringDashRefill : Refill {
        private static readonly MethodInfo RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo respawnTimer = typeof(Refill).GetField("respawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        // Config stuff
        private readonly float dashExpirationTime;
        private readonly float hairFlashTime;

        private static StrawberryJam2021Session session => StrawberryJam2021Module.Session;

        public ExpiringDashRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse")) {
            dashExpirationTime = Calc.Max(data.Float("dashExpirationTime"), 0.01f);
            hairFlashTime = dashExpirationTime * Calc.Clamp(data.Float("hairFlashThreshold"), 0f, 1f);

            Remove(Components.Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(Player player) {
            // Unconditionally add the dash, bypassing inventory limits
            player.Dashes = 1;
            flash = false;
            session.ExpiringDashRemainingTime = dashExpirationTime;
            session.ExpiringDashFlashThreshold = hairFlashTime;

            // Everything after this line is roundabout ways of doing the same things Refill does
            Audio.Play("event:/game/general/diamond_touch");

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine((IEnumerator) RefillRoutine.Invoke(this, new object[] { player })));
            respawnTimer.SetValue(this, 2.5f);
        }

        private static bool flash;

        public static void Load() {
            On.Celeste.Player.UpdateHair += UpdateHair;
            On.Celeste.Player.Update += Update;
            On.Celeste.Player.Die += OnPlayerDeath;
            On.Celeste.Player.OnTransition += OnTransition;
        }
        public static void Unload() {
            On.Celeste.Player.UpdateHair -= UpdateHair;
            On.Celeste.Player.Update -= Update;
            On.Celeste.Player.Die -= OnPlayerDeath;
            On.Celeste.Player.OnTransition -= OnTransition;
        }

        private static Color? previousOverride = null;
        public static void UpdateHair(On.Celeste.Player.orig_UpdateHair orig, Player player, bool applyGravity) {
            player.OverrideHairColor = flash ? Player.UsedHairColor : previousOverride;

            orig.Invoke(player, applyGravity);
        }

        public static PlayerDeadBody OnPlayerDeath(On.Celeste.Player.orig_Die orig, Player player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
            if (evenIfInvincible || !SaveData.Instance.Assists.Invincible) {
                flash = false;
                session.ExpiringDashRemainingTime = 0;
                player.Dashes = 0;
            }

            return orig.Invoke(player, direction, evenIfInvincible, registerDeathInStats);
        }

        public static void OnTransition(On.Celeste.Player.orig_OnTransition orig, Player player) {
            // We first remove the expiring dash if the player still has one
            if (session.ExpiringDashRemainingTime > 0) {
                player.Dashes = 0;
                flash = false;

                session.ExpiringDashRemainingTime = 0;
            }

            // We invoke this after, just to make sure the default recharge behavior still applies if applicable.
            orig.Invoke(player);
        }

        public static void Update(On.Celeste.Player.orig_Update orig, Player self) {
            orig.Invoke(self);

            if (self.Dashes == 0)
                return;

            if (session.ExpiringDashRemainingTime <= 0)
                return;

            session.ExpiringDashRemainingTime -= Engine.DeltaTime;

            if (session.ExpiringDashRemainingTime <= 0) {
                // Remove given dash
                self.Dashes = 0;
                flash = false;

                return;
            }

            if (session.ExpiringDashRemainingTime <= session.ExpiringDashFlashThreshold) {
                // Flash hair
                if (self.Scene.OnInterval(0.05f)) {
                    if (!flash)
                        previousOverride = self.OverrideHairColor;
                    flash = !flash;

                }
            }
        }
    }
}
