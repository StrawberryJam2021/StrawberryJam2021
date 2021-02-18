using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("StrawberryJam2021/DashSequenceController")]
    public class DashSequenceController : Entity {
        public DashSequenceController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            DashCode = data.Attr("dashCode", "*").ToUpper().Split(',');
            FlagLabel = data.Attr("flagLabel", "");

            //stole this code from max480's helping hand set flag on spawn trigger hope u don't mind
            Level level = null;
            if (Engine.Scene is Level) {
                level = Engine.Scene as Level;
            } else if (Engine.Scene is LevelLoader) {
                level = (Engine.Scene as LevelLoader).Level;
            }

            if (level != null) {
                for (int i = 1; i <= DashCode.Length; i++) {
                    level.Session.SetFlag(FlagLabel + "-" + i, false);
                }
            }
            Add(DashListener = new DashListener());
            DashListener.OnDash = OnDash;
            CodePosition = 0;
        }

        private void OnDash(Vector2 direction) {
            string input = "";

            if (direction.Y < 0f) {
                input = "U";
            } else if (direction.Y > 0f) {
                input = "D";
            }
            if (direction.X < 0f) {
                input += "L";
            } else if (direction.X > 0f) {
                input += "R";
            }

            if (input == DashCode[CodePosition] || DashCode[CodePosition] == "*") {
                SceneAs<Level>().Session.SetFlag(FlagLabel + "-" + (CodePosition + 1), true);
                CodePosition++;
                if (CodePosition >= DashCode.Length) {
                    RemoveSelf();
                }
            } else {
                for (int i = 1; i <= DashCode.Length; i++) {
                    SceneAs<Level>().Session.SetFlag(FlagLabel + "-" + i, false);
                }
                CodePosition = 0;
            }
        }

        public string[] DashCode;

        public string FlagLabel;

        private DashListener DashListener;

        private int CodePosition;
    }
}
