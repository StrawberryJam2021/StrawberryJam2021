using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [Tracked]
    [CustomEntity("SJ2021/ColorGradeMask")]
    public class ColorGradeMask : Mask {
        private static List<VirtualRenderTarget> FadeBuffers = new List<VirtualRenderTarget>();

        private static BlendState BetterAlphaBlend = new BlendState {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };

        /* Special color grades:
         * - (current)
         * - (core)
        */

        public string ColorGradeFrom    = "(current)";
        public string ColorGradeTo      = "(current)";
        public float FadeFrom = 0f;
        public float FadeTo = 1f;

        public int BufferIndex;

        public ColorGradeMask(Vector2 position, float width, float height)
            : base(position, width, height) { }

        public ColorGradeMask(EntityData data, Vector2 offset) : base(data, offset) {
            ColorGradeFrom = data.Attr("colorGradeFrom", "(current)");
            ColorGradeTo = data.Attr("colorGradeTo", data.Attr("colorGrade", "(current)"));
            FadeFrom = data.Float("fadeFrom", 0f);
            FadeTo = data.Float("fadeTo", 1f);
        }

        public MTexture GetColorGrade(bool from = false) {
            var level = SceneAs<Level>();
            var name = from ? ColorGradeFrom : ColorGradeTo;

            if (name == "(current)") {
                name = from ? new DynData<Level>(level).Get<string>("lastColorGrade") : level.Session.ColorGrade;
            } else if (name == "(core)") {
                switch (level.CoreMode) {
                    case Session.CoreModes.Cold: name = "cold"; break;
                    case Session.CoreModes.Hot: name = "hot"; break;
                    case Session.CoreModes.None: name = "none"; break;
                }
            }

            return GFX.ColorGrades.GetOrDefault(name, GFX.ColorGrades["none"]);
        }


        public static void Load() {
            IL.Celeste.Level.Render += Level_Render;
        }

        public static void UnLoad() {
            IL.Celeste.Level.Render -= Level_Render;
        }

        private static void Level_Render(ILContext il) {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdnull(),
                instr => instr.MatchCallOrCallvirt<GraphicsDevice>("SetRenderTarget"))) {

                Logger.Log("FlushelineCollab/ColorGradeMask", $"Adding color grade fade mask rendering at {cursor.Index} in IL for Level.Render");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(level => {
                    var masks = level.Tracker.GetEntities<ColorGradeMask>().OfType<ColorGradeMask>().Where(mask => mask.Fade == FadeType.Custom).ToList();
                    if (FadeBuffers.Count > masks.Count) {
                        for (var i = masks.Count; i < FadeBuffers.Count; i++)
                            FadeBuffers[i].Dispose();
                        FadeBuffers.RemoveRange(masks.Count, FadeBuffers.Count - masks.Count);
                    } else {
                        for (var i = FadeBuffers.Count; i < masks.Count; i++)
                            FadeBuffers.Add(VirtualContent.CreateRenderTarget($"colorgrademaskfade{i}", 320, 180));
                    }
                    if (masks.Count > 0) {
                        var renderTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();

                        Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, level.Camera.Matrix);
                        for (var i = 0; i < masks.Count; i++) {
                            var mask = masks[i];
                            mask.BufferIndex = i;

                            Engine.Graphics.GraphicsDevice.SetRenderTarget(FadeBuffers[i]);
                            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                            mask.DrawFadeMask();

                            Engine.Graphics.GraphicsDevice.BlendState = Mask.DestinationAlphaBlend;
                            foreach (var slice in mask.GetMaskSlices())
                                Draw.SpriteBatch.Draw(GameplayBuffers.Level, slice.Position, slice.Source, Color.White);

                            Engine.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                        }
                        Draw.SpriteBatch.End();

                        Engine.Graphics.GraphicsDevice.SetRenderTargets(renderTargets);
                    }
                });
            }

            int matrixLocal = -1;
            cursor.TryGotoNext(instr => instr.MatchLdcR4(6),
                instr => instr.MatchCall<Matrix>("CreateScale"),
                instr => instr.MatchLdsfld<Engine>("ScreenMatrix"),
                instr => true,
                instr => instr.MatchStloc(out matrixLocal));

            if (matrixLocal == -1) {
                Logger.Log("FlushelineCollab/ColorGradeMask", $"Failed to find local variable 'matrix' in Level.Render - Color Grade Mask disabled");
                return;
            }

            if (cursor.TryGotoNext(MoveType.Before, 
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Level>("Pathfinder"))) {

                Logger.Log("FlushelineCollab/ColorGradeMask", $"Adding color grade mask rendering at {cursor.Index} in IL for Level.Render");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_S, (byte)matrixLocal);
                cursor.EmitDelegate<Action<Level, Matrix>>((level, matrix) => {
                    var colorGradeMasks = level.Tracker.GetEntities<ColorGradeMask>();
                    if (colorGradeMasks.Count > 0) {
                        var levelData = new DynData<Level>(level);
                        var currentFrom = GFX.ColorGrades.GetOrDefault(levelData.Get<string>("lastColorGrade"), GFX.ColorGrades["none"]);
                        var currentTo = GFX.ColorGrades.GetOrDefault(level.Session.ColorGrade, GFX.ColorGrades["none"]);
                        var currentValue = ColorGrade.Percent;

                        var screenSize = new Vector2(320f, 180f);
                        var scaledScreen = screenSize / level.ZoomTarget;
                        var focusOffset = (level.ZoomTarget != 1f) ? ((level.ZoomFocusPoint - scaledScreen / 2f) / (screenSize - scaledScreen) * screenSize) : Vector2.Zero;
                        var paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                        var scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);

                        var zoomMatrix = Matrix.CreateTranslation(new Vector3(-focusOffset, 0f))
                                       * Matrix.CreateScale(scale)
                                       * Matrix.CreateTranslation(new Vector3(focusOffset + paddingOffset, 0f));

                        if (SaveData.Instance.Assists.MirrorMode) {
                            zoomMatrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(new Vector3(320f, 0f, 0f));
                        }

                        var fadeMasks = colorGradeMasks.Where(mask => (mask as ColorGradeMask).Fade == FadeType.Custom);
                        var batchMasks = colorGradeMasks.Where(mask => (mask as ColorGradeMask).Fade != FadeType.Custom);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, level.Camera.Matrix * zoomMatrix * matrix);
                        foreach (ColorGradeMask mask in colorGradeMasks) {
                            var from = mask.GetColorGrade(from: true);
                            var to = mask.GetColorGrade(from: false);

                            if (mask.Fade != FadeType.Custom) {
                                foreach (var slice in mask.GetMaskSlices()) {
                                    var value = Calc.Clamp(mask.FadeFrom + (slice.Value * (mask.FadeTo - mask.FadeFrom)), 0f, 1f);
                                    if (value < 1f) {
                                        ColorGrade.Set(from, to, value);
                                    } else {
                                        ColorGrade.Set(to);
                                    }

                                    Draw.SpriteBatch.Draw(GameplayBuffers.Level, slice.Position, slice.Source, Color.White);
                                }
                            } else {
                                ColorGrade.Set(from, to, mask.FadeFrom);
                                foreach (var slice in mask.GetMaskSlices())
                                    Draw.SpriteBatch.Draw(GameplayBuffers.Level, slice.Position, slice.Source, Color.White);
                                ColorGrade.Set(from, to, mask.FadeTo);
                                Engine.Graphics.GraphicsDevice.BlendState = BetterAlphaBlend;
                                Draw.SpriteBatch.Draw(FadeBuffers[mask.BufferIndex], level.Camera.Position, Color.White);
                                Engine.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                            }
                        }
                        Draw.SpriteBatch.End();

                        ColorGrade.Set(currentFrom, currentTo, currentValue);
                    }
                });
            }
        }
    }
}
