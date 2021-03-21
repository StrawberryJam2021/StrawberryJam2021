using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

        [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {

        bool HasSet = false;
        int NumberOfDashes = 0;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes");
        }

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            Player player = (Player)self.Entity;
            if (player.Dashes > 0) {
                return Player.NormalHairColor;
            }
            else {
                return orig(self, 1);
            }
        }

        private static Color modPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (self.Dashes > 0) {
                return Player.NormalHairColor;

            } else {
                return orig(self);
            }
               
        }
        public static void Load() {
            using (new DetourContext { After = { "*" } }) { 
                On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
                On.Celeste.Player.GetCurrentTrailColor += modPlayerGetTrailColor;
            }
        }
        public static void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor -= modPlayerGetTrailColor;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (!HasSet) {
                SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
                player.Dashes = NumberOfDashes;
                HasSet = true;
            }
        }
    }
}