using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/HoldableDashTrigger")]
    [Tracked]
    public class HoldableDashTrigger : Trigger {
        public Modes Mode;

        public const string DashWithHoldableFlag = "sj2021_canDashWithHoldable";
        
        public HoldableDashTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            Mode = data.Enum("mode", Modes.EnableOnStay);
        }

        public enum Modes {
            EnableOnStay,
            DisableOnStay,
            EnableToggle,
            DisableToggle
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            Session session = SceneAs<Level>().Session;
            if (Mode == Modes.EnableToggle) {
                session.SetFlag(DashWithHoldableFlag, setTo: true);
            } else if (Mode == Modes.DisableToggle) {
                session.SetFlag(DashWithHoldableFlag, setTo: false);
            }
        }

        public static bool CanDashWithHoldable(Player player) {
            // may not have consistent behavior when overlapping multiple types at once
            // but I'm not concerned since there's no real reason to do that
            HoldableDashTrigger trigger = player.CollideFirst<HoldableDashTrigger>();
            if (trigger != null) {
                if (trigger.Mode == Modes.EnableOnStay) {
                    return true;
                } else if (trigger.Mode == Modes.DisableOnStay) {
                    return false;
                }
            }
            return player.SceneAs<Level>().Session.GetFlag(DashWithHoldableFlag);
        }

        public static void Load() {
            On.Celeste.Player.NormalUpdate += On_Player_NormalUpdate;
        }

        public static void Unload() {
            On.Celeste.Player.NormalUpdate -= On_Player_NormalUpdate;
        }

        private static int On_Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
            int origResult = orig(self);
            if (self.Holding != null && CanDashWithHoldable(self) && self.CanDash) {
                self.Speed += DynamicData.For(self).Get<Vector2>("LiftBoost");
                return self.StartDash();
            } else {
                return origResult;
            }
        }
    }
}
