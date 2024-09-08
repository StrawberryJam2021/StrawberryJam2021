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
            IL.Celeste.Puffer.ctor_Vector2_bool += onPufferConstructor; //taken from maddie480's helping hand
            IL.Celeste.Puffer.Explode += onPufferExplode;
        }

        public static void Unload() {
            origUpdateHook?.Dispose();
            origUpdateHook = null;
            IL.Celeste.Puffer.ctor_Vector2_bool -= onPufferConstructor;
            IL.Celeste.Puffer.Explode -= onPufferExplode;
        }

        private static void playerOrigUpdateHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStfld<Vector2>("X"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((orig, self) => {
                    // Normally the 1.2x launch speed is hardcoded when using lenience frames, so here we manually add the extra 0.2x launch speed
                    if (self.Get<DataComponent>() is { } dataComponent && dataComponent.speedPreservingPuffer) {
                        return self.Speed.X + Math.Abs(0.2f * orig) * Math.Sign(self.Speed.X);
                    }
                    return orig;
                });
            }
        }

        private static void onPufferConstructor(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SineWave>("Randomize"))) {
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

                cursor.EmitDelegate<Action>(() => {
                    if (Engine.Scene.Tracker.GetEntity<Player>() is { } player) {
                        var pufferData = player.Get<DataComponent>();
                        if (pufferData is null) player.Add(pufferData = new DataComponent());
                        pufferData.storedSpeed = player.Speed.X;
                    }
                });
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("ExplodeLaunch"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Puffer>>((self) => {
                    if (self.Scene.Tracker.GetEntity<Player>() is { } player && player.Get<DataComponent>() is { } dataComponent) {
                        if (self is SpeedPreservePuffer) {
                            player.Speed.X += Math.Abs(dataComponent.storedSpeed) * Math.Sign(player.Speed.X);
                            dataComponent.speedPreservingPuffer = true;
                        } else {
                            dataComponent.speedPreservingPuffer = false;
                        }

                    }
                });
            }
        }

        private class DataComponent : Component {
            public float storedSpeed;
            public bool speedPreservingPuffer;
            public DataComponent() : base(false, false) { }
        }
    }
}
