using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/DashBoostField")]
    [Tracked]
    public class DashBoostField : Entity {
        public Modes Mode;
        public float DashSpeedMult;
        public float TargetTimeRateMult;
        public float Radius;

        public static float CurrentTimeRateMult;

        private Image boostFieldTexture;

        public const Modes DefaultMode = Modes.Blue;
        public const float DefaultDashSpeedMult = 1.7f;
        public const float DefaultTimeRateMult = 0.65f;
        public const float DefaultRadius = 1.5f;

        private static BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static MethodInfo dashCoroutineInfo = typeof(Player).GetMethod("DashCoroutine", privateInstance).GetStateMachineTarget();
        private static ILHook dashCoroutineHook;
        
        public DashBoostField(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Mode = data.Enum("mode", DefaultMode);
            DashSpeedMult = data.Float("dashSpeedMultiplier", DefaultDashSpeedMult);
            TargetTimeRateMult = data.Float("timeRateMultiplier", DefaultTimeRateMult);
            Radius = data.Float("radius", DefaultRadius) * 8;

            Depth = Depths.Above;
            Collider = new Circle(Radius);
            string textureColor = Mode == Modes.Blue ? "blue" : "red";
            Add(boostFieldTexture = new Image(GFX.Game[$"objects/StrawberryJam2021/dashBoostField/{textureColor}"]));
            boostFieldTexture.CenterOrigin();
            Color lightColor = Mode == Modes.Blue ? Calc.HexToColor("e0e0ff") : Calc.HexToColor("ffe0e0");
            Add(new VertexLight(lightColor, 1f, 16, 32));
            Add(new DashBoostFieldParticleRenderer());
        }

        public enum Modes {
            Blue,
            Red
        }

        public static void Load() {
            IL.Celeste.Level.Update += IL_Level_Update;
            On.Celeste.Player.Update += On_Player_Update;
            On.Celeste.Player.UnderwaterMusicCheck += On_Player_UnderwaterMusicCheck;
            On.Celeste.Player.DashBegin += On_Player_DashBegin;
            dashCoroutineHook = new ILHook(dashCoroutineInfo, IL_Player_DashCoroutine);
            IL.Celeste.Player.SuperWallJump += IL_Player_SuperWallJump;
            IL.Celeste.Player.SuperJump += IL_Player_SuperJump;
            IL.Celeste.Player.DreamDashBegin += IL_Player_DreamDashBegin;
            On.Celeste.Player.Die += On_Player_Die;
        }

        public static void Unload() {
            IL.Celeste.Level.Update -= IL_Level_Update;
            On.Celeste.Player.Update -= On_Player_Update;
            On.Celeste.Player.UnderwaterMusicCheck -= On_Player_UnderwaterMusicCheck;
            On.Celeste.Player.DashBegin -= On_Player_DashBegin;
            dashCoroutineHook?.Dispose();
            IL.Celeste.Player.SuperWallJump -= IL_Player_SuperWallJump;
            IL.Celeste.Player.SuperJump -= IL_Player_SuperJump;
            IL.Celeste.Player.DreamDashBegin -= IL_Player_DreamDashBegin;
            On.Celeste.Player.Die -= On_Player_Die;
        }

        private static float ModifyTimeRate(float timeRate) {
            if (!(Engine.Scene as Level).Paused) {
                timeRate *= CurrentTimeRateMult;
            }
            return timeRate;
        }

        private static void IL_Level_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // modify TimeRateB so we get audio slowing
            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(10f),
                instr => instr.MatchDiv())) {
                cursor.EmitDelegate<Func<float, float>>(ModifyTimeRate);
            }
        }

        private static bool IsDashingOrRespawning(Player player) {
            int state = player.StateMachine.State;
            return state == Player.StDash || state == Player.StIntroRespawn;
        }

        private static void On_Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            bool wasDashing = self.DashAttacking || self.StateMachine.State == Player.StDash;
            orig(self);
            // having the player itself handle collision is nicer
            DashBoostField boostField = self.CollideFirst<DashBoostField>();
            if (!self.Dead && boostField != null && !IsDashingOrRespawning(self))
                CurrentTimeRateMult = boostField.TargetTimeRateMult;
            else
                CurrentTimeRateMult = 1f;

            // the last dash has ended so this should definitely be reset
            if (wasDashing && !(self.DashAttacking || self.StateMachine.State == Player.StDash)) {
                DynamicData playerData = new DynamicData(self);
                playerData.Set("dashBoosted", false);
            }
        }

        private static bool On_Player_UnderwaterMusicCheck(On.Celeste.Player.orig_UnderwaterMusicCheck orig, Player self) {
            if (self.CollideCheck<DashBoostField>() && !IsDashingOrRespawning(self))
                return true;
            return orig(self);
        }

        private static void On_Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self) {
            DynamicData playerData = new DynamicData(self);
            DashBoostField boostField = self.CollideFirst<DashBoostField>();
            if (boostField != null) {
                playerData.Set("dashBoosted", true);
                playerData.Set("dashBoostSpeed", boostField.DashSpeedMult);
                self.Add(new Coroutine(RefillDashIfRedDashBoost(self)));
            } else {
                // just to be on the safe side
                playerData.Set("dashBoosted", false);
            }
            orig(self);
        }

        private static T SafeGet<T>(DynamicData data, string key, T defaultValue = default) where T : struct {
            T? value = data.Get<T?>(key);
            if (value != null) {
                return (T) value;
            } else {
                return defaultValue;
            }
        }

        private static float ModifySpeed(float speed, Player player) {
            DynamicData playerData = new DynamicData(player);
            if (SafeGet(playerData, "dashBoosted", defaultValue: false)) {
                speed *= SafeGet(playerData, "dashBoostSpeed", defaultValue: 1f);
            }
            return speed;
        }

        private static Vector2 ModifySpeed(Vector2 speed, Player player) {
            DynamicData playerData = new DynamicData(player);
            if (SafeGet(playerData, "dashBoosted", defaultValue: false)) {
                speed *= SafeGet(playerData, "dashBoostSpeed", defaultValue: 1f);
            }
            return speed;
        }

        private static void IL_Player_DashCoroutine(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            FieldInfo f_this = dashCoroutineInfo.DeclaringType.GetField("<>4__this");
            // right as Speed is set for the first time
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStfld<Player>("Speed"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, f_this);
                cursor.EmitDelegate<Func<Vector2, Player, Vector2>>(ModifySpeed);
            }
        }

        private static IEnumerator RefillDashIfRedDashBoost(Player player) {
            DashBoostField boostField = player.CollideFirst<DashBoostField>();
            if (boostField?.Mode == Modes.Red) {
                // wait a frame so that the player's speed will be correct
                yield return null;
                player.Dashes += 1;
                Audio.Play(SFX.game_gen_diamond_touch);
                Level level = player.SceneAs<Level>();
                float angle = player.Speed.Angle();
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, player.Position, Vector2.One * 4f, angle - (float) Math.PI / 2f);
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, player.Position, Vector2.One * 4f, angle + (float) Math.PI / 2f);
            }
        }

        private static void IL_Player_SuperWallJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // uncomment to boost X value as well
            // if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(170f))) {
            //     cursor.Emit(OpCodes.Ldarg_0);
            //     cursor.EmitDelegate<Func<float, Player, float>>(ModifySpeed);
            // }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-160f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>(ModifySpeed);
            }
        }

        private static void IL_Player_SuperJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(260f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>(ModifySpeed);
            }
        }

        private static void IL_Player_DreamDashBegin(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(240f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>(ModifySpeed);
            }
        }

        private static PlayerDeadBody On_Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
            CurrentTimeRateMult = 1f;
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }
    }
}
