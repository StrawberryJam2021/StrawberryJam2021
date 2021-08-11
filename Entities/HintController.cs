using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/HintController")]
    public class HintController : Entity {
        public string DialogId { get; }
        public bool SingleUse { get; }

        private static string FlagForRoom(string roomId) => $"HintController:{roomId}";

        private bool needsHint;

        public HintController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            DialogId = data.Attr("dialogId");
            SingleUse = data.Bool("singleUse");
        }

        public void DisplayHint() {
            if (Scene is not Level level) return;
            level.Session.SetFlag(FlagForRoom(level.Session.Level));

            if (!string.IsNullOrEmpty(DialogId)) {
                var player = level.Entities.FindFirst<Player>();
                Scene.Add(new DialogCutscene(DialogId, player, false));
            }
        }

        public override void Update() {
            base.Update();

            if (needsHint) {
                needsHint = false;
                DisplayHint();
            }
        }

        public static void Load() {
            Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
        }

        public static void Unload() {
            Everest.Events.Level.OnCreatePauseMenuButtons -= Level_OnCreatePauseMenuButtons;
        }

        private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal) {
            int retryIndex = menu.GetItems().FindIndex(item =>
                item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button) item).Label == Dialog.Clean("menu_pause_retry"));

            if (retryIndex < 0) return;
            var hintController = level.Entities.FindFirst<HintController>();

            if (hintController != null) {
                menu.Insert(retryIndex + 1, new TextMenu.Button(Dialog.Clean("sj2021_menu_hint")) {
                    OnPressed = () => {
                        hintController.needsHint = true;
                        menu.OnCancel();
                    },
                    Disabled = hintController.SingleUse && level.Session.GetFlag(FlagForRoom(level.Session.Level)),
                });
            }
        }
    }
}