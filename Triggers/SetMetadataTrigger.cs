using Celeste.Mod.Entities;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/SetMetadataTrigger")]
    public class SetMetadataTrigger : Trigger {

        private bool theoInBooster;

        public SetMetadataTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            theoInBooster = data.Bool("theoInBooster", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            MapMetaModeProperties meta = SceneAs<Level>().Session.MapData.GetMeta();
            meta.TheoInBubble = theoInBooster;
        }
    }
}