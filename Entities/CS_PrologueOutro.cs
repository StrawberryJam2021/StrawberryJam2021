using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CS_PrologueOutro")]
    public class CS_PrologueOutro : CutsceneEntity {
        private Player player;
        private PrologueBasket basket;
        private int state; // TEMP, allows me to restart the cutscene when it finishes

        public CS_PrologueOutro(Player player, PrologueBasket basket) {
            this.player = player;
            this.basket = basket;
        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }

        private IEnumerator Cutscene(Level level) {
            state = player.StateMachine.State;
            player.StateMachine.State = 11;
            player.Dashes = 1;
            yield return 0.5f;
            yield return player.DummyWalkTo(basket.X - 12f);
            yield return 0.4f;
            player.Facing = Facings.Right;
            yield return 0.3f;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("sitDown");
            yield return 3f;
            yield return PanCamera(level);
            yield return level.ZoomTo(new Vector2(180f, 90f), 3f, 3f);
            yield return level.ZoomBack(1f);
            EndCutscene(level);
        }

        private IEnumerator PanCamera(Level level) {

            float from = level.Camera.Position.X;
            float to = from + 1000f;
            for (float p = 0f; p < 2f; p += Engine.DeltaTime / 3f) {
                level.Camera.Position = new Vector2(from + (to - from) * Ease.QuadIn(p), level.Camera.Position.Y);
                yield return null;
            }
        }

        public override void OnEnd(Level level) {
            player.StateMachine.State = state;
        }
    }
}
