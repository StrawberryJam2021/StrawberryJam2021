using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/ExpiringDashRefill")]
    public class ExpiringDashRefill : Refill {
        private static readonly MethodInfo RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo respawnTimer = typeof(Refill).GetField("respawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        // Config stuff
        private readonly float dashExpirationTime;
        private static readonly float hairFlashTime = 0.2f;

        // Tracking
        private static double timeUntilDashExpire = 0;

        public ExpiringDashRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse")) {
            dashExpirationTime = Calc.Max(data.Float("dashExpirationTime"), 0.01f);
            //hairFlashTime = dashExpirationTime * Calc.Clamp(data.Float("hairFlashThreshold"), 0f, 1f);

            Remove(Components.Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(Player player) {
            // Unconditionally add the dash, bypassing inventory limits
            player.Dashes = 1;
            timeUntilDashExpire = dashExpirationTime;

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
            On.Celeste.Player.Update += update;
        }
        public static void Unload() {
            On.Celeste.Player.UpdateHair -= UpdateHair;
        }

        public static void UpdateHair(On.Celeste.Player.orig_UpdateHair orig, Player player, bool applyGravity) {
            if (player.Scene.Tracker.GetEntity<ExpiringDashRefill>() is ExpiringDashRefill refill) {
                if (flash) {
                    player.OverrideHairColor = Player.UsedHairColor;
                }
            }

            orig.Invoke(player, applyGravity);
        }

        public static void update(On.Celeste.Player.orig_Update orig, Player self) {

            orig.Invoke(self);

            self.OverrideHairColor = null;

            if (self.Dashes == 0)
                return;

            if (timeUntilDashExpire <= 0)
                return;

            timeUntilDashExpire -= Engine.DeltaTime;

            if (timeUntilDashExpire <= 0) {
                // Remove given dash
                self.Dashes = 0;
                flash = false;

                return;
            }

            if (timeUntilDashExpire <= hairFlashTime) {
                // Flash hair
                if (self.Scene.OnInterval(0.05f))
                    flash = !flash;
            }
        }

        public override void Update() {
            base.Update();

            if (Scene.Tracker.GetEntity<Player>() is not Player player)
                return;

            player.OverrideHairColor = null;

            if (player.Dashes == 0)
                return;

            if (timeUntilDashExpire <= 0)
                return;

            timeUntilDashExpire -= Engine.DeltaTime;

            if (timeUntilDashExpire <= 0) {
                // Remove given dash
                player.Dashes -= 1;
                flash = false;

                return;
            }

            if (timeUntilDashExpire <= hairFlashTime) {
                // Flash hair
                if (Scene.OnInterval(0.05f))
                    flash = !flash;
            }
        }

        public override void Removed(Scene scene) {
            if (scene.Tracker.GetEntity<Player>() is not Player player)
                return;

            // Make sure the player can't carry their dash out the room and keep it.
            player.Dashes = 0;


            // Make sure hair colour overrides are removed, in case player leaves while the hair is flashing blue.
            player.OverrideHairColor = null;

            base.Removed(scene);
        }
    }
}
