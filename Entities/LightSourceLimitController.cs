using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/LightSourceLimitController")]
    public class LightSourceLimitController : Entity {
        public const int VanillaLightLimit = 64;
        public const int VanillaVertexCount = 11520;
        public const int VanillaResultVertexCount = 384;
        public const int VanillaRenderTargetBufferSize = 1024;
        public const int VanillaLightsPerChannel = VanillaLightLimit / 4;
        public const int VanillaTextureSplit = 4;
        public const float VanillaMatrixScale = 0.0009765625f; // 1 / 1024
        public const float Radius = 128f;

        private static bool controllerInMap;

        public LightSourceLimitController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
        }

        public static void Load() {
            IL.Celeste.GameplayBuffers.Create += GameplayBuffersCreateHook;
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
            IL.Celeste.LightingRenderer.ctor += LightingRendererCtorHook;
            IL.Celeste.LightingRenderer.BeforeRender += LightingRendererBeforeRenderHook;
            IL.Celeste.LightingRenderer.ClearDirtyLights += GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLightGradients += GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLightOccluders += GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLight += LightingRendererDrawLightHook;
            On.Celeste.LightingRenderer.GetCenter += LightingRendererGetCenterHook;
            On.Celeste.LightingRenderer.GetMask += LightingRendererGetMaskHook;
        }

        public static void Unload() {
            IL.Celeste.GameplayBuffers.Create -= GameplayBuffersCreateHook;
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
            IL.Celeste.LightingRenderer.ctor -= LightingRendererCtorHook;
            IL.Celeste.LightingRenderer.BeforeRender -= LightingRendererBeforeRenderHook;
            IL.Celeste.LightingRenderer.ClearDirtyLights -= GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLightGradients -= GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLightOccluders -= GeneralLightLimitOverrideHook;
            IL.Celeste.LightingRenderer.DrawLight -= LightingRendererDrawLightHook;
            On.Celeste.LightingRenderer.GetCenter -= LightingRendererGetCenterHook;
            On.Celeste.LightingRenderer.GetMask -= LightingRendererGetMaskHook;
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            MapData mapData = self.session.MapData;
            controllerInMap = mapData.Levels.Any(l => l.Entities.Any(e => e.Name == "SJ2021/LightSourceLimitController"));
            orig(self);
        }

        #region Hooks
        private static void GameplayBuffersCreateHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaRenderTargetBufferSize))) {
                cursor.EmitDelegate<Func<int, int>>(GetLightBufferSize);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaRenderTargetBufferSize))) {
                cursor.EmitDelegate<Func<int, int>>(GetLightBufferSize);
            }
        }

        private static void LightingRendererCtorHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaVertexCount))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaResultVertexCount))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaVertexCount))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaLightLimit))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }
        }

        private static void LightingRendererBeforeRenderHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaLightLimit))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }

            cursor.Index = 0;
            
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(VanillaMatrixScale))) {
                cursor.EmitDelegate<Func<float, float>>(GetMatrixScalingFactor);
            }
        }

        private static void GeneralLightLimitOverrideHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(VanillaLightLimit))) {
                cursor.EmitDelegate<Func<int, int>>(GetArraySize);
            }
        }

        private static void LightingRendererDrawLightHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(VanillaRenderTargetBufferSize))) {
                cursor.EmitDelegate<Func<float, float>>(GetLightTextureSize);
            }
        }

        // Change appropriate constants for changes in light count and texture size
        private static Vector3 LightingRendererGetCenterHook(On.Celeste.LightingRenderer.orig_GetCenter orig, LightingRenderer self, int index) {
            if (controllerInMap) {
                int num = index % (VanillaLightsPerChannel * 4);
                return new Vector3(Radius * ((num % (VanillaTextureSplit * 2)) + 0.5f) * 2f, Radius * ((num / (VanillaTextureSplit * 2)) + 0.5f) * 2f, 0f);
            }
            return orig(self, index);
        }

        private static Color LightingRendererGetMaskHook(On.Celeste.LightingRenderer.orig_GetMask orig, LightingRenderer self, int index, float maskOn, float maskOff) {
            if (controllerInMap) {
                int num = index / (VanillaLightsPerChannel * 4);
                return new Color((num == 0) ? maskOn : maskOff, (num == 1) ? maskOn : maskOff, (num == 2) ? maskOn : maskOff, (num == 3) ? maskOn : maskOff);
            }
            return orig(self, index, maskOn, maskOff);
        }

        #endregion

        // Double our lighting render target size from 1024x1024 to 2048x2048
        private static int GetLightBufferSize(int orig) {
            if (controllerInMap) {
                return orig * 2;
            }
            return orig;
        }

        // Since we doubled both dimensions of the lighting texture, our light source count/associated arrays increases by 4x
        private static int GetArraySize(int orig) {
            if (controllerInMap) {
                return orig * 4;
            }
            return orig;
        }

        // Scaling matrix is 1/1024, patch to 1/2048
        private static float GetMatrixScalingFactor(float orig) {
            if (controllerInMap) {
                return orig / 2f;
            }
            return orig;
        }

        // Double our lighting texture size from 1024x1024 to 2048x2048
        private static float GetLightTextureSize(float orig) {
            if (controllerInMap) {
                return orig * 2f;
            }
            return orig;
        }
    }
}
