﻿using Celeste.Mod.CavernHelper;
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
    [CustomEntity("SJ2021/CrystalBombBadelineBoss")]
    [TrackedAs(typeof(FinalBoss))]
    public class CrystalBombBadelineBoss : FinalBoss {
        private string music;

        // be more lenient with death hitbox
        private const float playerCollideRadius = 8f;

        private static Hook crystalBombExplodeHook;
        private static MethodInfo crystalBombExplodeInfo = typeof(CrystalBomb).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo crystalBombExplodeHookInfo = typeof(CrystalBombBadelineBoss).GetMethod("On_CrystalBomb_Explode", BindingFlags.NonPublic | BindingFlags.Static);

        public CrystalBombBadelineBoss(EntityData data, Vector2 offset) : base(data, offset) {
            // Replace player collider
            Remove(Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayerCBBB, new Circle(playerCollideRadius, 0f, -6f)));

            music = data.Attr("music", "");
            if (data.Bool("disableCameraLock")) {
                Remove(Get<CameraLocker>());
            }
        }

        public override void Update() {
            base.Update();
            // don't try to visually avoid the player
            avoidPos = Vector2.Zero;

            if (Collidable) {
                foreach (CrystalBomb bomb in SceneAs<Level>()?.Entities.FindAll<CrystalBomb>()) {
                    if (CollideCheck(bomb))
                        DynamicData.For(bomb).Invoke("Explode");
                }
            }
        }

        private void OnPlayerCBBB(Player player) {
            player.Die((player.Center - Center).SafeNormalize());
        }

        private void OnHit() {
            OnPlayer(null);
        }

        public static void Load() {
            crystalBombExplodeHook = new Hook(crystalBombExplodeInfo, crystalBombExplodeHookInfo);
            On.Celeste.Seeker.RegenerateCoroutine += On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode += On_Puffer_Explode;
            IL.Celeste.FinalBoss.OnPlayer += IL_FinalBoss_OnPlayer;
            IL.Celeste.FinalBoss.CreateBossSprite += IL_FinalBoss_CreateBossSprite;
        }

        public static void Unload() {
            crystalBombExplodeHook?.Dispose();
            On.Celeste.Seeker.RegenerateCoroutine -= On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode -= On_Puffer_Explode;
            IL.Celeste.FinalBoss.OnPlayer -= IL_FinalBoss_OnPlayer;
            IL.Celeste.FinalBoss.CreateBossSprite -= IL_FinalBoss_CreateBossSprite;
        }

        private static void On_CrystalBomb_Explode(Action<CrystalBomb> orig, CrystalBomb self) {
            DynamicData bombData = DynamicData.For(self);
            if (bombData.Get<bool>("exploded"))
                return;
            Collider origCollider = self.Collider;
            self.Collider = bombData.Get<Circle>("pushRadius");
            foreach (FinalBoss boss in self.CollideAll<FinalBoss>()) {
                if (boss is CrystalBombBadelineBoss cbbb)
                    cbbb.OnHit();
            }
            self.Collider = origCollider;
            orig(self);
        }

        private static IEnumerator On_Seeker_RegenerateCoroutine(On.Celeste.Seeker.orig_RegenerateCoroutine orig, Seeker self) {
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext()) {
                yield return origEnum.Current;
            }
            Collider origCollider = self.Collider;
            self.Collider = self.pushRadius;
            foreach (FinalBoss boss in self.CollideAll<FinalBoss>()) {
                if (boss is CrystalBombBadelineBoss cbbb)
                    cbbb.OnHit();
            }
            self.Collider = origCollider;
        }

        private static void On_Puffer_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            orig(self);
            Collider origCollider = self.Collider;
            self.Collider = self.pushRadius;
            foreach (FinalBoss boss in self.CollideAll<FinalBoss>()) {
                if (boss is CrystalBombBadelineBoss cbbb)
                    cbbb.OnHit();
            }
            self.Collider = origCollider;
        }

        private static void IL_FinalBoss_OnPlayer(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("event:/music/lvl6/badeline_fight"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, FinalBoss, string>>(ChangeMusic);
            }
        }

        private static string ChangeMusic(string origMusic, FinalBoss self) {
            if (self is CrystalBombBadelineBoss crystalBoss && !string.IsNullOrEmpty(crystalBoss.music)) {
                return crystalBoss.music;
            } else {
                return origMusic;
            }
        }

        private static void IL_FinalBoss_CreateBossSprite(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchDup())) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Sprite, FinalBoss, Sprite>>(ChangeSprite);
                break;
            }
        }

        private static Sprite ChangeSprite(Sprite origSprite, FinalBoss self) {
            if (self is CrystalBombBadelineBoss) {
                return StrawberryJam2021Module.SpriteBank.Create("crystalBombBadelineBoss");
            } else {
                return origSprite;
            }
        }

    }
}
