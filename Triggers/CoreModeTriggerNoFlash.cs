using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/CoreModeTriggerNoFlash")]
    public class CoreModeTriggerNoFlash : Trigger {

        public Session.CoreModes mode;

        public CoreModeTriggerNoFlash(EntityData data, Vector2 offset) : base(data, offset) {
            mode = data.Enum<Session.CoreModes>("mode", Session.CoreModes.None);
        }

        public override void OnEnter(Player player) {
            Level level = Scene as Level;
            if (level.CoreMode == mode) {
                return;
            }
            level.CoreMode = mode;
        }
    }
}
