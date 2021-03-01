using Celeste.Mod.CavernHelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CrystalBombBadelineBoss")]
    [Tracked]
    public class CrystalBombBadelineBoss : FinalBoss {
        private DynamicData baseData;
        private Action<Player> base_OnPlayer;

        private Circle playerCollider;
        // be more lenient with death hitbox
        private const float playerCollideRadius = 8f;

        private static Hook crystalBombExplodeHook;
        private static MethodInfo crystalBombExplodeInfo = typeof(CrystalBomb).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo crystalBombExplodeHookInfo = typeof(CrystalBombBadelineBoss).GetMethod("On_CrystalBomb_Explode", BindingFlags.NonPublic | BindingFlags.Static);

        public CrystalBombBadelineBoss(EntityData data, Vector2 offset) : base(data, offset) {
            baseData = new DynamicData(typeof(FinalBoss), this);
            // store original OnPlayer method so we can call it later...
            base_OnPlayer = Get<PlayerCollider>().OnCollide;
            // ...and then replace it for our boss
            playerCollider = new Circle(playerCollideRadius, 0f, -6f);
            Remove(Get<PlayerCollider>());
            Add(new PlayerCollider(OnPlayer, playerCollider));
        }

        public override void Update() {
            base.Update();
            // don't try to visually avoid the player
            baseData.Set("avoidPos", Vector2.Zero);

            if (Collidable) {
                foreach (CrystalBomb bomb in SceneAs<Level>()?.Entities.FindAll<CrystalBomb>()) {
                    if (CollideCheck(bomb))
                        new DynamicData(bomb).Invoke("Explode");
                }
            }
        }

        private new void OnPlayer(Player player) {
            player.Die((player.Center - Center).SafeNormalize());
        }

        private void OnHit() {
            base_OnPlayer(null);
        }

        public static void Load() {
            crystalBombExplodeHook = new Hook(crystalBombExplodeInfo, crystalBombExplodeHookInfo);
            On.Celeste.Seeker.RegenerateCoroutine += On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode += On_Puffer_Explode;
        }

        public static void Unload() {
            crystalBombExplodeHook?.Dispose();
            On.Celeste.Seeker.RegenerateCoroutine -= On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode -= On_Puffer_Explode;
        }

        private static void On_CrystalBomb_Explode(Action<CrystalBomb> orig, CrystalBomb self) {
            DynamicData bombData = new DynamicData(self);
            if (bombData.Get<bool>("exploded"))
                return;
            Collider origCollider = self.Collider;
            self.Collider = bombData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = origCollider;
            orig(self);
        }

        private static IEnumerator On_Seeker_RegenerateCoroutine(On.Celeste.Seeker.orig_RegenerateCoroutine orig, Seeker self) {
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext()) {
                yield return origEnum.Current;
            }
            DynamicData seekerData = new DynamicData(self);
            Collider origCollider = self.Collider;
            self.Collider = seekerData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = origCollider;
        }

        private static void On_Puffer_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            orig(self);
            DynamicData pufferData = new DynamicData(self);
            Collider origCollider = self.Collider;
            self.Collider = pufferData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = origCollider;
        }
    }
}
