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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.MaxHelpingHand.Entities;
using Celeste.Mod.MaxHelpingHand.Effects;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [Tracked]
    public class StylegroundMask : Mask {
        public static Dictionary<string, VirtualRenderTarget> BgBuffers = new Dictionary<string, VirtualRenderTarget>();
        public static Dictionary<string, VirtualRenderTarget> FgBuffers = new Dictionary<string, VirtualRenderTarget>();
        public string[] RenderTags = new string[] { };
        public bool Foreground = false;
        public bool EntityRenderer = false;
        public bool BehindForeground = false;
        
        public float AlphaFrom;
        public float AlphaTo;

        private ColorGradeMask coreModeGrading;

        public static readonly string DynDataRendererName = "SJ21_StylegroundMaskRenderer";

        public StylegroundMask(Vector2 position, float width, float height)
            : base(position, width, height) {

            Depth = 2000000;
        }

        public StylegroundMask(EntityData data, Vector2 offset) : base(data, offset) {
            Depth = 2000000;
            RenderTags = data.Attr("tag").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            EntityRenderer = data.Bool("entityRenderer");
            BehindForeground = data.Bool("behindFg");
            AlphaFrom = data.Float("alphaFrom", 0f);
            AlphaTo = data.Float("alphaTo", 1f);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (EntityRenderer && !Foreground) {
                scene.Add(new StylegroundMask(Position, Width, Height) {
                    Depth = -2000000,
                    Foreground = true,
                    Fade = Fade, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    RenderTags = RenderTags,
                    EntityRenderer = EntityRenderer,
                    BehindForeground = BehindForeground,
                    AlphaFrom = AlphaFrom,
                    AlphaTo = AlphaTo
                });
            }

            HeatWave heatWave;
            if ((heatWave = (scene as Level).Foreground.GetEach<HeatWave>().FirstOrDefault(current => RenderTags.Any(tag => GetTags(current).Contains($"stylemask_{tag}")) &&
                (current.GetType() != typeof(HeatWaveNoColorGrade)))) != null && !Foreground) {

                scene.Add(coreModeGrading = new ColorGradeMask(Position, Width, Height) {
                    Fade = Fade, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    ColorGradeTo = "(core)",
                });
            }
        }

        public override void Render() {
            base.Render();
            if (EntityRenderer) {
                var bufferDict = Foreground ? FgBuffers : BgBuffers;
                foreach (var tag in RenderTags) {
                    if (bufferDict.ContainsKey(tag)) {
                        var buffer = bufferDict[tag];
                        foreach (var slice in GetMaskSlices()) {
                            Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(AlphaFrom, AlphaTo));
                        }
                    }
                }
            }
        }


        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.BackdropRenderer.Render += BackdropRenderer_Render;
            IL.Celeste.Level.Render += Level_Render;
            IL.Celeste.DisplacementRenderer.BeforeRender += DisplacementRenderer_BeforeRender;
            On.Celeste.HeatWave.Update += HeatWave_Update;
        }

        public static void UnLoad() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.BackdropRenderer.Render -= BackdropRenderer_Render;
            IL.Celeste.Level.Render -= Level_Render;
            IL.Celeste.DisplacementRenderer.BeforeRender -= DisplacementRenderer_BeforeRender;
            On.Celeste.HeatWave.Update -= HeatWave_Update;
        }

        private static void HeatWave_Update(On.Celeste.HeatWave.orig_Update orig, HeatWave self, Scene scene) {
            if (GetTags(self).Any(tag => tag.StartsWith("stylemask_"))) {
                var levelData = new DynData<Level>(scene as Level);
                var lastColorGrade = levelData.Get<string>("lastColorGrade");
                var colorGradeEase = levelData.Get<float>("colorGradeEase");
                var colorGradeEaseSpeed = levelData.Get<float>("colorGradeEaseSpeed");
                var colorGrade = (scene as Level).Session.ColorGrade;
                orig(self, scene);
                levelData.Set("lastColorGrade", lastColorGrade);
                levelData.Set("colorGradeEase", colorGradeEase);
                levelData.Set("colorGradeEaseSpeed", colorGradeEaseSpeed);
                (scene as Level).Session.ColorGrade = colorGrade;
            } else {
                orig(self, scene);
            }
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            if (isFromLoader) {
                var renderer = new StylegroundMaskRenderer();
                new DynData<Level>(self).Set(DynDataRendererName, renderer);
                self.Add(renderer);
            }
        }

        private static void BackdropRenderer_Render(On.Celeste.BackdropRenderer.orig_Render orig, BackdropRenderer self, Scene scene) {
            if (!(scene is Level level)) {
                orig(self, scene);
                return;
            }

            var levelData = new DynData<Level>(level);

            if (self != level.Background && self != level.Foreground) {
                orig(self, scene);
                return;
            }

            var bufferDict = (self == level.Foreground) ? FgBuffers : BgBuffers;

            var lastVisible = new Dictionary<Backdrop, bool>();
            var renderedKeys = new HashSet<string>();

            foreach (var backdrop in self.Backdrops) {
                lastVisible[backdrop] = backdrop.Visible;
                foreach (var tag in GetTags(backdrop)) {
                    if (tag.StartsWith("stylemask_")) {
                        var key = tag.Substring(10);
                        if (!bufferDict.ContainsKey(key))
                            bufferDict.Add(key, VirtualContent.CreateRenderTarget(tag, 320, 180));
                        renderedKeys.Add(key);
                    }
                }
            }
            foreach (var i in bufferDict.Keys.Where((key) => !renderedKeys.Contains(key)).ToArray()) {
                bufferDict[i].Dispose();
                bufferDict.Remove(i);
            }

            EnableTag(self, "", lastVisible);
            orig(self, scene);
            var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
            foreach (var pair in bufferDict) {
                var tag = pair.Key;
                var buffer = pair.Value;
                EnableTag(self, tag, lastVisible);
                Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                orig(self, scene);
            }
            Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);

            foreach (var pair in lastVisible)
                pair.Key.Visible = pair.Value;
        }

        private static void Level_Render(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Level>("Background"),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchCallvirt<Renderer>("Render"))) {
                Logger.Log("SJ2021/StylegroundMask", $"Adding background styleground mask render call at {cursor.Index} in IL for Level.Render");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>((level) => {
                    new DynData<Level>(level).Get<StylegroundMaskRenderer>(DynDataRendererName)?.RenderWith(level, false);
                });
            }

            cursor.Index = 0;

            while (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Level>("Foreground"),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchCallvirt<Renderer>("Render"))) {
                Logger.Log("SJ2021/StylegroundMask", $"Adding foreground styleground mask render behind call at {cursor.Index} in IL for Level.Render");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>((level) => {
                    new DynData<Level>(level).Get<StylegroundMaskRenderer>(DynDataRendererName)?.RenderWith(level, true, true);
                });

                cursor.Index += 4;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>((level) => {
                    new DynData<Level>(level).Get<StylegroundMaskRenderer>(DynDataRendererName)?.RenderWith(level, true, false);
                });
            }
        }

        private static void DisplacementRenderer_BeforeRender(ILContext il) {
            var cursor = new ILCursor(il);

            int heatWaveLoc = -1;
            int levelArg = -1;
            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdloc(out heatWaveLoc),
                instr => instr.MatchLdarg(out levelArg),
                instr => instr.MatchIsinst<Level>(),
                instr => instr.MatchCallvirt<HeatWave>("RenderDisplacement"))) {

                ILLabel breakLabel = null;
                if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchBrfalse(out breakLabel))) {
                    Logger.Log("SJ2021/StylegroundMask", $"Masking heat wave displacement rendering at {cursor.Index} in IL for DisplacementRenderer.BeforeRender");

                    cursor.Emit(OpCodes.Ldarg, levelArg);
                    cursor.Emit(OpCodes.Isinst, typeof(Level));
                    cursor.EmitDelegate<Func<Level, bool>>(level => {
                        var baseRendering = true;
                        foreach (var heatWave in level.Foreground.GetEach<HeatWave>()) {
                            var tags = GetTags(heatWave);
                            if (tags.Any(tag => tag.StartsWith("stylemask_"))) {
                                baseRendering = tags.Contains("nomaskhide");
                                if (new DynData<HeatWave>(heatWave).Get<float>("heat") > 0f) {
                                    foreach (StylegroundMask mask in level.Tracker.GetEntities<StylegroundMask>()) {
                                        if (mask.RenderTags.Any(tag => GetTags(heatWave).Contains($"stylemask_{tag}"))) {
                                            foreach (var slice in mask.GetMaskSlices()) {
                                                Draw.Rect(slice.Position.X, slice.Position.Y, slice.Source.Width, slice.Source.Height, new Color(0.5f, 0.5f, 0.1f, 1f));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return baseRendering;
                    });
                    cursor.Emit(OpCodes.Brfalse_S, breakLabel);
                }
            }
        }

        private static HashSet<string> GetTags(Backdrop backdrop) {
            var tags = new HashSet<string>();
            foreach (var fulltag in backdrop.Tags) {
                string[] taglist = fulltag.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in taglist)
                    tags.Add(tag);
            }
            return tags;
        }

        private static void EnableTag(BackdropRenderer renderer, string tag, Dictionary<Backdrop, bool> lastVisible) {
            foreach (var backdrop in renderer.Backdrops) {
                var tags = GetTags(backdrop);

                var foundTag = tags.Any(s => (string.IsNullOrEmpty(tag) && s.StartsWith("stylemask_")) || (!string.IsNullOrEmpty(tag) && s == $"stylemask_{tag}"));

                if (string.IsNullOrEmpty(tag))
                    foundTag = !foundTag || tags.Contains("nomaskhide");

                if (foundTag && lastVisible[backdrop])
                    backdrop.Visible = true;
                else
                    backdrop.Visible = false;
            }
        }

        public class StylegroundMaskRenderer : Renderer {
            public bool Foreground;
            public bool Behind;

            public void RenderWith(Scene scene, bool fg, bool behind = false) {
                Foreground = fg;
                Behind = behind;
                Render(scene);
            }

            public override void Render(Scene scene) {
                var bufferDict = Foreground ? FgBuffers : BgBuffers;
                if (bufferDict.Count == 0)
                    return;
                var level = scene as Level;
                var masks = scene.Tracker.GetEntities<StylegroundMask>().OfType<StylegroundMask>()
                    .Where(mask => !mask.EntityRenderer && (!Foreground || mask.BehindForeground == Behind) && mask.IsVisible());
                var fadeMasks = masks.Where(mask => mask.Fade == FadeType.Custom);
                var batchMasks = masks.Where(mask => mask.Fade != FadeType.Custom);
                if (fadeMasks.Count() > 0) {
                    var targets = Engine.Graphics.GraphicsDevice.GetRenderTargets();

                    Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                    foreach (var mask in fadeMasks) {
                        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                        mask.DrawFadeMask();

                        Engine.Graphics.GraphicsDevice.BlendState = Mask.DestinationAlphaBlend;
                        foreach (var tag in mask.RenderTags) {
                            if (bufferDict.ContainsKey(tag)) {
                                var buffer = bufferDict[tag];
                                foreach (var slice in mask.GetMaskSlices())
                                    Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White);
                            }
                        }

                        Engine.Graphics.GraphicsDevice.SetRenderTargets(targets);
                        Engine.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                        Draw.SpriteBatch.Draw(GameplayBuffers.TempA, level.Camera.Position, Color.White);
                    }
                    Draw.SpriteBatch.End();
                }
                if (batchMasks.Count() > 0) {
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                    foreach (var mask in batchMasks) {
                        foreach (var tag in mask.RenderTags) {
                            if (bufferDict.ContainsKey(tag)) {
                                var buffer = bufferDict[tag];
                                foreach (var slice in mask.GetMaskSlices()) {
                                    Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(mask.AlphaFrom, mask.AlphaTo));
                                }
                            }
                        }
                    }
                    Draw.SpriteBatch.End();
                }
            }
        }
    }
}
