using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CoreToggleNoFlash")]
    public class CoreToggleNoFlash : CoreModeToggle {

        public CoreToggleNoFlash(Vector2 position, bool onlyFire, bool onlyIce, bool persistent)
            : base(position, onlyFire, onlyIce, persistent) {
        }

        public CoreToggleNoFlash(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("onlyFire", false), data.Bool("onlyIce", false),
                  data.Bool("persistent", false)) { }

        public static void Load() {
            IL.Celeste.CoreModeToggle.OnPlayer += CoreModeToggleOnPlayerHook;
        }

        public static void Unload() {
            IL.Celeste.CoreModeToggle.OnPlayer -= CoreModeToggleOnPlayerHook;
        }

        private static void CoreModeToggleOnPlayerHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.15f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, CoreModeToggle, float>>((orig, toggle) => {
                    if (toggle is CoreToggleNoFlash) {
                        return 0f;
                    }
                    return orig;
                });
            }
        }
    }
}
