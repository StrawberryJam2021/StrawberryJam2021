using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public static class ToggleSwapBlock {
        private const int STAY = 8, DONE = 9;
        private static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };
        private static string defaultIndicatorPath = "objects/StrawberryJam2021/toggleIndicator/plain/";
        private static Type toggleSwapBlockType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "CanyonHelper").GetType().Assembly.GetType("Celeste.Mod.CanyonHelper.ToggleSwapBlock");
        private static MethodInfo getNextNode = toggleSwapBlockType.GetMethod("GetNextNode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo recalculateLaserColor = toggleSwapBlockType.GetMethod("RecalculateLaserColor", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo drawBlockStyle = toggleSwapBlockType.GetMethod("DrawBlockStyle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo updateTextureAndSpeed = typeof(ToggleSwapBlock).GetMethod("UpdateTextureAndSpeed", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static MethodInfo drawTexture = typeof(ToggleSwapBlock).GetMethod("DrawTexture", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        private static Hook updateTextureAndSpeedHook;
        private static Hook drawTextureHook;

        private class DataComponent : Component {
            public readonly bool isReskin;
            public readonly bool isConstant;
            public readonly float speed;
            public readonly string indicatorPath;
            public MTexture texture;

            public DataComponent(bool isReskin, bool isConstant, float speed, string indicatorPath) : base(false, false) {
                this.isReskin = isReskin;
                this.isConstant = isConstant;
                this.speed = speed;
                this.indicatorPath = indicatorPath;
            }
        }

        public static void Load() {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            updateTextureAndSpeedHook = new Hook(recalculateLaserColor, updateTextureAndSpeed);
            drawTextureHook = new Hook(drawBlockStyle, drawTexture);
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            updateTextureAndSpeedHook.Dispose();
            drawTextureHook.Dispose();
        }

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            bool isSJToggleBlock = entityData.Name == "SJ2021/ToggleSwapBlock";
            if (isSJToggleBlock) {
                bool isReskin = entityData.Bool("directionIndicator", true);
                bool isConstant = entityData.Bool("constantSpeed", false);
                float speed = 360f * entityData.Float("travelSpeed", 1f);
                string indicatorPath = entityData.Attr("customIndicatorPath", "");
                if (indicatorPath == "") {
                    indicatorPath = defaultIndicatorPath;
                }
                if (indicatorPath.Last() != '/') {
                    indicatorPath += '/';
                }
                Component dataComponent = new DataComponent(isReskin, isConstant, speed, indicatorPath);
                Entity block = (Entity) Activator.CreateInstance(toggleSwapBlockType, new object[] { entityData, offset });
                block.Add(dataComponent);
                level.Add(block);
            }
            return isSJToggleBlock;
        }

        private static void UpdateTextureAndSpeed<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Entity {
            orig(self);
            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);

            DataComponent dataComp = self.Get<DataComponent>();
            if (dataComp == null) {
                return;
            }
            Vector2[] nodes = data.Get<Vector2[]>("nodes");
            int currNode = data.Get<int>("nodeIndex");
            int nextNode = (int) getNextNode.Invoke(self, new object[] { currNode });
            Vector2 dir = nodes[nextNode] - nodes[currNode];

            if (dataComp.isReskin) {
                bool stopAtEnd = data.Get<bool>("stopAtEnd");
                int indicator;
                if (stopAtEnd && currNode == nodes.Length - 1) {
                    indicator = DONE;
                } else {
                    if (dir.Equals(Vector2.Zero)) {
                        indicator = STAY;
                    } else {
                        indicator = (int) Math.Round(dir.Angle() * (4 / Math.PI));
                        if (indicator < 0) {
                            indicator += 8;
                        }
                    }
                }
                dataComp.texture = GFX.Game[dataComp.indicatorPath + paths[indicator]];
            }

            if (dataComp.isConstant) {
                data.Set("travelSpeed", dataComp.speed / dir.Length());
            }
        }

        private static void DrawTexture<ToggleSwapBlock>(Action<ToggleSwapBlock, Vector2, float, float, MTexture[,], Sprite, Color> orig,
            ToggleSwapBlock self, Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) where ToggleSwapBlock : Entity {
            DataComponent dataComp = self.Get<DataComponent>();
            if (dataComp == null || !dataComp.isReskin) {
                orig(self, pos, width, height, ninSlice, middle, color);
            } else {
                orig(self, pos, width, height, ninSlice, null, color);
                dataComp.texture.DrawCentered(pos + self.Center - self.Position);
            }
        }
    }
}