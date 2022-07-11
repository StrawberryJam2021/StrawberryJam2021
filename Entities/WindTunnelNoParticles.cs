using Celeste.Mod.Entities;
using FactoryHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WindTunnelNoParticles")]
    public class WindTunnelNoParticles : WindTunnel {

        private static ILHook windTunnelCtorHook;
        private static ConstructorInfo windTunnelCtorInfo = typeof(WindTunnel).GetConstructor(new Type[] { typeof(Vector2), typeof(int), typeof(int), typeof(float), typeof(string), typeof(string), typeof(bool) });

        public WindTunnelNoParticles(EntityData data, Vector2 offset)
            : base(data, offset) { }

        public static void OnWindTunnelCtor(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchDiv())) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<int, WindTunnel, int>>((orig, self) => {
                    if (self is WindTunnelNoParticles tunnel) {
                        return 0;
                    }
                    return orig;
                });
            }
        }

        public static void Load() {
            windTunnelCtorHook = new ILHook(windTunnelCtorInfo, OnWindTunnelCtor);
        }

        public static void Unload() {
            windTunnelCtorHook?.Dispose();
            windTunnelCtorHook = null;
        }
    }
}
