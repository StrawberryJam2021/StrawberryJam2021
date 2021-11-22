using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [Tracked]
    public class LightingMask : Mask {
        private static List<VirtualRenderTarget> FadeBuffers = new List<VirtualRenderTarget>();

        public static BlendState SubtractAlpha = new BlendState {
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.ReverseSubtract
        };

        public static BlendState InvertAlpha = new BlendState {
            ColorSourceBlend = Blend.InverseDestinationAlpha,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Subtract
        };

        public static BlendState DestinationTransparencySubtractAlpha = new BlendState {
            ColorSourceBlend = Blend.InverseSourceAlpha,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };

        public float LightingFrom;
        public float LightingTo;
        public bool AddBase;

        public int BufferIndex;

        public LightingMask(Vector2 position, float width, float height)
            : base(position, width, height) { }

        public LightingMask(EntityData data, Vector2 offset) : base(data, offset) {
            LightingFrom = data.Float("lightingFrom", -1f);
            LightingTo = data.Float("lightingTo", 0f);
            AddBase = data.Bool("addBase", true);
        }


        public static void Load() {
            On.Celeste.LightingRenderer.Render += LightingRenderer_Render;
        }

        public static void UnLoad() {
            On.Celeste.LightingRenderer.Render -= LightingRenderer_Render;
        }

        private static void LightingRenderer_Render(On.Celeste.LightingRenderer.orig_Render orig, LightingRenderer self, Scene scene) {
            var lightingMasks = scene.Tracker.GetEntities<LightingMask>();
            if (scene is Level level && lightingMasks.Count > 0) {
                var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
                var lightingRects = new List<Rectangle>();

                var fadeMasks = lightingMasks.OfType<LightingMask>().Where(mask => mask.Fade == FadeType.Custom).ToArray();
                if (FadeBuffers.Count > fadeMasks.Length) {
                    for (var i = fadeMasks.Length; i < FadeBuffers.Count; i++)
                        FadeBuffers[i].Dispose();
                    FadeBuffers.RemoveRange(fadeMasks.Length, FadeBuffers.Count - fadeMasks.Length);
                } else {
                    for (var i = FadeBuffers.Count; i < fadeMasks.Length; i++)
                        FadeBuffers.Add(VirtualContent.CreateRenderTarget($"lightingmaskfade{i}", 320, 180));
                }
                if (fadeMasks.Length > 0) {
                    Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                    for (var i = 0; i < fadeMasks.Length; i++) {
                        var mask = fadeMasks[i];
                        mask.BufferIndex = i;

                        var lightingTo = (mask.LightingTo >= 0f ? mask.LightingTo : level.Session.LightingAlphaAdd);
                        var lightingFrom = (mask.LightingFrom >= 0f ? mask.LightingFrom : level.Session.LightingAlphaAdd);

                        lightingTo = MathHelper.Clamp((mask.AddBase ? level.BaseLightingAlpha : 0f) + lightingTo, 0f, 1f);
                        lightingFrom = MathHelper.Clamp((mask.AddBase ? level.BaseLightingAlpha : 0f) + lightingFrom, 0f, 1f);

                        var inverted = lightingTo > lightingFrom;
                        var baseAlpha = inverted ? (1f - lightingFrom) : lightingFrom;
                        var subAlpha = lightingFrom - lightingTo;

                        Engine.Graphics.GraphicsDevice.SetRenderTarget(FadeBuffers[i]);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                        Engine.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                        foreach (var slice in mask.GetMaskSlices())
                            Draw.SpriteBatch.Draw(GameplayBuffers.Light, slice.Position, slice.Source, Color.White * baseAlpha);
                        //  Draw.Rect(slice.Position, slice.Source.Width, slice.Source.Height, Color.White * baseAlpha);
                        Engine.Graphics.GraphicsDevice.BlendState = SubtractAlpha;
                        mask.DrawFadeMask(Color.White * subAlpha);
                        if (inverted) {
                            Engine.Graphics.GraphicsDevice.BlendState = InvertAlpha;
                            foreach (var slice in mask.GetMaskSlices())
                                Draw.SpriteBatch.Draw(GameplayBuffers.Light, slice.Position, slice.Source, Color.White);
                        }
                    }
                    Draw.SpriteBatch.End();
                    Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);
                }

                GFX.FxDither.CurrentTechnique = GFX.FxDither.Techniques["InvertDither"];
                GFX.FxDither.Parameters["size"].SetValue(new Vector2(GameplayBuffers.Light.Width, GameplayBuffers.Light.Height));
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, GFX.DestinationTransparencySubtract, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, GFX.FxDither, level.Camera.Matrix);

                foreach (LightingMask mask in lightingMasks) {
                    var lightingTo = (mask.LightingTo >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingTo) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);
                    var lightingFrom = (mask.LightingFrom >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingFrom) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);

                    foreach (var slice in mask.GetMaskSlices()) {
                        var lighting = MathHelper.Clamp(slice.GetValue(lightingFrom, lightingTo), 0f, 1f);
                        if (mask.Fade != FadeType.Custom)
                            Draw.SpriteBatch.Draw(GameplayBuffers.Light, slice.Position, slice.Source, Color.White * lighting);
                        else
                            Draw.SpriteBatch.Draw(FadeBuffers[mask.BufferIndex], slice.Position, slice.Source, Color.White);
                        lightingRects.Add(new Rectangle((int)slice.Position.X, (int)slice.Position.Y, slice.Source.Width, slice.Source.Height));
                    }
                }

                Draw.SpriteBatch.End();

                Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Light);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                foreach (var rect in lightingRects) {
                    Draw.Rect(rect, Color.White);
                }
                Draw.SpriteBatch.End();
                Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);
            }
            orig(self, scene);
        }
    }
}
