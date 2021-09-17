using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ExpiringDashRefill")]
    public class ExpiringDashRefill : Refill {

        private static readonly MethodInfo OnPlayerMethodInfo = typeof(Refill).GetMethod("OnPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo respawnTimer = typeof(Refill).GetField("respawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly Action<Player> baseOnPlayer;

        // Config stuff
        private readonly float dashExpirationTime;
        private readonly float hairFlashThreshold;

        // Tracking
        private double timeUntilDashExpire = 0;

        public ExpiringDashRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse")) {
            baseOnPlayer = (Action<Player>) OnPlayerMethodInfo.CreateDelegate(typeof(Action<Player>), this);

            dashExpirationTime = data.Float("dashExpirationTime");
            hairFlashThreshold = data.Float("hairFlashThreshold");

            Remove(Components.Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayer));
        }

        private void OnPlayer(Player player) {
            // Unconditionally add the dash, bypassing inventory limits
            Scene.Tracker.GetEntity<Player>().Dashes = 1;
            timeUntilDashExpire = dashExpirationTime;

            // Everything after this line is roundabout ways of doing the same things Refill does
            Audio.Play("event:/game/general/diamond_touch");

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine((IEnumerator) RefillRoutine.Invoke(this, new object[] { player })));
            respawnTimer.SetValue(this, 2.5f);
        }

        private bool flash;

        public override void Update() {
            base.Update();

            Player player = Scene.Tracker.GetEntity<Player>();
            player.OverrideHairColor = Player.NormalHairColor;

            if (player.Dashes == 0)
                timeUntilDashExpire = 0;


            if (timeUntilDashExpire <= 0)
                return;

            timeUntilDashExpire -= Engine.DeltaTime;

            if (timeUntilDashExpire <= 0) {
                // Remove given dash
                player.Dashes -= 1;

                return;
            }

            if (timeUntilDashExpire <= hairFlashThreshold) {
                // Flash hair
                if (Scene.OnInterval(0.05f))
                    flash = !flash;

                player.OverrideHairColor = flash ? Player.NormalHairColor : Player.UsedHairColor;
            }
        }
    }
}
