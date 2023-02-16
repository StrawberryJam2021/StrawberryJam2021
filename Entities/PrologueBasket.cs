using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Cutscenes;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PrologueBasket")]
    public class PrologueBasket : Entity {
        private TalkComponent talk;
        private MTexture sprite;
        private Vector2 position;

        public PrologueBasket(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Depth = 2000;
            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 40), new Vector2(0f, -8f), Interact));
            talk.PlayerMustBeFacing = false;
            position = data.Position + offset;
            sprite = GFX.Game["objects/StrawberryJam2021/prologueBasket/basket"];
        }

        public override void Render() {
            base.Render();
            sprite.DrawCentered(position);
        }

        private void Interact(Player player) {
            Scene.Add(new CS_PrologueOutro(player, this));
        }
    }
}
