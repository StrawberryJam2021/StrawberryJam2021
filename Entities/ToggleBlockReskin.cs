using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class ToggleBlockReskin {
        private const int STAY = 8, DONE = 9;
        private static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };
        private static string pathPrefix = "objects/StrawberryJam2021/toggleIndicator/";
        private static MTexture[] allTextures = new MTexture[paths.Length];
        private static Type toggleSwapBlockType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "CanyonHelper").GetType().Assembly.GetType("Celeste.Mod.CanyonHelper.ToggleSwapBlock");
        private static FieldInfo nodesField = toggleSwapBlockType.GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo oscillateField = toggleSwapBlockType.GetField("oscillate", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo stopAtEndField = toggleSwapBlockType.GetField("stopAtEnd", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo recalculateLaserColor = toggleSwapBlockType.GetMethod("RecalculateLaserColor", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo drawBlockStyle = toggleSwapBlockType.GetMethod("DrawBlockStyle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo updateTexture = typeof(ToggleBlockReskin).GetMethod("UpdateTexture", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static MethodInfo drawTexture = typeof(ToggleBlockReskin).GetMethod("DrawTexture", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static Hook updateTextureHook;
        private static Hook drawTextureHook;
        private static Dictionary<int, TextureList> cache = new Dictionary<int, TextureList>();

        private class TextureList : Component {
            public MTexture[] textures;

            public TextureList(MTexture[] textures) : base(false, false) {
                this.textures = textures;
            }
        }

        public static void Load() {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            updateTextureHook = new Hook(recalculateLaserColor, updateTexture);
            drawTextureHook = new Hook(drawBlockStyle, drawTexture);
        }

        public static void InitializeTextures() {
            for (int i = 0; i < paths.Length; i++) {
                allTextures[i] = GFX.Game[pathPrefix + paths[i]];
            }
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            updateTextureHook.Dispose();
            drawTextureHook.Dispose();
        }

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            bool isReskin = entityData.Name == "SJ2021/ToggleSwapBlock";
            if (isReskin) {
                Entity block = (Entity) Activator.CreateInstance(toggleSwapBlockType, new object[] { entityData, offset });
                if (!cache.TryGetValue(entityData.ID, out TextureList list)) {
                    cache.Add(entityData.ID, list = new TextureList(GetTextures(block)));
                }
                block.Add(list);
                level.Add(block);
            }
            return isReskin;
        }

        private static MTexture[] GetTextures(object block) {
            int[] indicators = CalculateIndicators(block);
            MTexture[] textures = new MTexture[indicators.Length];
            for (int i = 0; i < textures.Length; i++) {
                textures[i] = allTextures[indicators[i]];
            }
            return textures;
        }

        private static int[] CalculateIndicators(object block) {
            Vector2[] nodes = (Vector2[]) nodesField.GetValue(block);
            bool oscillate = (bool) oscillateField.GetValue(block);
            bool stopAtEnd = (bool) stopAtEndField.GetValue(block);
            int[] indicators = new int[nodes.Length * (oscillate ? 2 : 1)];
            int end = nodes.Length - 1;
            for (int i = 0; i < end; i++) {
                indicators[i] = IndicatorFromNodes(nodes, i, i + 1);
            }
            if (!oscillate) {
                indicators[end] = stopAtEnd ? DONE : IndicatorFromNodes(nodes, end, 0);
            } else {
                for (int i = 1; i <= end; i++) {
                    indicators[nodes.Length + i] = IndicatorFromNodes(nodes, i, i - 1);
                }
                indicators[end] = indicators[nodes.Length + end];
                indicators[nodes.Length] = indicators[0];
            }
            return indicators;
        }

        private static int IndicatorFromNodes(Vector2[] nodes, int start, int end) {
            int indicator;
            Vector2 dir = nodes[end] - nodes[start];
            if (dir.Equals(Vector2.Zero)) {
                indicator = STAY;
            } else {
                indicator = (int) Math.Round(dir.Angle() * (4 / Math.PI));
                if (indicator < 0) {
                    indicator += 8;
                }
            }
            return indicator;
        }

        public static void UpdateTexture<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Entity {
            orig(self);
            TextureList list = self.Get<TextureList>();
            if (list == null) {
                return;
            }
            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            MTexture[] textures = list.textures;
            int index = data.Get<int>("nodeIndex");
            if (data.Get<bool>("returning")) {
                index += textures.Length / 2;
            }
            data["texture"] = textures[index];
        }

        public static void DrawTexture<ToggleSwapBlock>(Action<ToggleSwapBlock, Vector2, float, float, MTexture[,], Sprite, Color> orig,
            ToggleSwapBlock self, Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) where ToggleSwapBlock : Entity {
            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            if (!data.Data.TryGetValue("texture", out object texture)) {
                orig(self, pos, width, height, ninSlice, middle, color);
            } else {
                orig(self, pos, width, height, ninSlice, null, color);
                ((MTexture) texture).DrawCentered(pos + self.Center - self.Position);
            }
        }
    }
}