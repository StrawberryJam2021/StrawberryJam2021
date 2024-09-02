using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/DashSequenceController")]
    [Tracked]
    public class DashSequenceController : Entity {
        public Vector2[] DashCode;
        public string FlagLabel;
        public string FailureFlag;
        public int Index;

        private int codePosition;

        private DashSequenceDisplay display;

        public DashSequenceController(EntityData data, Vector2 offset) 
            : base(data.Position + offset) {
            DashCode = data.Attr("dashCode", "*").ToUpper()
                .Split(',').Select(s => s switch {
                    "U" => -Vector2.UnitY,
                    "D" => Vector2.UnitY,
                    "L" => -Vector2.UnitX,
                    "R" => Vector2.UnitX,
                    "UL" => new Vector2(-1, -1),
                    "UR" => new Vector2(1, -1),
                    "DL" => new Vector2(-1, 1),
                    "DR" => new Vector2(1, 1),
                    _ => Vector2.Zero,
                }).ToArray();

            FlagLabel = data.Attr("flagLabel", "");
            FailureFlag = data.Attr("flagOnFailure", "");
            Index = data.Int("index");

            //stole this code from maddie480's helping hand set flag on spawn trigger hope u don't mind
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


        public override void Update() {
            base.Update();
            if (display == null)
                display = Scene.Tracker.GetEntity<DashSequenceDisplay>();
        }

        private void OnDash(Vector2 direction) {
            if (display != null && Index == display.Index) {
                Vector2 dir = Calc.Sign(direction);
                if (DashCode[codePosition] == Vector2.Zero || dir == DashCode[codePosition]) {
                    SceneAs<Level>().Session.SetFlag(FlagLabel + "-" + (codePosition + 1), true);
                    codePosition++;
                    display.ValidateInput();
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
                    display.Fail();
                }
            }
        }
    }
}
