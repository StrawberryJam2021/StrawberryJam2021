using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using System.Reflection;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    public class CrystallineHelperTimeFreezeMusicController : Entity {

        public static void Load() {
            IL.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }
        public static void Unload() {
            IL.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static void LevelLoader_LoadingThread(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchRet());
            if (cursor.TryGotoPrev(instr => instr.MatchLdarg(0), instr => instr.MatchLdcI4(1), instr => instr.MatchCallvirt<LevelLoader>("set_Loaded"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(CrystallineHelperTimeFreezeMusicController).GetMethod("LoadingThreadModifier", BindingFlags.Public|BindingFlags.Static));
            }
        }

        public static void LoadingThreadModifier(LevelLoader loader) {
            foreach (LevelData levelData in loader.Level.Session.MapData?.Levels) {
                foreach (EntityData entityData in levelData.Entities) {
                    if (entityData.Name == "SJ2021/TimeFreezeMusicController") {
                        loader.Level.Add(new CrystallineHelperTimeFreezeMusicController(entityData, levelData.Position));
                        return;
                    }
                }
            }
        }
        
        
        public static FieldInfo crystallineHelper_TimeCrystal_stopStage;

        public float paramOff, paramOn;
        public string paramName;
        public bool prevValue;

        public CrystallineHelperTimeFreezeMusicController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            paramName = data.Attr("paramName");
            paramOff = data.Float("paramOff");
            paramOn = data.Float("paramOn");
            Tag = Tags.Global | Tags.Persistent;
        }

        public override void Update() {
            base.Update();
            bool value = (int)crystallineHelper_TimeCrystal_stopStage.GetValue(null) == 1;
            if(value != prevValue) {
                AudioState audio = SceneAs<Level>().Session.Audio;
                audio.Music.Param(paramName, value ? paramOn : paramOff);
                audio.Apply();
            }
            prevValue = value;
        }
    }
}
