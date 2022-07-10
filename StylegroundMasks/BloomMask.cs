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
    [CustomEntity("SJ2021/BloomMask")]
    public class BloomMask : Mask {

        private static VirtualRenderTarget BloomBuffer;

        public float BaseFrom;
        public float BaseTo;
        public float StrengthFrom;
        public float StrengthTo;

        public BloomMask(Vector2 position, float width, float height)
            : base(position, width, height) { }

        public BloomMask(EntityData data, Vector2 offset) : base(data, offset) {
            BaseFrom = data.Float("baseFrom", -1f);
            BaseTo = data.Float("baseTo", -1f);
            StrengthFrom = data.Float("strengthFrom", -1f);
            StrengthTo = data.Float("strengthTo", -1f);
        }
        public static void Load() {
            On.Celeste.GameplayBuffers.Create += GameplayBuffers_Create;
            On.Celeste.BloomRenderer.Apply += BloomRenderer_Apply;
            IL.Celeste.BloomRenderer.Apply += BloomRenderer_ApplyIL;
        }

        public static void UnLoad() {
            On.Celeste.GameplayBuffers.Create -= GameplayBuffers_Create;
            On.Celeste.BloomRenderer.Apply -= BloomRenderer_Apply;
            IL.Celeste.BloomRenderer.Apply -= BloomRenderer_ApplyIL;
        }

        private static void GameplayBuffers_Create(On.Celeste.GameplayBuffers.orig_Create orig) {
            orig();
            BloomBuffer?.Dispose();
            BloomBuffer = VirtualContent.CreateRenderTarget("bloomMask-buffer", 320, 180);
        }

        private static void BloomRenderer_Apply(On.Celeste.BloomRenderer.orig_Apply orig, BloomRenderer self, VirtualRenderTarget target, Scene scene) {
            new DynData<BloomRenderer>(self).Set("bloomMaskLastStrength", self.Strength);
            if (scene.Tracker.GetEntity<BloomMask>() != null)
                self.Strength = 1f;
            orig(self, target, scene);
        }

        private static void BloomRenderer_ApplyIL(ILContext il) {
            var cursor = new ILCursor(il);

            var textureLoc = 0;
            if (!cursor.TryGotoNext(
                instr => instr.MatchCall(typeof(GaussianBlur), "Blur"),
                instr => instr.MatchStloc(out textureLoc))) {

                Logger.Log("SJ2021/BloomMask", $"Failed to find local variable 'texture' in BloomRenderer.Apply - Bloom Mask disabled");
                return;
            }

            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(-10f),
                instr => instr.MatchLdcR4(-10f))) {

                if (cursor.TryGotoPrev(MoveType.AfterLabel,
                    instr => instr.MatchCall(typeof(Draw), "get_SpriteBatch"))) {

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.Emit(OpCodes.Ldloc_S, (byte)textureLoc);
                    cursor.EmitDelegate<Action<BloomRenderer, VirtualRenderTarget, Scene, Texture2D>>((self, target, scene, texture) => {
                        var selfData = new DynData<BloomRenderer>(self);
                        var sliceRects = new List<Rectangle>();
                        var renderedMask = false;
                        var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
                        var bloomMaskLastStrength = selfData.Get<float>("bloomMaskLastStrength");
                        foreach (BloomMask entity in scene.Tracker.GetEntities<BloomMask>()) {
                            var level = scene as Level;
                            renderedMask = true;

                            var baseFrom = (entity.BaseFrom >= 0f ? entity.BaseFrom : self.Base);
                            var baseTo = (entity.BaseTo >= 0f ? entity.BaseTo : self.Base);

                            var strengthFrom = (entity.StrengthFrom >= 0f ? entity.StrengthFrom : bloomMaskLastStrength);
                            var strengthTo = (entity.StrengthTo >= 0f ? entity.StrengthTo : bloomMaskLastStrength);

                            var slices = entity.GetMaskSlices();

                            Engine.Instance.GraphicsDevice.SetRenderTarget(BloomBuffer);
                            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, level.Camera.Matrix);
                            Draw.SpriteBatch.Draw(GameplayBuffers.TempA, Vector2.Transform(Vector2.Zero, -level.Camera.Matrix), Color.White);
                            foreach (var slice in slices) {
                                Draw.Rect(slice.Position.X, slice.Position.Y, slice.Source.Width, slice.Source.Height, Color.White * slice.GetValue(baseFrom, baseTo));
                            }
                            Draw.SpriteBatch.End();
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BloomRenderer.BlurredScreenToMask);
                            Draw.SpriteBatch.Draw(texture, Vector2.Zero, Color.White);
                            Draw.SpriteBatch.End();

                            Engine.Instance.GraphicsDevice.SetRenderTarget(target);
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BloomRenderer.AdditiveMaskToScreen, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, level.Camera.Matrix);
                            foreach (var slice in slices) {
                                var strength = slice.GetValue(strengthFrom, strengthTo);
                                for (int i = 0; i < strength; i++) {
                                    var scale = (i < strength - 1f) ? 1f : (strength - i);
                                    Draw.SpriteBatch.Draw(BloomBuffer, slice.Position, slice.Source, Color.White * scale);
                                }
                                sliceRects.Add(new Rectangle((int)slice.Position.X, (int)slice.Position.Y, slice.Source.Width, slice.Source.Height));
                            }
                            Draw.SpriteBatch.End();
                        }
                        if (renderedMask)
                            Engine.Instance.GraphicsDevice.SetRenderTargets(lastTargets);

                        selfData.Set("bloomMaskRects", sliceRects);
                    });
                }
            }

            if (cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchCall<Engine>("get_Instance"),
                instr => instr.MatchCallvirt<Game>("get_GraphicsDevice"),
                instr => instr.MatchLdarg(1))) {

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate<Action<BloomRenderer, Scene>>((self, scene) => {
                    var selfData = new DynData<BloomRenderer>(self);
                    var slices = selfData.Get<List<Rectangle>>("bloomMaskRects");
                    if (slices.Count > 0) {
                        //Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, (scene as Level).Camera.Matrix);
                        foreach (var slice in slices) {
                            Draw.Rect(slice, Color.Transparent);
                        }
                        Draw.SpriteBatch.End();
                    }
                    self.Strength = selfData.Get<float>("bloomMaskLastStrength");
                });
            }
        }
    }
}
