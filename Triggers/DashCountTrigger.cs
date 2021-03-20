using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
                return Player.UsedHairColor;
            }
        }

        private static Color modPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (self.Dashes > 0) {
                return Player.NormalHairColor;
                
            } else {
                return Player.UsedHairColor;
            }
               
        }
        public static void Load() {
            On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor += modPlayerGetTrailColor;
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