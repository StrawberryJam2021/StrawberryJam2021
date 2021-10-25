using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/DashSequenceController")]
    [Tracked]
    public class DashSequenceController : Entity {
        public string[] DashCode;
        public string FlagLabel;
        public string FailureFlag;
        public int Index;

        private int codePosition;

        public DashSequenceController(EntityData data, Vector2 offset) 
            : base(data.Position + offset) {
            DashCode = data.Attr("dashCode", "*").ToUpper().Split(',');
            FlagLabel = data.Attr("flagLabel", "");
            FailureFlag = data.Attr("flagOnFailure", "");
            Index = data.Int("index");

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
                if (!string.IsNullOrEmpty(FailureFlag)) {
                    level.Session.SetFlag(FailureFlag, false);
                }
            }
            Add(new DashListener(OnDash));
            codePosition = 0;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (scene.Tracker.GetEntity<DashSequenceDisplay>() == null)
                scene.Add(new DashSequenceDisplay());
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

            if (input == DashCode[codePosition] || DashCode[codePosition] == "*") {
                SceneAs<Level>().Session.SetFlag(FlagLabel + "-" + (codePosition + 1), true);
                codePosition++;
                if (codePosition >= DashCode.Length) {
                    RemoveSelf();
                }
            } else {
                for (int i = 1; i <= DashCode.Length; i++) {
                    SceneAs<Level>().Session.SetFlag(FlagLabel + "-" + i, false);
                }
                if (codePosition != 0 && !string.IsNullOrEmpty(FailureFlag)) {
                    SceneAs<Level>().Session.SetFlag(FailureFlag, true);
                }
                codePosition = 0;
            }
        }
    }
}
