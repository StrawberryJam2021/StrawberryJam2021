using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/HintController")]
    public class HintController : Entity {
        public string DialogId1 { get; }
        public string DialogId2 { get; }
        public bool SingleUse1 { get; }
        public bool SingleUse2 { get; }
        public string ToggleFlag { get; }

        public string DialogId => ToggleFlagValue ? DialogId2 : DialogId1;
        public bool SingleUse => ToggleFlagValue ? SingleUse2 : SingleUse1;

        private string UsedFlag => $"HintControllerUsed:{(Engine.Scene as Level)?.Session.Level ?? string.Empty}:{ToggleFlagValue}";
        
        private bool ToggleFlagValue => (Engine.Scene as Level)?.Session.GetFlag(ToggleFlag) ?? false;
        private bool UsedFlagValue => (Engine.Scene as Level)?.Session.GetFlag(UsedFlag) ?? false;
        
        private static bool showingHint;

        public HintController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            DialogId1 = data.Attr("dialogId1");
            DialogId2 = data.Attr("dialogId2");
            SingleUse1 = data.Bool("singleUse1");
            SingleUse2 = data.Bool("singleUse2");
            ToggleFlag = data.Attr("toggleFlag");
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
            Logger.Log("SJ2021/HintController", $"Adding IL hook for {il.Method.Name}");
            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Level>("get_FrozenOrPaused"))) {
                // force update and render if showing hint
                cursor.EmitDelegate<Func<bool, bool>>(fop => !showingHint && fop);
            } else {
                Logger.Log("SJ2021/HintController", "Failed to add IL hook!");
            }
        }

        private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal) {
            int retryIndex = menu.GetItems().FindIndex(item =>
                item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button) item).Label == Dialog.Clean("menu_pause_retry"));

            if (retryIndex < 0) {
                return;
            }

            var hintController = level.Entities.FindFirst<HintController>();

            if (hintController != null) {
                menu.Insert(retryIndex + 1, new TextMenu.Button(Dialog.Clean("sj2021_menu_hint")) {
                    OnPressed = () => {
                        menu.OnCancel();
                        hintController.ShowHint();
                    },
                    Disabled = hintController.SingleUse && hintController.UsedFlagValue,
                });
            }
        }

        private void ShowHint() {
            if (Scene is Level level) {
                level.Paused = true;
                level.Session.SetFlag(UsedFlag);
            }

            Add(new Coroutine(ShowHintSequence()));
        }

        private IEnumerator ShowHintSequence() {
            showingHint = true;
            yield return Textbox.Say(DialogId);
            showingHint = false;

            if (Scene is Level level) {
                level.Paused = false;
            }
        }
    }
}