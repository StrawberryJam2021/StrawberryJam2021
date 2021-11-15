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
            hook_Player_DashCoroutine.Dispose();
        }

        private static void ModDashSpeed(ILContext il) {
            ILCursor cursor = new ILCursor(il); //Creates a new cursor to read through the Instruction list from
                                                //We don't need any Variable Definitions because I found a very cheeky solution
                                                //The next comment represents the code that we are changing in IL and exactly how. comment lines starting with ////PATCH//// refer to any additional IL patched in by the cursor.
            /* 
////CODE IN C#////// if (swapBlock != null && swapBlock.Direction.X == (float)Math.Sign(player.DashDir.X))
                ldloc.s 5  // put swapBlock on stack
                brfalse.s IL_036f // if swapBlock doesn't have a value assigned to it == if swapBlock *is* null, then we jump to the instructions after the closing brackets, starting at ldarg.0 below.
 
                ldloc.s 5 // put swapBlock on stack again, running brfalse "uses up" the swapBlock emitted earlier.
                ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Celeste.SwapBlock::Direction // remove swapBlock from stack and retrieve its Direction Vector2.
                ldfld float32 [FNA]Microsoft.Xna.Framework.Vector2::X // remove Direction and simply get the X value.
                ldloc.1  // get Player
                ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Celeste.Player::DashDir // remove Player, retrieve Player's dash direction
                ldfld float32 [FNA]Microsoft.Xna.Framework.Vector2::X // remove DashDir and get the X component of DashDir
                call int32 [mscorlib]System.Math::Sign(float32) // get the Sign (+1 or -1) of the X component of DashDir
                conv.r4 // Convert to float (radix-point, 4 bytes)
                bne.un.s IL_036f // If the two values on the stack (SwapBlock.Direction.X and Math.Sign(Player.DashDir.X)) are *not* equal, then we jump to the instructions after the closing brackets.
 
//////////////////// Our Target ////////////////////////////////////////////////
                // player.StateMachine.State = 1;
                IL_0356: ldloc.1
                ...
                // player.Speed = Vector2.Zero; # We don't really care about this code so I'm gonna skip it to clean up this documentation a bit.
                // return false;
                ...
                IL_036e: ret
 
////Vanilla Target////  IL_036f: 
////PATCH////   // NewToggleSwapBlock.CheckForNewToggleSwapBlocks(Player player)        
////PATCH////   ldloc.1
////PATCH////   call ReferenceBag Delegate !!!! this means we use an EmitDelegate<Func<Player, bool>>(CheckForNewToggleSwapBlocks);
////PATCH////   brtrue <Our Target> (IL_0356 above)
///Vanilla code/// <swapCancel>5__2 = Vector2.One; ||||||| PATCH TO: <swapCancel>5__2 = NewToggleSwapBlock.ModifyDashSpeedWithSwapBlock(Vector2 vector, Player player) with stack (Vector2.get_One(), player)
                ldarg.0  
                call valuetype [FNA]Microsoft.Xna.Framework.Vector2 [FNA]Microsoft.Xna.Framework.Vector2::get_One()
            ///PATCH POINT OF CURSOR2///
////PATCH////   ldloc.1 //Get Player
////PATCH////   call ReferenceBag Delegate !!!! this means we use an EmitDelegate<Func<Vector2, Player, Vector2>>(ModifyDashSpeedWithSwapBlock);
                IL_0375: stfld valuetype [FNA]Microsoft.Xna.Framework.Vector2 Celeste.Player/'<DashCoroutine>d__427'::'<swapCancel>5__2'
             */

            //The process: First we get our cursors in position and retrieve our ILLabel to which we will branch to
            int playerIndex = 1; //The assumed value is first.
            ILLabel VanillaTarget = null;
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchBneUn(out VanillaTarget), //VanillaTarget is now well-defined
                instr => instr.MatchLdloc(out playerIndex), // playerIndex is now well-defined
                instr => instr.MatchLdfld<Player>("StateMachine"), instr => instr.MatchLdcI4(1)) //Remaining checks to confirm we're in the correct position.
                && VanillaTarget != null)                                                         //Cursor is now directly before, or "at" the instruction `bne.un.s IL_036f`
            {
                cursor.Index++; //Move after bne.un.s
                ILLabel OurTarget = cursor.MarkLabel();
                cursor.GotoLabel(VanillaTarget, MoveType.Before);
                //cursor is now exactly at IL_036f and any code written here will come, crucially, *before* ldarg.0, so any branches to IL_036f will actually branch to our code first.
                MethodInfo getOne = typeof(Vector2).GetProperty("One", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
                if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall(getOne))) {

                    //It's hooking time babey
                    cursor.Emit(OpCodes.Ldloc, playerIndex); // Add Player to Stack
                    cursor.EmitDelegate<Func<Player, bool>>(CheckForNewToggleSwapBlocks); // check the code with NewToggleSwapBlocks in mind
                    cursor.Emit(OpCodes.Brtrue, OurTarget); // If player Collides with a NewToggleSwapBlock with direction matching the sign of player Dash Direction, jump back to the code inside the if statement

                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldloc, playerIndex); // Add Player to Stack
                    cursor.EmitDelegate<Func<Vector2, Player, Vector2>>(ModifyDashSpeedWithSwapBlock); //Modify the code with our NewToggleSwapBlocks
                }
            }
        }

        private static bool CheckForNewToggleSwapBlocks(Player player) {
            if (!(player.DashDir.X != 0f && Input.GrabCheck))
                return false; // We wanna get rid of this case because it's the initial case that we dont wanna worry about.
            NewToggleSwapBlock ntsb = player.CollideFirst<NewToggleSwapBlock>(player.Position + Vector2.UnitX * Math.Sign(player.DashDir.X)); //Same thing as the SwapBlock but with NewToggleSwapBlock
            return ntsb != null && !ntsb.allowDashSliding && Math.Sign(ntsb.Direction.X) == Math.Sign(player.DashDir.X); //if this is true then brtrue will pass it back to the inside of the if statement
        }

        //Important detail! Since swapCancel's X and Y values are 1 and 0 only we can do this. Normally we wouldn't be allowed to do this.
        private static Vector2 ModifyDashSpeedWithSwapBlock(Vector2 orig, Player player) {
            Vector2 swapCancel = orig;
            foreach (NewToggleSwapBlock entity in player.Scene.Tracker.GetEntities<NewToggleSwapBlock>()) {
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
