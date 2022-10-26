using Monocle;
using System;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PrologueBasket")]
    public class PrologueBasket : Entity{
        private TalkComponent talk;
        private MTexture sprite;
        private Vector2 position;

        public PrologueBasket(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Depth = 2000;
            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 40), new Vector2(0f, 0f), Interact));
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
