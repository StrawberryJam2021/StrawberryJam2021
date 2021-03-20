using Celeste.Mod.CavernHelper;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {
        bool HasSet = false;
        int NumberOfDashes = 0;
        Player player;
        Color OneDashColor;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes");
        }
        public override void Update() {
            base.Update();

            if(HasSet && player.Dashes > 0) {
                On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
            }
            else if(HasSet) { 
                On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            }
        }
        private Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            return OneDashColor;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            player = base.Scene.Tracker.GetEntity<Player>();
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (!HasSet) {
                OneDashColor = player.Hair.GetHairColor(1);
                SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
                player.Dashes = NumberOfDashes;
                HasSet = true;
            }
        }
    }
}