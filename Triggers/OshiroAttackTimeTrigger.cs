﻿using System;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/OshiroAttackTimeTrigger")]
    public class OshiroAttackTimeTrigger : Trigger {
        private bool Enable = true;
        private static ILHook oshiroHook;
        private static BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static MethodInfo oshiroCoroutineInfo = typeof(AngryOshiro).GetMethod("ChaseCoroutine", privateInstance).GetStateMachineTarget();
        public OshiroAttackTimeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("Enable", true);
        }
        public static void Load(){
            oshiroHook = new ILHook(oshiroCoroutineInfo, ModAttackTime);
        }
        public static void Unload() {
            oshiroHook?.Dispose();
        }

        private static void ModAttackTime(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AreaKey>("Mode"))) {
                Logger.Log("SJ2021", $"Modifying B-Side requirement for Oshiro timing @{cursor.Index}.");
                cursor.EmitDelegate<Func<int, int>>(val => (StrawberryJam2021Module.Session.OshiroBSideMode)?1:(int)(Monocle.Engine.Scene as Level).Session.Area.Mode);
                break;
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            StrawberryJam2021Module.Session.OshiroBSideMode = Enable;
        }
    }
}
