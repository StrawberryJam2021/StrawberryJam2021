using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using On.Celeste;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/pinballFallCutscene")]
    class PinballFallCutscene : CutsceneEntity {
        public PinballFallCutscene() {

        }

        public override void OnBegin(Level level) {
            Add(new Coroutine(Cutscene(level)));
        }
        private IEnumerator Cutscene(Level level) {
            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
            Player player = level.Entities.FindFirst<Player>();
            Glitch.Value = .2f;
            yield return .4f;
            Glitch.Value = 0f;
            while (!player.OnGround(1)){
                yield return null;
            }
            MInput.Disabled = false;
            EndCutscene(level);

        }
        public override void Update() {
            MInput.Disabled = false;
            if (Level.CanPause && (Input.Pause.Pressed || Input.ESC.Pressed)) {
                Input.Pause.ConsumeBuffer();
                Input.ESC.ConsumeBuffer();
                Level.Pause();
            }
            MInput.Disabled = true;
            base.Update();
            
        }
        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            MInput.Disabled = false;
        }
        public override void OnEnd(Level level) {
            level.OnEndOfFrame += delegate {
                if (WasSkipped) {
                        level.Remove(this);
                        level.UnloadLevel();
                        level.EndCutscene();
                        level.Session.Level = "Tutorial";
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                        level.LoadLevel(Player.IntroTypes.None);
                        MInput.Disabled = false;
                        FallEffects.Show(visible: false);
                        level.Wipe.Cancel();
                } 
                else {
                    MInput.Disabled = false;
                    level.Remove(this);
                }
                
            };
        }

    }
}
