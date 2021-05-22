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
        const int STAY = 8, DONE = 9;
        static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done"};
        static Type toggleSwapBlockType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "CanyonHelper").GetType().Assembly.GetType("Celeste.Mod.CanyonHelper.ToggleSwapBlock");
        static FieldInfo nodesField = toggleSwapBlockType.GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo oscillateField = toggleSwapBlockType.GetField("oscillate", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo stopAtEndField = toggleSwapBlockType.GetField("stopAtEnd", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo recalculateLaserColor = toggleSwapBlockType.GetMethod("RecalculateLaserColor", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo updateSprite = typeof(ToggleBlockReskin).GetMethod("UpdateSprite", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType);
        static Hook updateSpriteHook;
        static Dictionary<int, SpriteList> cache = new Dictionary<int, SpriteList>();

        private class SpriteList : Component {
            public Sprite[] sprites;

            public SpriteList(Sprite[] sprites) : base(false, false) {
                this.sprites = sprites;
            }
        }

        public static void Load() {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            updateSpriteHook = new Hook(recalculateLaserColor, updateSprite);
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            updateSpriteHook.Dispose();
        }

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            bool isReskin = entityData.Name == "SJ2021/ToggleSwapBlock";
            if (isReskin) {
                Entity block = (Entity) Activator.CreateInstance(toggleSwapBlockType, new object[] { entityData, offset });
                if (!cache.TryGetValue(entityData.ID, out SpriteList list)) {
                    cache.Add(entityData.ID, list = new SpriteList(MakeSprites(block)));
                }
                block.Add(list);
                level.Add(block);
            }
            return isReskin;
        }

        private static Sprite[] MakeSprites(object block) {
            Vector2[] nodes = (Vector2[]) nodesField.GetValue(block);
            bool oscillate = (bool) oscillateField.GetValue(block);
            bool stopAtEnd = (bool) stopAtEndField.GetValue(block);
            Sprite[] sprites = new Sprite[nodes.Length * (oscillate ? 2 : 1)];
            int end = nodes.Length - 1;
            for (int i = 0; i < end; i++) {
                sprites[i] = MakeSprite(IndicatorFromNodes(nodes, i, i + 1));
            }
            if (!oscillate) {
                sprites[end] = MakeSprite(stopAtEnd ? DONE : IndicatorFromNodes(nodes, end, 0));
            } else {
                for (int i = 1; i <= end; i++) {
                    sprites[nodes.Length + i] = MakeSprite(IndicatorFromNodes(nodes, i, i - 1));
                }
                sprites[end] = sprites[nodes.Length + end];
                sprites[nodes.Length] = sprites[0];
            }
            return sprites;
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

        private static Sprite MakeSprite(int idx) {
            string path = paths[idx];
            Sprite sprite = new Sprite(GFX.Game, "objects/StrawberryJam2021/toggleIndicator/");
            sprite.AddLoop("idle", path, 0f);
            sprite.AddLoop("moving", path, 0f);
            sprite.Justify = new Vector2(0.5f, 0.5f);
            return sprite;
        }

        public static void UpdateSprite<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Entity {
            orig(self);
            SpriteList spriteList = self.Get<SpriteList>();
            if (spriteList == null) {
                return;
            }
            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            Sprite[] sprites = spriteList.sprites;
            int index = data.Get<int>("nodeIndex");
            if (data.Get<bool>("returning")) {
                index += sprites.Length / 2;
            }
            Sprite sprite = sprites[index];
            data.Set("middleRed", sprite);
            sprite.Play("idle");
        }
    }
}