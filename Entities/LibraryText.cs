using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/LibraryText")]
    public class LibraryText : Entity {

        private string[] text;
        private bool outline;

        public LibraryText(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Tag = Tags.HUD;
            text = Dialog.Clean(data.Attr("dialog", "app_ending")).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            outline = data.Bool("outline");
        }

        public override void Render() {
            Vector2 cam = ((Level) Scene).Camera.Position;
            Vector2 pos = (Position - cam) * 6f;

            if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                pos.X = 1920f - pos.X;

            pos.Y -= (text.Length - 1) * ActiveFont.LineHeight / 2;

            foreach(string line in text) {
                if (outline)
                    ActiveFont.DrawOutline(line, pos, new Vector2(0.5f, 0.5f), Vector2.One * 1.25f, Color.White, 2f, Color.Black);
                else
                    ActiveFont.Draw(line, pos, new Vector2(0.5f, 0.5f), Vector2.One * 1.25f, Color.White);
                pos.Y += ActiveFont.LineHeight;
            }
        }

    }
}
