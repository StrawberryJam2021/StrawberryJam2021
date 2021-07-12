using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
 
namespace Celeste.Mod.StrawberryJam2021.Entities {
    public static class ToggleSwapBlock {
        private const int STAY = 8, DONE = 9;
        private const float vanillaSpeed = 360f;
        private static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };
        private static string defaultIndicatorPath = "objects/StrawberryJam2021/toggleIndicator/plain/";
        private static Type toggleSwapBlockType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "CanyonHelper").GetType().Assembly.GetType("Celeste.Mod.CanyonHelper.ToggleSwapBlock");
        private static MethodInfo getNextNode = toggleSwapBlockType.GetMethod("GetNextNode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static string[] privateHookMethods = new string[] {
            "RecalculateLaserColor",
            "DrawBlockStyle",
            "OnPlayerDashed"
        };
        private static string[] publicHookMethods = new string[] {
            "Added",
            "Update"
        };
        private static List<Hook> hooks = new List<Hook>();

        private class DataComponent : Component {
            public readonly bool useIndicators;
            public readonly string indicatorPath;
            public readonly bool isConstant;
            public readonly float speed;
            public readonly bool disableTracks;
            public readonly bool allowDashSliding;
            public MTexture indicatorTexture;

            public DataComponent(bool useIndicators, string indicatorPath, bool isConstant, float speed, bool disableTracks, bool allowDashSliding) : base(false, false) {
                this.useIndicators = useIndicators;
                this.indicatorPath = indicatorPath;
                this.isConstant = isConstant;
                this.speed = speed;
                this.disableTracks = disableTracks;
                this.allowDashSliding = allowDashSliding;
            }
        }

        public static void Load() {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            createHooks(privateHookMethods, BindingFlags.NonPublic);
            createHooks(publicHookMethods, BindingFlags.Public);
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            foreach (Hook hook in hooks) {
                hook.Dispose();
            }
            hooks.Clear();
        }

        private static void createHooks(string[] methodNames, BindingFlags publicFlag) {
            foreach (string name in methodNames) {
                hooks.Add(new Hook(toggleSwapBlockType.GetMethod(name, publicFlag | BindingFlags.Instance),
                    typeof(ToggleSwapBlock).GetMethod("Mod" + name, BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(toggleSwapBlockType)));
            }
        }

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            bool isSJToggleBlock = entityData.Name == "SJ2021/ToggleSwapBlock";
            if (isSJToggleBlock) {
                Entity block = (Entity) Activator.CreateInstance(toggleSwapBlockType, new object[] { entityData, offset });
                bool useIndicators = entityData.Bool("directionIndicator", true);
                bool isConstant = entityData.Bool("constantSpeed", false);
                float speed = vanillaSpeed * entityData.Float("travelSpeed", 1f);
                string indicatorPath = entityData.Attr("customIndicatorPath", "");
                if (indicatorPath == "") {
                    indicatorPath = defaultIndicatorPath;
                }
                if (indicatorPath.Last() != '/') {
                    indicatorPath += '/';
                }
                bool disableTracks = entityData.Bool("disableTracks", false);
                bool allowDashSliding = entityData.Bool("allowDashSliding", false);
                Component dataComponent = new DataComponent(useIndicators, indicatorPath, isConstant, speed, disableTracks, allowDashSliding);
                block.Add(dataComponent);
                level.Add(block);
            }
            return isSJToggleBlock;
        }

        private static void ModOnPlayerDashed<ToggleSwapBlock>(Action<ToggleSwapBlock, Vector2> orig, ToggleSwapBlock self, Vector2 direction) where ToggleSwapBlock : Entity {
            orig(self, direction);
        }

        private static void ModUpdate<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Solid {
            DataComponent dataComp = self.Get<DataComponent>();
            if (dataComp != null && dataComp.allowDashSliding) {
                Player player = self.Scene.Tracker.GetEntity<Player>();
                if (player != null) {
                    StateMachine stateMachine = player.StateMachine;
                    Vector2 speed = player.Speed;
                    player.StateMachine = new StateMachine(26);
                    orig(self);
                    player.Speed = speed;
                    player.StateMachine = stateMachine;
                    return;
                }
            }
            orig(self);
        }

        private static void ModAdded<ToggleSwapBlock>(Action<ToggleSwapBlock, Scene> orig, ToggleSwapBlock self, Scene scene) where ToggleSwapBlock : Entity {
            orig(self, scene);

            DataComponent dataComp = self.Get<DataComponent>();
            if (dataComp == null || !dataComp.disableTracks) {
                return;
            }

            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            HideEntities(data.Get<Entity[]>("lasers"));
            HideEntities(data.Get<Entity[]>("nodeTextures"));
        }

        private static void HideEntities(Entity[] entities) {
            foreach (Entity e in entities) {
                e.Visible = false;
            }
        }

        private static void ModRecalculateLaserColor<ToggleSwapBlock>(Action<ToggleSwapBlock> orig, ToggleSwapBlock self) where ToggleSwapBlock : Entity {
            orig(self);

            DataComponent dataComp = self.Get<DataComponent>();
            if (dataComp == null) {
                return;
            }

            DynData<ToggleSwapBlock> data = new DynData<ToggleSwapBlock>(self);
            Vector2[] nodes = data.Get<Vector2[]>("nodes");
            int currNode = data.Get<int>("nodeIndex");
            int nextNode = (int) getNextNode.Invoke(self, new object[] { currNode });
            Vector2 dir = nodes[nextNode] - nodes[currNode];

            if (dataComp.useIndicators) {
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
                dataComp.indicatorTexture = GFX.Game[dataComp.indicatorPath + paths[indicator]];
            }

            if (dataComp.isConstant) {
                data.Set("travelSpeed", dataComp.speed / dir.Length());
            }
        }

        private static void ModDrawBlockStyle<ToggleSwapBlock>(Action<ToggleSwapBlock, Vector2, float, float, MTexture[,], Sprite, Color> orig,
            ToggleSwapBlock self, Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) where ToggleSwapBlock : Entity {
            DataComponent dataComp = self.Get<DataComponent>();

            if (dataComp == null || !dataComp.useIndicators) {
                orig(self, pos, width, height, ninSlice, middle, color);
            } else {
                orig(self, pos, width, height, ninSlice, null, color);
                dataComp.indicatorTexture.DrawCentered(pos + self.Center - self.Position);
            }
        }
    }
}