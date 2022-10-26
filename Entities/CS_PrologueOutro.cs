using System.Collections;
using Celeste.Mod.Entities;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CS_PrologueOutro")]
    public class CS_PrologueOutro : CutsceneEntity {
        private Player player;

        public CS_PrologueOutro(Player player) {
            this.player = player;
        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }

        private IEnumerator Cutscene(Level level) {
            yield return 0.0f;
            EndCutscene(level);
        }

        public override void OnEnd(Level level) {
        }
    }
}
