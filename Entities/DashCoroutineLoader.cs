using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public static class DashCoroutineLoader {
		private static readonly Type playerType = typeof(Player);
        private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
		private static readonly MethodInfo CorrectDashPrecision = playerType.GetMethod("CorrectDashPrecision", flags);
        private static readonly MethodInfo CallDashEvents = playerType.GetMethod("CallDashEvents", flags);
        private static readonly MethodInfo CreateTrail = playerType.GetMethod("CreateTrail", flags);
		//private static readonly MethodInfo DashCoroutine = playerType.GetMethod("DashCoroutine", flags);
		private static IDetour hook_Player_DashCoroutine;
		private static MethodInfo m = typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();


		public static void Load() {
            On.Celeste.Player.DashCoroutine += ModDashCoroutine;
            //IL.Celeste.Player.DashBegin += modDashLength;
            //IL.Celeste.Player.DashCoroutine += modDashLength;
            //hook_Player_DashCoroutine = new ILHook(m, ModDashSpeed);
			//hook_Player_DashCoroutine = new ILHook(m, (il) => PlayerDashCoroutine(m.DeclaringType.GetField("<>4__this"), il));
		}

		//public static void Unload() {
		//	hook_Player_DashCoroutine.Dispose();
  //      }

		//private static void PlayerDashCoroutine(FieldInfo playerFieldInfo, ILContext il) {
		//	ILCursor cursor = new ILCursor(il);

		//	cursor.Emit(OpCodes.Ldarg_0);
		//	cursor.Emit(OpCodes.Ldfld, playerFieldInfo);

		//	while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(1.2f))) {
		//		cursor.EmitDelegate<Func<float>>(Ten);
		//		cursor.Emit(OpCodes.Mul);
		//	}
		//}

		//private static float Ten() {
		//	return 10f;
		//}

        //Hooks into Player::DashCoroutine to implement changes with NewToggleSwapBlock, written by @Viv#1113
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
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBneUn(out VanillaTarget), //VanillaTarget is now well-defined
                instr => instr.MatchLdloc(out playerIndex), // playerIndex is now well-defined
                instr => instr.MatchLdfld<Player>("StateMachine"), instr => instr.MatchLdcI4(1))) //Remaining checks to confirm we're in the correct position.
                                                                                                  //Cursor is now directly before, or "at" the instruction `bne.un.s IL_036f`
            {
                if (VanillaTarget != null) {
                    cursor.GotoLabel(VanillaTarget, MoveType.Before);
                    //cursor is now exactly at IL_036f and any code written here will come, crucially, *before* ldarg.0, so any branches to IL_036f will actually branch to our code first.
                    ILCursor cursor2 = cursor.Clone();
                    //We want to clone the cursor and leave our first cursor here to operate later.
                    //this is a good practice of making a "safe" hook, only hooking once you know you have all the materials to cleanly hook.
                    if (cursor2.TryGotoNext(instr => instr.MatchCall<Vector2>("get_One"), instr => instr.MatchStfld(out FieldReference _))) {
                        cursor2.Index++; //Move After Vector2::get_One()

                        //It's hooking time babey
                        cursor.Emit(OpCodes.Ldloc, playerIndex); // Add Player to Stack
                        cursor.EmitDelegate<Func<Player, bool>>(CheckForNewToggleSwapBlocks); // check the code with NewToggleSwapBlocks in mind
                        cursor.Emit(OpCodes.Brtrue, VanillaTarget); // If player Collides with a NewToggleSwapBlock with direction matching the sign of player Dash Direction, jump back to the code inside the if statement

                        cursor2.Emit(OpCodes.Ldloc, playerIndex); // Add Player to Stack
                        cursor2.EmitDelegate<Func<Vector2, Player, Vector2>>(ModifyDashSpeedWithSwapBlock); //Modify the code with our NewToggleSwapBlocks
                    }

                }
            }
        }

        private static bool CheckForNewToggleSwapBlocks(Player player) {
            if (player.DashDir.X != 0f && Input.GrabCheck)
                return false; // We wanna get rid of this case because it's the initial case that we dont wanna worry about.
            NewToggleSwapBlock ntsb = player.CollideFirst<NewToggleSwapBlock>(player.Position + Vector2.UnitX * Math.Sign(player.DashDir.X)); //Same thing as the SwapBlock but with NewToggleSwapBlock
            return ntsb != null && ntsb.dirVector.X == (float) Math.Sign(player.DashDir.X); //if this is true then brtrue will pass it back to the inside of the if statement
        }

        //Important detail! Since swapCancel's X and Y values are 1 and 0 only we can do this. Normally we wouldn't be allowed to do this.
        private static Vector2 ModifyDashSpeedWithSwapBlock(Vector2 orig, Player player) {
            Vector2 swapCancel = orig;
            foreach (NewToggleSwapBlock entity in player.Scene.Tracker.GetEntities<NewToggleSwapBlock>()) {
                if (player.CollideCheck(entity, player.Position + Vector2.UnitY) && entity != null && entity.moving) {
                    if (player.DashDir.X != 0f && Math.Sign(entity.dirVector.X) == Math.Sign(player.DashDir.X)) {
                        player.Speed.X = (swapCancel.X = 0f);
                    }
                    if (player.DashDir.Y != 0f && Math.Sign(entity.dirVector.Y) == Math.Sign(player.DashDir.Y)) {
                        player.Speed.Y = (swapCancel.Y = 0f);
                    }
                }
            }
            return swapCancel;
        }

        private static IEnumerator ModDashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player self) {
            yield return null;

            DynData<Player> data = new DynData<Player>(self);

            Vector2 beforeDashSpeed = data.Get<Vector2>("beforeDashSpeed");
            Level level = data.Get<Level>("level");

            if (SaveData.Instance.Assists.DashAssist) {
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            }
            level.Displacement.AddBurst(self.Center, 0.4f, 8f, 64f, 0.5f, Ease.QuadOut, Ease.QuadOut);
            Vector2 value = data.Get<Vector2>("lastAim");
            if (self.OverrideDashDirection.HasValue) {
                value = self.OverrideDashDirection.Value;
            }
            if (CorrectDashPrecision == null) {
                ((object) null).GetType();
            }
            value = (Vector2) CorrectDashPrecision.Invoke(self, new object[] { value });
            Vector2 speed = value * 240f;
            if (Math.Sign(beforeDashSpeed.X) == Math.Sign(speed.X) && Math.Abs(beforeDashSpeed.X) > Math.Abs(speed.X)) {
                speed.X = beforeDashSpeed.X;
            }
            self.Speed = speed;
            if (self.CollideCheck<Water>()) {
                self.Speed *= 0.75f;
            }
            self.DashDir = value;
            data.Set("gliderBoostDir", value);
            self.SceneAs<Level>().DirectionalShake(self.DashDir, 0.2f);
            if (self.DashDir.X != 0f) {
                self.Facing = (Facings) Math.Sign(self.DashDir.X);
            }
            CallDashEvents.Invoke(self, new object[] { });
            if (self.StateMachine.PreviousState == 19) {
                level.Particles.Emit(FlyFeather.P_Boost, 12, self.Center, Vector2.One * 4f, (-value).Angle());
            }
            if (data.Get<bool>("onGround") && self.DashDir.X != 0f && self.DashDir.Y > 0f && self.Speed.Y > 0f && (!self.Inventory.DreamDash || !self.CollideCheck<DreamBlock>(self.Position + Vector2.UnitY))) {
                self.DashDir.X = Math.Sign(self.DashDir.X);
                self.DashDir.Y = 0f;
                self.Speed.Y = 0f;
                self.Speed.X *= 1.2f;
                self.Ducking = true;
            }
            SlashFx.Burst(self.Center, self.DashDir.Angle());
            CreateTrail.Invoke(self, new object[] { });
            if (SaveData.Instance.Assists.SuperDashing) {
                data.Set("dashTrailTimer", 0.1f);
                data.Set("dashTrailCounter", 2);
            } else {
                data.Set("dashTrailTimer", 0.08f);
                data.Set("dashTrailCounter", 1);
            }

            if (self.DashDir.X != 0f && Input.GrabCheck) {
                SwapBlock swapBlock = self.CollideFirst<SwapBlock>(self.Position + Vector2.UnitX * Math.Sign(self.DashDir.X));
                if (swapBlock != null && swapBlock.Direction.X == (float) Math.Sign(self.DashDir.X)) {
                    self.StateMachine.State = 1;
                    self.Speed = Vector2.Zero;
                    yield break;
                }

                NewToggleSwapBlock newToggleSwapBlock = self.CollideFirst<NewToggleSwapBlock>(self.Position + Vector2.UnitX * Math.Sign(self.DashDir.X));
                if (newToggleSwapBlock != null && Math.Sign(newToggleSwapBlock.dirVector.X) == Math.Sign(self.DashDir.X)) {
                    self.StateMachine.State = 1;
                    self.Speed = Vector2.Zero;
                    yield break;
                }
            }

            Vector2 swapCancel = Vector2.One;
            foreach (SwapBlock entity in self.Scene.Tracker.GetEntities<SwapBlock>()) {
                if (self.CollideCheck(entity, self.Position + Vector2.UnitY) && entity != null && entity.Swapping) {
                    if (self.DashDir.X != 0f && entity.Direction.X == (float) Math.Sign(self.DashDir.X)) {
                        self.Speed.X = (swapCancel.X = 0f);
                    }
                    if (self.DashDir.Y != 0f && entity.Direction.Y == (float) Math.Sign(self.DashDir.Y)) {
                        self.Speed.Y = (swapCancel.Y = 0f);
                    }
                }
            }
            foreach (NewToggleSwapBlock entity in self.Scene.Tracker.GetEntities<NewToggleSwapBlock>()) {
                if (self.CollideCheck(entity, self.Position + Vector2.UnitY) && entity != null && entity.moving) {
                    if (self.DashDir.X != 0f && Math.Sign(entity.dirVector.X) == Math.Sign(self.DashDir.X)) {
                        self.Speed.X = (swapCancel.X = 0f);
                    }
                    if (self.DashDir.Y != 0f && Math.Sign(entity.dirVector.Y) == Math.Sign(self.DashDir.Y)) {
                        self.Speed.Y = (swapCancel.Y = 0f);
                    }
                }
            }

            if (SaveData.Instance.Assists.SuperDashing) {
                yield return 0.3f;
            } else {
                yield return 0.15f;
            }

            CreateTrail.Invoke(self, new object[] { });
            self.AutoJump = true;
            self.AutoJumpTimer = 0f;
            if (self.DashDir.Y <= 0f) {
                self.Speed = self.DashDir * 160f;
                self.Speed.X *= swapCancel.X;
                self.Speed.Y *= swapCancel.Y;
            }
            if (self.Speed.Y < 0f) {
                self.Speed.Y *= 0.75f;
            }
            self.StateMachine.State = 0;
        }
    }
}
