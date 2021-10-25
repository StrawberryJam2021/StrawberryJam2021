﻿using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    // This entity is added/removed from the scene automatically.
    [Tracked]
    public class DashSequenceDisplay : Entity {
        private static readonly Color correctA = Color.LimeGreen;
        private static readonly Color correctB = Calc.HexToColor("1bd1bc"); // cyan

        private float drawlerp;
        private readonly MTexture bg = GFX.Gui["strawberryCountBG"];

        private readonly SortedList<int, MTexture[]> dashCodes = new SortedList<int, MTexture[]>();

        public int Index;
        private float lengthTarget, lengthPrev, lengthLerp;
        private MTexture[] currentCodeArrows;
        private KeyValuePair<int, MTexture[]>? nextCode;
        private float[] currentCodeArrowsAnim;
        private float animThreshold;

        private int codePosition;
        private readonly Wiggler wiggler;

        public DashSequenceDisplay() {
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
            Depth = -100;
            Y = 96f;
            Add(wiggler = Wiggler.Create(0.5f, 2f));
            Add(new Coroutine(ChangingDashCodeRoutine()));
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
            lengthLerp = Calc.Approach(lengthLerp, 1, Engine.DeltaTime * 1.5f);

            if (currentCodeArrowsAnim != null)
                for (int i = 0; i < currentCodeArrowsAnim.Length; i++)
                    currentCodeArrowsAnim[i] = Calc.Approach(currentCodeArrowsAnim[i], i < animThreshold * currentCodeArrowsAnim.Length ? 1f : 0f, Engine.DeltaTime * 2.5f);

            base.Update();
        }

        public void InitializeDashCodes(Level level) {
            dashCodes.Clear();
            DashSequenceController[] controllers = level.Tracker.GetEntities<DashSequenceController>().Cast<DashSequenceController>().ToArray();
            if (controllers.Length != 0) {
                foreach (DashSequenceController controller in controllers)
                    dashCodes[controller.Index] = controller.DashCode.Select(v => GFX.Gui[$"controls/directions/{(int) v.X}x{(int) v.Y}"]).ToArray();

                if (dashCodes.Count != 0)
                    nextCode = dashCodes.First();
            } else
                nextCode = null;
        }

        public void ValidateInput() {
            codePosition++;
            wiggler.Start();
            if (codePosition >= currentCodeArrows.Length) {
                KeyValuePair<int, MTexture[]> next = dashCodes.FirstOrDefault(pair => pair.Key > Index);
                if (next.Value != null)
                    nextCode = next;
                else
                    nextCode = null;
            }
        }

        public void Fail() {
            codePosition = 0;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            InitializeDashCodes(SceneAs<Level>());
        }

        public override void Render() {
            if (!(drawlerp <= 0f)) {
                Vector2 pos = Vector2.Lerp(new Vector2(-bg.Width, Y), Vector2.UnitY * Y, Ease.CubeOut(drawlerp));
                pos = pos.Round();

                float l = lengthPrev + (lengthTarget - lengthPrev) * Ease.CubeOut(lengthLerp);
                Vector2 bgPos = pos + Vector2.UnitX * (32 + (l - 7.5f) * 48);
                if (bgPos.X > 0f)
                    bg.DrawJustified(pos, new Vector2(0f, 0.5f), Color.White, new Vector2((bgPos.X - pos.X + 64) / bg.Width, 1.25f));
                bg.DrawJustified(bgPos, new Vector2(0f, 0.5f), Color.White, Vector2.One * 1.25f);

                if (currentCodeArrows != null) {
                    Vector2 arrowOffset = pos + Vector2.UnitX * 32;
                    for (int i = 0; i < currentCodeArrows.Length; i++) {
                        Color c = Color.White;
                        float s = Ease.ExpoInOut(currentCodeArrowsAnim[i]) * 0.75f, r = 0;
                        if (i < codePosition) {

                            c = Settings.Instance.DisableFlashes ? correctA : Color.Lerp(correctA, correctB, Calc.SineMap(Scene.RawTimeActive * 20f - i / 2f, 0f, 1f));
                        }
                        if (i + 1 == codePosition) {
                            s += wiggler.Value * 0.25f;
                            r = wiggler.Value * 0.25f;
                        }
                        currentCodeArrows[i].DrawCentered(arrowOffset + Vector2.UnitX * (i * 48), c, s, r);
                    }
                }
            }
        }

        private IEnumerator ChangingDashCodeRoutine() {
            while (true) {
                while (this.nextCode?.Value == currentCodeArrows)
                    yield return null;

                KeyValuePair<int, MTexture[]> nextCode = this.nextCode ?? default;

                int count = -1;
                if (nextCode.Value != null)
                    count = nextCode.Value.Length;
                else if (currentCodeArrows != null)
                    yield return 0.5f;

                bool changedLength = false;
                if (currentCodeArrows != null) {
                    while (animThreshold > 0f) {
                        animThreshold = Calc.Approach(animThreshold, 0f, Engine.DeltaTime * 4f);
                        yield return null;
                    }

                    lengthPrev = lengthTarget;
                    lengthLerp = 0f;
                    lengthTarget = count;
                    changedLength = true;
                    yield return nextCode.Value == null ? 1f : 0.3f;
                }

                if (nextCode.Value != null) {
                    if (!changedLength) {
                        lengthPrev = lengthTarget;
                        lengthLerp = 0f;
                        lengthTarget = count;
                    }
                    Index = nextCode.Key;
                    currentCodeArrows = nextCode.Value;
                    currentCodeArrowsAnim = new float[count];
                    codePosition = 0;

                    animThreshold = 0f;
                    while (animThreshold < 1f) {
                        animThreshold = Calc.Approach(animThreshold, 1f, Engine.DeltaTime * 4f);
                        yield return null;
                    }
                }
            }
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Level.TransitionRoutine += Level_TransitionRoutine;
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        internal static void Unload() {
            On.Celeste.Level.TransitionRoutine -= Level_TransitionRoutine;
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction) {
            yield return new SwapImmediately(orig(self, next, direction));

            DashSequenceDisplay display = self.Tracker.GetEntity<DashSequenceDisplay>();
            if (display != null)
                display.InitializeDashCodes(self);
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            orig(self);
            if (self.Level.Tracker.GetEntity<DashSequenceDisplay>() == null)
                self.Level.Add(new DashSequenceDisplay());
        }

        #endregion
    }
}