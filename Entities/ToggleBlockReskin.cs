using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class ToggleBlockReskin {
        private const int STAY = 8, DONE = 9;
        private static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };
        private static string pathPrefix = "objects/StrawberryJam2021/toggleIndicator/";
        private static MTexture[] textures = new MTexture[paths.Length];
        private static Type toggleSwapBlockType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "CanyonHelper").GetType().Assembly.GetType("Celeste.Mod.CanyonHelper.ToggleSwapBlock");
        private static MethodInfo getNextNode = toggleSwapBlockType.GetMethod("GetNextNode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo recalculateLaserColor = toggleSwapBlockType.GetMethod("RecalculateLaserColor", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo drawBlockStyle = toggleSwapBlockType.GetMethod("DrawBlockStyle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo updateTexture = typeof(ToggleBlockReskin).GetMethod("UpdateTexture", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static MethodInfo drawTexture = typeof(ToggleBlockReskin).GetMethod("DrawTexture", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static Hook updateTextureHook;
        private static Hook drawTextureHook;

        private class TextureComponent : Component {
            public MTexture texture;

            public TextureComponent() : base(false, false) { }
        }

        public static void Load() {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            updateTextureHook = new Hook(recalculateLaserColor, updateTexture);
            drawTextureHook = new Hook(drawBlockStyle, drawTexture);
        }

        public static void InitializeTextures() {
            for (int i = 0; i < paths.Length; i++) {
                textures[i] = GFX.Game[pathPrefix + paths[i]];
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
                block.Add(new TextureComponent());
                level.Add(block);
            }
            return isReskin;
        }

        private static void UpdateTexture<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Entity {
            orig(self);
            TextureComponent texComp = self.Get<TextureComponent>();
            if (texComp == null) {
                return;
            }
            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            Vector2[] nodes = data.Get<Vector2[]>("nodes");
            int currNode = data.Get<int>("nodeIndex");
            int nextNode = (int) getNextNode.Invoke(self, new object[] { currNode });
            bool stopAtEnd = data.Get<bool>("stopAtEnd");
            int indicator;
            if (stopAtEnd && currNode == nodes.Length - 1) {
                indicator = DONE;
            } else {
                Vector2 dir = nodes[nextNode] - nodes[currNode];
                if (dir.Equals(Vector2.Zero)) {
                    indicator = STAY;
                } else {
                    indicator = (int) Math.Round(dir.Angle() * (4 / Math.PI));
                    if (indicator < 0) {
                        indicator += 8;
                    }
                }
            }
            texComp.texture = textures[indicator];
        }

        private static void DrawTexture<ToggleSwapBlock>(Action<ToggleSwapBlock, Vector2, float, float, MTexture[,], Sprite, Color> orig,
            ToggleSwapBlock self, Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) where ToggleSwapBlock : Entity {
            TextureComponent texComp = self.Get<TextureComponent>();
            if (texComp == null) {
                orig(self, pos, width, height, ninSlice, middle, color);
            } else {
                orig(self, pos, width, height, ninSlice, null, color);
                texComp.texture.DrawCentered(pos + self.Center - self.Position);
            }
        }
    }
}