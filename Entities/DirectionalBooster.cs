using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/DirectionalBooster")]
    public class DirectionalBooster : Booster {
        private readonly DynamicData boosterData;
        private readonly ParticleType appearParticleType;
        private readonly Sprite sprite;

        public DirectionalBooster(EntityData data, Vector2 offset) : base(data.Position + offset, true) {
            boosterData = new DynamicData(this);

            // replace sprite
            Remove(boosterData.Get<Sprite>("sprite"));
            sprite = StrawberryJam2021Module.SpriteBank.Create("directionalBooster");
            boosterData.Set("sprite", sprite);
            Add(sprite);

            // replace particle type
            var boosterColor = data.HexColor("boosterColor", Calc.HexToColor("e6a434"));
            boosterData.Set("particleType", new ParticleType(P_BurstRed) { Color = boosterColor, Color2 = boosterColor });
            appearParticleType = new ParticleType(P_RedAppear) { Color = boosterColor, Color2 = boosterColor };
        }

        private static string animForDirection(Vector2 direction) {
            int xDir = Math.Abs(direction.X) < 0.1 ? 0 : Math.Sign(direction.X);
            int yDir = Math.Abs(direction.Y) < 0.1 ? 0 : Math.Sign(direction.Y);

            if (xDir == 0) {
                if (yDir == -1) {
                    return "spin_up";
                }

                if (yDir == 1) {
                    return "spin_down";
                }

                return "";
            }

            if (yDir == -1) {
                return "spin_upright";
            }

            if (yDir == 1) {
                return "spin_downright";
            }

            return "spin_right";
        }

        public static void Load() {
            On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
            IL.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
            IL.Celeste.Booster.AppearParticles += Booster_AppearParticles;
        }

        public static void Unload() {
            On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
            IL.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
            IL.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
        }

        private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction) {
            orig(self, player, direction);
            if (self is DirectionalBooster directionalBooster) {
                directionalBooster.sprite.FlipX = direction.X < 0.1f;
            }
        }

        private static void Booster_PlayerBoosted(ILContext il) {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdstr("spin"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<string, Booster, Vector2, string>>((anim, self, direction) =>
                self is not DirectionalBooster ? anim : animForDirection(direction));
        }

        private static void Booster_AppearParticles(ILContext il) {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdsfld<Booster>(nameof(P_RedAppear)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<ParticleType, Booster, ParticleType>>((type, self) =>
                self is DirectionalBooster directionalBooster ? directionalBooster.appearParticleType : type);
        }
    }
}