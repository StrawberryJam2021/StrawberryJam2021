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

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/OshiroAttackTimeTrigger")]
    public class OshiroAttackTimeTrigger : Trigger {
        private bool Enable = true;
        public OshiroAttackTimeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Enable = data.Bool("Enable", true);
        }
        public static void Load(){
            IL.Celeste.AngryOshiro.ChaseCoroutine += ModAttackTime;
        }
        public static void Unload() {
            IL.Celeste.AngryOshiro.ChaseCoroutine -= ModAttackTime;
        }

        private static void ModAttackTime(ILContext il) {
            ILCursor cursor = new ILCursor(il);
                while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AreaKey>("Mode"))) {
                    Logger.Log("SJ2021", $"Modifying B-Side requirement for Oshiro timing @{cursor.Index}.");
                    cursor.Emit(OpCodes.Pop);
                    cursor.EmitDelegate<Func<bool, bool>>(val => StrawberryJam2021Module.Session.OshiroBSideMode || (Monocle.Engine.Scene as Level).Session.Area.Mode!=0);
                    break;
                }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            StrawberryJam2021Module.Session.OshiroBSideMode = Enable;
        }
    }
}
