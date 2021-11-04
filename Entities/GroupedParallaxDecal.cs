using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    class GroupedParallaxDecal {
        private static IDetour hook_Level_orig_LoadLevel;
        private static Dictionary<string, GroupedParallaxDecal> ParallaxDecalByGroup; //Key = group name, Value = ParallaxDecalGroupHolder thingy

        public static void Load() {
            hook_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel", BindingFlags.Public | BindingFlags.Instance), NoTouchy);
            On.Celeste.Level.UnloadLevel += disposeOfParallaxDecalByGroup_Dictionary_Here;
        }
        public static void Unload() {
            hook_Level_orig_LoadLevel?.Dispose();
            On.Celeste.Level.UnloadLevel -= disposeOfParallaxDecalByGroup_Dictionary_Here;
        }
        private static void NoTouchy(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            ILLabel target = null; //required because out target is not always responsive.
            int lIndex = -1; //LevelData Index
            int dIndex = -1; //DecalData Index
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Session>("get_LevelData"), i2 => i2.MatchStloc(out lIndex))) {
                cursor.EmitDelegate<Action>(() => { idIntegerForDecalEntities = 0; });
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<LevelData>("FgDecals")) && cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out int _), instr => instr.MatchBr(out target))) {
                    //brtrue <target> is now our free "continue" operator
                    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out dIndex))) //it is safe to assume that since we have retrieved the stloc.s X, that that will remain consistent, since it is within the context of the function running it.
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldloc, dIndex); //dIndex is absolutely set
                        cursor.Emit(OpCodes.Ldloc, lIndex); //lIndex is absolutely set by first if 
                        cursor.EmitDelegate<Func<Level, DecalData, LevelData, bool>>(ImplementCustomDecalToEntitySet);
                        cursor.Emit(OpCodes.Brtrue, target);
                    }
                }
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<LevelData>("BgDecals")) && cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out int _), instr => instr.MatchBr(out target))) {
                    //brtrue <target> is now our free "continue" operator
                    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out dIndex))) //it is safe to assume that since we have retrieved the stloc.s X, that that will remain consistent, since it is within the context of the function running it.
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldloc, dIndex); //dIndex is absolutely set
                        cursor.Emit(OpCodes.Ldloc, lIndex); //lIndex is absolutely set by first if 
                        cursor.EmitDelegate<Func<Level, DecalData, LevelData, bool>>(TheMethodYouNeedToActuallyWorkWith);
                        cursor.Emit(OpCodes.Brtrue, target);
                    }
                }
            }
        }

        // GroupedParallaxDecal class should have a constructor with params LevelData ld and DecalData dd, as well as a method to add an DecalData to its list and store that Image from that method, as well as its Position relative to the first DecalData added, we'll call this AddDecalToGroup(DecalData newDD)

        private static bool TheMethodYouNeedToActuallyWorkWith(Level level, DecalData data, LevelData levelData) {
            //If the conditions are not met to add this to the Grouped Parallax Decal, return false, otherwise determine its group,
            //If its group is found in the ParallaxDecalByGroup dictionary already, run AddDecalToGroup, otherwise construct the GroupedParallaxDecal with that DecalData and add it to the Dictionary by group
        }
    }
}
