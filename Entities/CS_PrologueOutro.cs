using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CS_PrologueOutro")]
    public class CS_PrologueOutro : CutsceneEntity {

        private Player player;
        private PrologueBasket basket;
        private Sprite logo;
        private float logoOpacity;
        private float logoSize;
        private int state; // TEMP, allows me to restart the cutscene when it finishes

        public CS_PrologueOutro(Player player, PrologueBasket basket) {
            this.player = player;
            this.basket = basket;
            logo = new Sprite(GFX.Gui, "StrawberryJam2021/logo/logo");
            logo.Add("idle", "", 0.07f);
            logo.Play("idle");
            logo.Visible = false;
            logoOpacity = 1f; // TEMP set to 1f so that the sprite is at least visible
            logoSize = 1f; // TEMP set to 1f so that the sprite is at least visible
        }

        public override void Render() {
            logo.Texture.DrawCentered(logo.Position, Color.White * logoOpacity, logoSize); 
            // I'm copying from shatter cassette and this bullshit does not work
            // I have NO idea why but this draws the sprite way too large and pixelated despite being taken from GUI Atlas
            // If someone could please explain to me how to render digital art vs pixel art I would be so happy
            // Also it would be nice if there was a function that draws relative to the screen not the level, once again, copying stuff from BreathingMinigame just didn't work
        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }

        private IEnumerator Cutscene(Level level) {
            state = player.StateMachine.State; // TEMP
            player.StateMachine.State = 11;
            player.Dashes = 1;
            yield return 0.5f;
            yield return player.DummyWalkTo(basket.X - 12f);
            yield return 0.4f;
            player.Facing = Facings.Right;
            yield return 0.3f;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("sitDown");
            yield return 2f;
            yield return PanCamera(level);
            logo.Position = level.Camera.Position; // If possible get rid of this line
            logo.Visible = true;
            LogoEaseIn(); // don't know if should be yield return, should people be able to confirm before the logo is fully formed?
            while (!Input.MenuConfirm.Pressed) {
                yield return null;
            }
            logo.Visible = false;
            EndCutscene(level);
        }

        private IEnumerator LogoEaseIn() {
            // This should just set an alpha value for the logo, and also increase the size, idk why but it's not working properly
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 3) {
                logoOpacity = Ease.CubeOut(p);
                logoSize = (Ease.CubeOut(p) / 2) + 0.5f;
                yield return null;
            }
        }

        private IEnumerator PanCamera(Level level) {
            float from = level.Camera.Position.Y;
            float to = from - 5000f;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 4) {
                level.Camera.Position = new Vector2(level.Camera.Position.X, from + (to - from) * Ease.CubeInOut(p));
                yield return null;
            }
        }

        public override void OnEnd(Level level) {
            player.StateMachine.State = state; // TEMP
            //level.CompleteArea(false, false, true);
        }
    }
}
