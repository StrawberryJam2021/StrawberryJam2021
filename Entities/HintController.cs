using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/HintController")]
    public class HintController : Entity {
        public string DialogId { get; }
        public bool SingleUse { get; }

        private static string FlagForRoom(string roomId) => $"HintController:{roomId}";

        private static bool showingHint;

        public HintController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            DialogId = data.Attr("dialogId");
            SingleUse = data.Bool("singleUse");
            Tag = Tags.PauseUpdate;
        }

        public static void Load() {
            Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
            IL.Celeste.Textbox.Render += Textbox_Render_Update;
            IL.Celeste.Textbox.Update += Textbox_Render_Update;
        }

        public static void Unload() {
            Everest.Events.Level.OnCreatePauseMenuButtons -= Level_OnCreatePauseMenuButtons;
            IL.Celeste.Textbox.Render -= Textbox_Render_Update;
            IL.Celeste.Textbox.Update -= Textbox_Render_Update;
        }

        private static void Textbox_Render_Update(ILContext il) {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                instr => instr.MatchLdloc(0),
                instr => instr.MatchCallvirt<Level>("get_FrozenOrPaused"));

            // force update and render if showing hint
            cursor.EmitDelegate<Func<bool, bool>>(fop => !showingHint && fop);
        }

        private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal) {
            int retryIndex = menu.GetItems().FindIndex(item =>
                item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button) item).Label == Dialog.Clean("menu_pause_retry"));

            if (retryIndex < 0) return;
            var hintController = level.Entities.FindFirst<HintController>();

            if (hintController != null) {
                menu.Insert(retryIndex + 1, new TextMenu.Button(Dialog.Clean("sj2021_menu_hint")) {
                    OnPressed = () => {
                        menu.OnCancel();
                        hintController.ShowHint();
                    },
                    Disabled = hintController.SingleUse && level.Session.GetFlag(FlagForRoom(level.Session.Level)),
                });
            }
        }

        private void ShowHint() {
            if (Scene is Level level)
                level.Paused = true;
            Add(new Coroutine(ShowHintSequence()));
        }

        private IEnumerator ShowHintSequence() {
            showingHint = true;
            yield return Textbox.Say(DialogId);
            showingHint = false;

            if (Scene is Level level)
                level.Paused = false;
        }
    }
}