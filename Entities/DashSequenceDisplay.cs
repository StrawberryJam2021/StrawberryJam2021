using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    // This entity is added/removed from the scene automatically.
    [Tracked]
    public class DashSequenceDisplay : Entity {
        private float drawlerp;
        private readonly MTexture bg = GFX.Gui["strawberryCountBG"];
        private readonly MTexture arrow = GFX.Gui["dotarrow_outline"];

        private readonly SortedList<int, string[]> dashCodes = new SortedList<int, string[]>();

        public DashSequenceDisplay() {
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
            Depth = -100;
            Y = 96f;
        }

        public void AddDashCode(int index, string[] dashCode) {
            System.Console.WriteLine(index + " " + dashCode.Aggregate((a, b) => $"{a},{b}"));
            if (!dashCodes.ContainsKey(index))
                dashCodes.Add(index, dashCode);
        }

        public override void Update() {
            Level level = SceneAs<Level>();

            float y = level.strawberriesDisplay.DrawLerp > 0 ? 192 : 96;
            if (!level.TimerHidden) {
                if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter)
                    y += 58f;
                else if (Settings.Instance.SpeedrunClock == SpeedrunType.File)
                    y += 78f;
            }
            Y = Calc.Approach(Y, y, Engine.DeltaTime * 800f);

            drawlerp = Calc.Approach(drawlerp, true ? 1 : 0, Engine.DeltaTime * 2);
            base.Update();
        }

        public void InitializeDashCodes() {
            dashCodes.Clear();
            foreach (DashSequenceController controller in Scene.Tracker.GetEntities<DashSequenceController>()) {
                dashCodes[controller.Index] = controller.DashCode;
            }
            System.Console.WriteLine(dashCodes.Count);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            InitializeDashCodes();
        }

        public override void Render() {
            if (!(drawlerp <= 0f)) {
                Vector2 pos = Vector2.Lerp(new Vector2(-bg.Width, Y), Vector2.UnitY * Y, Ease.CubeOut(drawlerp));
                pos = pos.Round();
                bg.DrawJustified(pos, new Vector2(0f, 0.5f), Color.White, Vector2.One * 1.25f);

                Vector2 arrowOffset = pos + Vector2.UnitX * (arrow.Width / 2);
                arrow.DrawCentered(arrowOffset);
            }
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Level.TransitionRoutine += Level_TransitionRoutine;
        }

        internal static void Unload() {
            On.Celeste.Level.TransitionRoutine -= Level_TransitionRoutine;
        }

        private static IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction) {
            yield return new SwapImmediately(orig(self, next, direction));

            DashSequenceDisplay display = self.Tracker.GetEntity<DashSequenceDisplay>();
            if (display != null)
                display.InitializeDashCodes();
        }

        #endregion
    }
}