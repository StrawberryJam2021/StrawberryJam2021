using System;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SpeedPreservePuffer")]
    public class SpeedPreservePuffer : Puffer {
        public bool Static;
        public static float storedSpeed;
        public static Puffer lastPuffer;
        private static ILHook origUpdateHook;

        public SpeedPreservePuffer(EntityData data, Vector2 offset) : base(data, offset) {
            Static = data.Bool("static", true);
            if (Static) {
                Get<SineWave>()?.RemoveSelf();
                Position.X += 0.0001f;
            }
            Depth = Depths.Player - 1;
        }

        public static void Load() {
            origUpdateHook = new ILHook(typeof(Player).GetMethod("orig_Update"), playerOrigUpdateHook);
            IL.Celeste.Puffer.ctor_Vector2_bool += onPufferConstructor; //taken from max480's helping hand
            IL.Celeste.Puffer.Explode += onPufferExplode;
        }

        public static void Unload() {
            origUpdateHook?.Dispose();
            IL.Celeste.Puffer.ctor_Vector2_bool -= onPufferConstructor;
            IL.Celeste.Puffer.Explode -= onPufferExplode;
        }

        private static void playerOrigUpdateHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStfld<Vector2>("X"))) {
                Logger.Log("SJ2021/SpeedPreservePuffer", $"Injecting call to un-hardcode super boost lenience at {cursor.Index} in IL for Player.orig_Update");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((orig, self) => {
                    // Normally the 1.2x launch speed is hardcoded when using lenience frames, so here we manually add the extra 0.2x launch speed
                    if (lastPuffer is SpeedPreservePuffer) {
                        return self.Speed.X + Math.Abs(0.2f * orig) * Math.Sign(self.Speed.X);
                    }
                    return orig;
                });
            }
        }

        private static void onPufferConstructor(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SineWave>("Randomize"))) {
                Logger.Log("SJ2021/SpeedPreservePuffer", $"Injecting call to unrandomize puffer sine wave at {cursor.Index} in IL for Puffer constructor");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Puffer).GetField("idleSine", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitDelegate<Action<Puffer, SineWave>>((self, idleSine) => {
                    if (self is SpeedPreservePuffer puffer && puffer.Static) {
                        // unrandomize the initial pufferfish position.
                        idleSine.Reset();
                    }
                });
            }
        }

        private static void onPufferExplode(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchCallvirt<Entity>("get_Center"),
                instr => instr.MatchCallvirt<Scene>("CollideCheck"),
                instr => instr.MatchBrtrue(out _))) {
                Logger.Log("SJ2021/SpeedPreservePuffer", $"Injecting call to store player speed before puffer explosion at {cursor.Index} in IL for Puffer.Explode");

                cursor.EmitDelegate<Action>(() => {
                    Player player = Engine.Scene.Tracker.GetEntity<Player>();
                    storedSpeed = player.Speed.X;
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("ExplodeLaunch"))) {
                Logger.Log("SJ2021/SpeedPreservePuffer", $"Injecting call to add player speed to puffer explosion at {cursor.Index} in IL for Puffer.Explode");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Puffer>>((self) => {
                    lastPuffer = self;
                    if (self is SpeedPreservePuffer) {
                        Player player = Engine.Scene.Tracker.GetEntity<Player>();
                        player.Speed.X += Math.Abs(storedSpeed) * Math.Sign(player.Speed.X);
                    }
                });
            }
        }
    }
}
