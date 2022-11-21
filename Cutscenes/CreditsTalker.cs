using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    [Tracked]
    [CustomEntity("SJ2021/CreditsTalker")]
    public class CreditsTalker : Entity {
        public CreditsTalker(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            TalkComponent.HoverDisplay display = new() {
                InputPosition = new Vector2(0f, -75f),
                Texture = GFX.Gui["SJ2021/Lobbies/credits_talker"]
            };
            Add(new TalkComponent(new Rectangle(0, 0, data.Width, data.Height), data.Nodes[0] + offset - Position, OnTalk, display) {
                PlayerMustBeFacing = false
            });
        }

        private void OnTalk(Player player) {
            Scene.Add(new CS_Credits(fromHeartside: false));
        }
    }
}
