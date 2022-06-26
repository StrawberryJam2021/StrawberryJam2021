using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.MaxHelpingHand.Effects;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [Tracked]
    [CustomEntity("SJ2021/StylegroundMask")]
    public class StylegroundMask : Mask {
        public string[] RenderTags = new string[] { };
        public bool Foreground = false;
        public bool EntityRenderer = false;
        public bool BehindForeground = false;
        
        public float AlphaFrom;
        public float AlphaTo;

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

            if (!Foreground && (scene as Level).Foreground.Backdrops.Any(backdrop =>
                backdrop is HeatWave wave and not HeatWaveNoColorGrade &&
                RenderTags.Any(tag => wave.Tags.Contains(StylegroundMaskRenderer.TagPrefix + tag)))) {

                scene.Add(new ColorGradeMask(Position, Width, Height) {
                    Fade = Fade, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    ColorGradeTo = "(core)",
                });
            }
        }

        public override void Render() {
            base.Render();
            if (EntityRenderer) {
                var bufferDict = StylegroundMaskRenderer.GetBuffers(Foreground);
                foreach (var tag in RenderTags) {
                    if (bufferDict.TryGetValue(tag, out var buffer)) {
                        foreach (var slice in GetMaskSlices()) {
                            Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(AlphaFrom, AlphaTo));
                        }
                    }
                }
            }
        }
    }

    public class StylegroundMaskRenderer : Renderer {
        public const string TagPrefix = "sjstylemask_";
        public const string DynDataRendererName = "SJ21_StylegroundMaskRenderer";

        /// <summary>
        /// used internally to render consumed stylegrounds
        /// </summary>
        private static BackdropRenderer DummyBackdropRenderer = new();

        public static Dictionary<string, VirtualRenderTarget> BgBuffers = new();
        public static Dictionary<string, VirtualRenderTarget> FgBuffers = new();

        public bool Foreground;
        public bool Behind;

        // tag -> backdrops
        public Dictionary<string, List<Backdrop>> FGBackdrops = new();
        public Dictionary<string, List<Backdrop>> BGBackdrops = new();

        public Dictionary<string, List<StylegroundMask>> Masks = new();

        public static Dictionary<string, VirtualRenderTarget> GetBuffers(bool foreground) => foreground ? FgBuffers : BgBuffers;

        public Dictionary<string, List<Backdrop>> GetBackdrops(bool foreground) => foreground ? FGBackdrops : BGBackdrops;

        public List<StylegroundMask> GetMasksWithTag(Level level, string tag) {
            if (Masks.TryGetValue(tag, out var cachedMasks)) {
                return cachedMasks;
            }

            var masks = level.Tracker.GetEntities<StylegroundMask>()
                                 .Where(m => (m as StylegroundMask).RenderTags.Contains(tag))
                                 .Cast<StylegroundMask>()
                                 .ToList();

            return Masks[tag] = masks;
        }

        public static VirtualRenderTarget GetBuffer(string tag, bool foreground) {
            var buffers = GetBuffers(foreground);

            if (!buffers.ContainsKey(tag))
                buffers.Add(tag, VirtualContent.CreateRenderTarget(tag, 320, 180));

            return buffers[tag];
        }

        private static bool TagIsMaskTag(string tag) => tag.StartsWith(TagPrefix);

        private static string StripTagPrefix(string tag) => tag.Substring(TagPrefix.Length);

        private static void AddBackdrop(string tag, Backdrop backdrop, Dictionary<string, List<Backdrop>> into) {
            if (!into.ContainsKey(tag)) {
                into.Add(tag, new());
            }

            into[tag].Insert(0, backdrop);
        }

        public static StylegroundMaskRenderer GetRendererInLevel(Level level) => new DynData<Level>(level).Get<StylegroundMaskRenderer>(DynDataRendererName);

        private void ConsumeStylegroundsFrom(List<Backdrop> from, Dictionary<string, List<Backdrop>> into) {
            // reversed loop to allow removing items while iterating
            for (int i = from.Count - 1; i >= 0; i--) {
                var backdrop = from[i];

                foreach (var tag in backdrop.Tags) {
                    if (TagIsMaskTag(tag)) {
                        AddBackdrop(StripTagPrefix(tag), backdrop, into);
                        backdrop.Renderer = DummyBackdropRenderer;
                        from.RemoveAt(i);
                    }
                }
            }
        }

        public void ConsumeStylegrounds(Level level) {
            ConsumeStylegroundsFrom(level.Foreground.Backdrops, FGBackdrops);
            ConsumeStylegroundsFrom(level.Background.Backdrops, BGBackdrops);
        }

        public static bool IsEntityInView(Level level, Entity entity) {
            Camera camera = level.Camera;
            return new Rectangle((int)entity.X, (int) entity.Y, (int) entity.Width, (int) entity.Height)
                   .Intersects(new Rectangle((int) camera.X, (int)camera.Y, 320, 180));
        }

        public bool AnyMaskIsInView(Level level, string tag) {
            foreach (var mask in GetMasksWithTag(level, tag)) {
                if (IsEntityInView(level, mask))
                    return true;
            }

            return false;
        }

        public void RenderStylegroundsIntoBuffers(Level level, bool foreground) {
            foreach (var pair in GetBackdrops(foreground)) {
                string tag = pair.Key;
                var backdrops = pair.Value;

                if (AnyMaskIsInView(level, tag)) {
                    // since masked stylegrounds are not in the level's styleground renderers at all,
                    // we need to go through the whole update-render cycle here
                    DummyBackdropRenderer.Backdrops = backdrops;
                    if (!level.Paused)
                        DummyBackdropRenderer.Update(level);
                    DummyBackdropRenderer.BeforeRender(level);

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GetBuffer(tag, foreground));
                    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                    DummyBackdropRenderer.Render(level);
                }
            }
        }

        public void RenderWith(Scene scene, bool fg, bool behind = false) {
            Foreground = fg;
            Behind = behind;
            Render(scene);
        }

        public override void Render(Scene scene) {
            var level = scene as Level;

            var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
            RenderStylegroundsIntoBuffers(level, Foreground);
            Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);

            var bufferDict = GetBuffers(Foreground);
            if (bufferDict.Count == 0)
                return;

            var masks = scene.Tracker.GetEntities<StylegroundMask>().Cast<StylegroundMask>()
                .Where(mask => !mask.EntityRenderer && (!Foreground || mask.BehindForeground == Behind) && mask.IsVisible());
            var fadeMasks = masks.Where(mask => mask.Fade == Mask.FadeType.Custom);
            var batchMasks = masks.Where(mask => mask.Fade != Mask.FadeType.Custom);

            if (fadeMasks.Any()) {
                var targets = Engine.Graphics.GraphicsDevice.GetRenderTargets();

                Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                foreach (var mask in fadeMasks) {
                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                    mask.DrawFadeMask();

                    Engine.Graphics.GraphicsDevice.BlendState = Mask.DestinationAlphaBlend;
                    foreach (var tag in mask.RenderTags) {
                        if (bufferDict.TryGetValue(tag, out var buffer)) {
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

            if (batchMasks.Any()) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                foreach (var mask in batchMasks) {
                    foreach (var tag in mask.RenderTags) {
                        if (bufferDict.TryGetValue(tag, out var buffer)) {
                            foreach (var slice in mask.GetMaskSlices()) {
                                Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(mask.AlphaFrom, mask.AlphaTo));
                            }
                        }
                    }
                }
                Draw.SpriteBatch.End();
            }
        }

        #region Hooks
        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            IL.Celeste.Level.Render += Level_Render;
            IL.Celeste.DisplacementRenderer.BeforeRender += DisplacementRenderer_BeforeRender;
            On.Celeste.HeatWave.Update += HeatWave_Update;
        }

        public static void UnLoad() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            IL.Celeste.Level.Render -= Level_Render;
            IL.Celeste.DisplacementRenderer.BeforeRender -= DisplacementRenderer_BeforeRender;
            On.Celeste.HeatWave.Update -= HeatWave_Update;
        }

        private static void HeatWave_Update(On.Celeste.HeatWave.orig_Update orig, HeatWave self, Scene scene) {
            if (self.Tags.Any(tag => tag.StartsWith(TagPrefix))) {
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
                renderer.ConsumeStylegrounds(self);
            }

            GetRendererInLevel(self).Masks.Clear();
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
                    GetRendererInLevel(level)?.RenderWith(level, false);
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
                    GetRendererInLevel(level)?.RenderWith(level, true, true);
                });

                cursor.Index += 4;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>((level) => {
                    GetRendererInLevel(level)?.RenderWith(level, true, false);
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
                            var tags = heatWave.Tags;
                            if (tags.Any(tag => tag.StartsWith(TagPrefix))) {
                                baseRendering = tags.Contains("nomaskhide");
                                if (new DynData<HeatWave>(heatWave).Get<float>("heat") > 0f) {
                                    foreach (StylegroundMask mask in level.Tracker.GetEntities<StylegroundMask>()) {
                                        if (mask.RenderTags.Any(tag => heatWave.Tags.Contains(TagPrefix + tag))) {
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
        #endregion
    }
}
