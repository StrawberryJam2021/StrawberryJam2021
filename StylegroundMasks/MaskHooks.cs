using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    public static class MaskHooks {
        public static void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            BloomMask.Load();
            StylegroundMask.Load();
            LightingMask.Load();
            ColorGradeMask.Load();
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            BloomMask.UnLoad();
            StylegroundMask.UnLoad();
            LightingMask.UnLoad();
            ColorGradeMask.UnLoad();
        }


        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Microsoft.Xna.Framework.Vector2 offset, EntityData entityData) {
            if (!((level.Session?.Area.GetSID().StartsWith("StrawberryJam2021/0-Lobbies/")) ?? false)) {
                return false;
            }
            switch (entityData.Name) {
                case "SJ2021/AllInOneMask":
                    level.Add(new AllInOneMask(entityData, offset));
                    return true;
                case "SJ2021/BloomMask":
                    level.Add(new BloomMask(entityData, offset));
                    return true;
                case "SJ2021/ColorGradeMask":
                    level.Add(new ColorGradeMask(entityData, offset));
                    return true;
                case "SJ2021/LightingMask":
                    level.Add(new LightingMask(entityData, offset));
                    return true;
                case "SJ2021/StylegroundMask":
                    level.Add(new StylegroundMask(entityData, offset));
                    return true;
            }
            return false;
        }
    }
}
