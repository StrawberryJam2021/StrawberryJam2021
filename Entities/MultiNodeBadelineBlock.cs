using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    class MultiNodeBadelineBlock {
        public static void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
        }

        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            if (entityData.Name == "SJ2021/MultiNodeBadelineBlock") {
                entityData.Name = "BrokemiaHelper/nonBadelineMovingBlock";
                return Level.LoadCustomEntity(entityData, level);
            }
            
            return false;
        }
    }
}
