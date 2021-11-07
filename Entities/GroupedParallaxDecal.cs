using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class GroupedParallaxDecal : Entity {
        
        LevelData levelData;
        // GroupedParallaxDecal class should have a constructor with params LevelData ld and DecalData dd,
        public GroupedParallaxDecal(LevelData ld, DecalData dd, bool isFG)  {
            ld = levelData;

            Depth = isFG ? Depths.FGDecals : Depths.BGDecals;
            Position = dd.Position;

            Image i = new Image(GFX.Game[dd.Texture]);
            i.Position = dd.Position;
            Add(i);
        }

        private static IDetour hook_Level_orig_LoadLevel;
        private static Dictionary<string, GroupedParallaxDecal> ParallaxDecalByGroup; //Key = group name, Value = ParallaxDecalGroupHolder thingy
        private bool isFG;

        public static void Load() {
            hook_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel", BindingFlags.Public | BindingFlags.Instance), NoTouchy);
            On.Celeste.Level.UnloadLevel += disposeOfParallaxDecalByGroup_Dictionary_Here;
        }

        private static void disposeOfParallaxDecalByGroup_Dictionary_Here(On.Celeste.Level.orig_UnloadLevel orig, Level self) {
            ParallaxDecalByGroup.Clear();
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
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<LevelData>("FgDecals")) && cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out int _), instr => instr.MatchBr(out target))) {
                    //brtrue <target> is now our free "continue" operator
                    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out dIndex))) //it is safe to assume that since we have retrieved the stloc.s X, that that will remain consistent, since it is within the context of the function running it.
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldloc, dIndex); //dIndex is absolutely set
                        cursor.Emit(OpCodes.Ldloc, lIndex); //lIndex is absolutely set by first if 
                        cursor.Emit(OpCodes.Ldc_I4, 1); 
                        cursor.EmitDelegate<Func<Level, DecalData, LevelData, bool, bool>>(CheckAndAddDecalToGroup);
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
                        cursor.Emit(OpCodes.Ldc_I4, 0);
                        cursor.EmitDelegate<Func<Level, DecalData, LevelData, bool, bool>>(CheckAndAddDecalToGroup);
                        cursor.Emit(OpCodes.Brtrue, target);
                    }
                }
            }
        }

        //a method to add an DecalData to its list and store that Image from that method (class? -Ly), as well as its Position relative to the first DecalData added, we'll call this AddDecalToGroup(DecalData newDD)
        private static void AddDecalToGroup(GroupedParallaxDecal group, DecalData dd) {
            Image i = new Image(GFX.Game[dd.Texture]);
            i.Position = dd.Position - group.Position;
            group.Add(i);
        }

        private static bool CheckAndAddDecalToGroup(Level level, DecalData dd, LevelData ld, bool isFG) {
            //If the conditions are not met to add this to the Grouped Parallax Decal, return false, otherwise determine its group,
            //If its group is found in the ParallaxDecalByGroup dictionary already, run AddDecalToGroup, otherwise construct the GroupedParallaxDecal with that DecalData and add it to the Dictionary by group
            
            
            if (!dd.Texture.Contains("Gameplay/decals/SJGroupedParallaxDecals/")) {
                return false;
            }

            //group name is contained in the file path, probably a better way to do this but Idk the file path structure but I know this will work.
            string groupName = dd.Texture.Substring(dd.Texture.IndexOf("SJGroupedParallaxDecals/") + 26);
            groupName = groupName.Substring(groupName.IndexOf("/"));

            if (ParallaxDecalByGroup.ContainsKey(groupName)) {
                AddDecalToGroup(ParallaxDecalByGroup[groupName], dd);
            } else {
                ParallaxDecalByGroup.Add(groupName, new GroupedParallaxDecal(ld, dd, isFG));
            }
            return true;
        }
    }
}
