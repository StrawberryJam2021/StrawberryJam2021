using Celeste.Mod.CherryHelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class CrabPedestal : ItemCrystalPedestal {
        private readonly Vector2 node;
        private readonly string setFlag;
        
        public CrabPedestal(EntityData data, Vector2 offset) : base(data, offset) {
            node = data.FirstNodeNullable(offset) ?? Vector2.Zero;
            setFlag = data.Attr("setFlag");
            Remove(Get<ItemCrystalCollider>());
            Add(new ItemCrystalCollider(OnCollide, new Hitbox(24f, 24f, -12f, -8f)) {
                Active = true,
                Visible = true,
            });
        }
        
        public void OnCollide(ItemCrystal crystal) {
            if (crystal == null) return;
            OnHoldable(crystal);

            var level = SceneAs<Level>();
            level.Session.SetFlag(setFlag);
            if (level.CollideFirst<RumbleTrigger>(node) is { } rumbleTrigger) {
                rumbleTrigger.Invoke();
            }
        }
    }
}