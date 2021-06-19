using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using IL.Monocle;
using Monocle;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;
using MonoMod.Cil;

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
            FieldInfo f_this = oshiroCoroutineInfo.DeclaringType.GetField("<>4__this");
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AreaKey>("Mode"))) {
                Logger.Log("SJ2021", $"Modifying B-Side requirement for Oshiro timing @{cursor.Index}.");
                cursor.EmitDelegate<Func<int, int>>(val => (StrawberryJam2021Module.Session.OshiroBSideMode || (Monocle.Engine.Scene as Level).Session.Area.Mode!=0)?1:0);
                break;
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            StrawberryJam2021Module.Session.OshiroBSideMode = Enable;
        }
    }
}
