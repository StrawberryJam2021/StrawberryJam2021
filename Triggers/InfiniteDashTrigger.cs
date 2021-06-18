using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/InfiniteDashTrigger")]
    public class InfiniteDashTrigger : Trigger{
        public InfiniteDashTrigger(EntityData data, Vector2 offset)
           : base(data, offset) {
            
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            SceneAs<Level>().Session.Inventory.Dashes = player.Dashes = 2;

        }
        public override void OnStay(Player player) {
            base.OnStay(player);
            player.RefillDash();
        }
        public override void OnLeave(Player player) {
            base.OnLeave(player);
            SceneAs<Level>().Session.Inventory.Dashes = player.Dashes = 1;
     
        }
    }
}
