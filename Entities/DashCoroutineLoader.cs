using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public static class DashCoroutineLoader {

        private static IDetour hook_Player_DashCoroutine;

        public static void Load() {
            MethodInfo m = typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
            hook_Player_DashCoroutine = new ILHook(m, ModDashSpeed);
        }

        public static void Unload() {
            hook_Player_DashCoroutine?.Dispose();
            hook_Player_DashCoroutine = null;
        }

        private static void ModDashSpeed(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            int playerIndex = 1;
            ILLabel VanillaTarget = null;
            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchBneUn(out VanillaTarget),
                instr => instr.MatchLdloc(out playerIndex),
                instr => instr.MatchLdfld<Player>("StateMachine"),
                instr => instr.MatchLdcI4(1))) {
                if (VanillaTarget != null) {
                    cursor.GotoPrev(MoveType.After, instr => instr.MatchBneUn(out ILLabel _));
                    ILLabel OurTarget = cursor.MarkLabel();
                    cursor.GotoLabel(VanillaTarget, MoveType.After);
                    ILCursor cursor2 = cursor.Clone();
                    if (cursor2.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_One")) && cursor2.TryGotoNext(instr => instr.OpCode == OpCodes.Stfld)) {
                        cursor.Emit(OpCodes.Ldloc, playerIndex);
                        cursor.EmitDelegate<Func<Player, bool>>(CheckForNewToggleSwapBlocks);
                        cursor.Emit(OpCodes.Brtrue, OurTarget);
                        cursor2.Emit(OpCodes.Ldloc, playerIndex);
                        cursor2.EmitDelegate<Func<Vector2, Player, Vector2>>(ModifyDashSpeedWithSwapBlock);
                    }
                }
            }
        }

        private static bool CheckForNewToggleSwapBlocks(Player player) {
            if (!(player.DashDir.X != 0f && Input.GrabCheck))
                return false; // We wanna get rid of this case because it's the initial case that we dont wanna worry about.
            ToggleSwapBlock ntsb = player.CollideFirst<ToggleSwapBlock>(player.Position + Vector2.UnitX * Math.Sign(player.DashDir.X)); //Same thing as the SwapBlock but with NewToggleSwapBlock
            return ntsb != null && !ntsb.allowDashSliding && Math.Sign(ntsb.Direction.X) == Math.Sign(player.DashDir.X); //if this is true then brtrue will pass it back to the inside of the if statement
        }

        //Important detail! Since swapCancel's X and Y values are 1 and 0 only we can do this. Normally we wouldn't be allowed to do this.
        private static Vector2 ModifyDashSpeedWithSwapBlock(Vector2 orig, Player player) {
            Vector2 swapCancel = orig;
            foreach (ToggleSwapBlock entity in player.Scene.Tracker.GetEntities<ToggleSwapBlock>()) {
                if (entity != null && !entity.allowDashSliding && entity.moving && entity.GetPlayerRider() == player) {
                    if (player.DashDir.X != 0f && Math.Sign(entity.Direction.X) == Math.Sign(player.DashDir.X)) {
                        player.Speed.X = (swapCancel.X = 0f);
                    }
                    if (player.DashDir.Y != 0f && Math.Sign(entity.Direction.Y) == Math.Sign(player.DashDir.Y)) {
                        player.Speed.Y = (swapCancel.Y = 0f);
                    }
                }
            }
            return swapCancel;
        }
    }
}
