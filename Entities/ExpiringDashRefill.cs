using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    // At the moment this entity is heavily based around the assumption that the player can only hold at-most one ER at any given moment
    [Tracked]
    [CustomEntity("SJ2021/ExpiringDashRefill")]
    public class ExpiringDashRefill : Refill {
        // Config stuff
        private readonly float dashExpirationTime;
        private readonly float hairFlashTime;

        private static StrawberryJam2021Session session => StrawberryJam2021Module.Session;

        public ExpiringDashRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse")) {
            dashExpirationTime = Calc.Max(data.Float("dashExpirationTime"), 0.01f);
            hairFlashTime = dashExpirationTime * Calc.Clamp(data.Float("hairFlashThreshold"), 0f, 1f);

            Remove(Components.Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayerEDR));
        }

        private void OnPlayerEDR(Player player) {
            // The dash shouldn't be picked up if the ExpiringDash the player holds would last longer
            // If the player's in stamina panic, this rule is ignored
            if (session.ExpiringDashRemainingTime >= dashExpirationTime && player.Stamina >= 20f)
                return;

            int playerRealDashes = player.Dashes - (session.ExpiringDashRemainingTime > 0 ? 1 : 0);

            // Unconditionally add the dash, bypassing inventory limits
            expireFlash = false;
            player.Dashes = playerRealDashes + 1;
            session.ExpiringDashRemainingTime = dashExpirationTime;
            session.ExpiringDashFlashThreshold = hairFlashTime;

            player.RefillStamina();

            // Everything after this line is roundabout ways of doing the same things Refill does
            Audio.Play("event:/game/general/diamond_touch");

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine(RefillRoutine(player)));
            respawnTimer = 2.5f;
        }

        private static bool expireFlash;

        public static void Load() {
            On.Celeste.Player.Update += Update;
            On.Celeste.Player.Die += OnPlayerDeath;
            On.Celeste.Player.OnTransition += OnTransition;
            On.Celeste.Player.DashBegin += OnDashBegin;

            On.Celeste.PlayerHair.GetHairColor += GetHairColor;
        }
        public static void Unload() {
            On.Celeste.Player.Update -= Update;
            On.Celeste.Player.Die -= OnPlayerDeath;
            On.Celeste.Player.OnTransition -= OnTransition;
            On.Celeste.Player.DashBegin -= OnDashBegin;

            On.Celeste.PlayerHair.GetHairColor -= GetHairColor;
        }

        public static Color GetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index)
            => expireFlash ? Player.FlashHairColor : orig.Invoke(self, index);

        public static void OnDashBegin(On.Celeste.Player.orig_DashBegin orig, Player player) {
            // The expiring dash should get used first
            session.ExpiringDashRemainingTime = 0;
            expireFlash = false;

            orig.Invoke(player);
        }

        public static PlayerDeadBody OnPlayerDeath(On.Celeste.Player.orig_Die orig, Player player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
            if (evenIfInvincible || !SaveData.Instance.Assists.Invincible) {
                expireFlash = false;
                session.ExpiringDashRemainingTime = 0;
            }

            return orig.Invoke(player, direction, evenIfInvincible, registerDeathInStats);
        }

        public static void OnTransition(On.Celeste.Player.orig_OnTransition orig, Player player) {
            // We first remove the expiring dash if the player still has one
            if (session.ExpiringDashRemainingTime > 0) {
                player.Dashes--;
                expireFlash = false;

                session.ExpiringDashRemainingTime = 0;
            }

            // We invoke this after, just to make sure the default recharge behavior still applies if applicable.
            orig.Invoke(player);
        }

        public static void Update(On.Celeste.Player.orig_Update orig, Player self) {
            // If touching the ground would've replenished the dash if the ExpiringDash wasn't there, remove the timer
            if (!self.Inventory.NoRefills && self.OnGround() && self.Dashes <= self.MaxDashes) {
                session.ExpiringDashRemainingTime = 0;
                expireFlash = false;
            }

            if (session.ExpiringDashRemainingTime <= 0) {
                orig.Invoke(self);
                return;
            }

            session.ExpiringDashRemainingTime -= Engine.DeltaTime;

            if (session.ExpiringDashRemainingTime <= 0) {
                // Remove given dash
                self.Dashes--;
                expireFlash = false;
                orig.Invoke(self);
                return;
            }

            if (session.ExpiringDashRemainingTime <= session.ExpiringDashFlashThreshold) {
                // Flash hair
                if (self.Scene.OnInterval(0.05f))
                    expireFlash = !expireFlash;
            }

            orig.Invoke(self);
        }
    }
}
