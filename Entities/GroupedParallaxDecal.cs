﻿using Microsoft.Xna.Framework;
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
using System.Xml;
using static Celeste.Mod.DecalRegistry;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class GroupedParallaxDecal : Entity {
        
        LevelData levelData;

        private float parallaxAmount;


        // GroupedParallaxDecal class should have a constructor with params LevelData ld and DecalData dd,
        public GroupedParallaxDecal(LevelData ld, DecalData dd, bool isFG): base(dd.Position)  {
            ld = levelData;
            string path = dd.Texture.Substring(0, dd.Texture.Length - 4).Trim();

            Logger.Log("GroupedParallaxDecal", "Path: " + dd.Texture);
            Logger.Log("GroupedParallaxDecal", "Path Length: " + path.Length);

            DecalInfo dInfo = DecalRegistry.RegisteredDecals[path];

            //there's two relevant attributes to parallaxing: depth and parallax amount
            //do this way instead of using something like List.Find(...) because most parallaxed decals only have two xml attributes, and we need both of them.
            foreach (KeyValuePair<string, XmlAttributeCollection> xmlAC in dInfo.CustomProperties) {
                if (xmlAC.Key.Equals("parallax")) {
                    parallaxAmount = float.Parse(xmlAC.Value["amount"].Value);
                } else if (xmlAC.Key.Equals("depth")) {
                    Depth = int.Parse(xmlAC.Value["value"].Value);
                }
            }

            AddDecalToGroup(this, dd);
        }

        public override void Render() {
            //adapted from Decal.Render()
            Vector2 position = Position;
            Vector2 vector = (base.Scene as Level).Camera.Position + new Vector2(160f, 90f);
            Vector2 vector2 = (Position - vector) * parallaxAmount;
            Position += vector2;
            base.Render();
            Position = position;
        }

        private static IDetour hook_Level_orig_LoadLevel;
        private static Dictionary<string, GroupedParallaxDecal> ParallaxDecalByGroup; //Key = group name, Value = ParallaxDecalGroupHolder thingy

        public static void Load() {
            ParallaxDecalByGroup = new Dictionary<string, GroupedParallaxDecal>();
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
            Image i = new(GFX.Game["decals/" + dd.Texture.Substring(0, dd.Texture.Length - 4)]);
            i.Position = dd.Position - group.Position;
            i.CenterOrigin();
            Logger.Log("GroupedParallaxDecal", i.Position.X + "|" + i.Position.Y);
            group.Add(i);
        }

        private static bool CheckAndAddDecalToGroup(Level level, DecalData dd, LevelData ld, bool isFG) {
            //If the conditions are not met to add this to the Grouped Parallax Decal, return false, otherwise determine its group,
            //If its group is found in the ParallaxDecalByGroup dictionary already, run AddDecalToGroup, otherwise construct the GroupedParallaxDecal with that DecalData and add it to the Dictionary by group
            
            if (!dd.Texture.Contains("sjgroupedparallaxdecals")) {
                Logger.Log("GroupedParallaxDecal", dd.Texture);
                

                return false;
            }
            
            //group name is contained in the file path, probably a better way to do this but Idk the file path structure but I know this will work.
            string groupName = dd.Texture.Substring(dd.Texture.IndexOf("sjgroupedparallaxdecals/") + 24); //len("sjgroupedparallaxdecals/") = 24
            groupName = groupName.Substring(0, groupName.IndexOf("/"));

            Logger.Log("GroupedParallaxDecal", dd.Texture);
            Logger.Log("GroupedParallaxDecal", groupName);

            if (ParallaxDecalByGroup.ContainsKey(groupName)) {
                AddDecalToGroup(ParallaxDecalByGroup[groupName], dd);
            } else {
                GroupedParallaxDecal groupeddecal = new(ld, dd, isFG);
                ParallaxDecalByGroup.Add(groupName, groupeddecal);
                level.Add(groupeddecal);
            }
            return true;
        }
    }
}
