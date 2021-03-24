using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using System;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {
        int NumberOfDashes = 0;
        private static bool IsInCurrentMap = false;
        private static int NormalDashAmount = 1;
        int NormalDashAmountprivate = 1;
        private static bool ResetOnDeath = false;
        bool ResetOnDeathPrivate = false;
        private static Player player;
        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes");
            NormalDashAmountprivate = data.Int("DashAmountOnReset");
            ResetOnDeathPrivate = data.Bool("ResetOnDeath");
        }

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            if (self.Entity is Player player2) {
                player = player2;
                if (player.Dashes > 0 && player != null && IsInCurrentMap) {
                    return Player.NormalHairColor;
                } else {
                    return orig(self, 1);
                }
            }
            return orig(self, 1);
        }

        private static Color modPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (self.Dashes > 0 && IsInCurrentMap) {
                return Player.NormalHairColor;

            } else {
                return orig(self);
            } 
        }

        public static void Load() {
            using (new DetourContext { After = { "*" } }) { 
                On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
                On.Celeste.Player.GetCurrentTrailColor += modPlayerGetTrailColor;
                On.Celeste.Player.Die += modDie;
            }
            Everest.Events.Level.OnExit += modOnExit;
            Everest.Events.Player.OnDie += modPlayerdie;
            On.Celeste.DeathEffect.Draw += modDraw;
        }

        public static void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor -= modPlayerGetTrailColor;
            Everest.Events.Player.OnDie -= modPlayerdie;
            On.Celeste.Player.Die -= modDie;
            On.Celeste.DeathEffect.Draw -= modDraw;
        }

        private static PlayerDeadBody modDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
            if (IsInCurrentMap) {
                PlayerDeadBody Deadbody = orig(self, Vector2.Zero, false, false);
                if (player.Dashes > 0) {
                    new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.NormalHairColor;
                } else {
                    new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.UsedHairColor;
                }
                return Deadbody;

            } else
                return orig(self, Vector2.Zero, false, false);
        }

        private static void modDraw(On.Celeste.DeathEffect.orig_Draw orig, Vector2 position, Color color, float ease) {
            if (IsInCurrentMap) {
                if (player.Dashes >= 0) {
                    color = Player.NormalHairColor;
                } else {
                    color = Player.UsedHairColor;
                }
                Color color2 = ((Math.Floor(ease * 10f) % 2.0 == 0.0) ? color : Color.White);
                MTexture mTexture = GFX.Game["characters/player/hair00"];
                float num = ((ease < 0.5f) ? (0.5f + ease) : Ease.CubeOut(1f - (ease - 0.5f) * 2f));
                for (int i = 0; i < 8; i++) {
                    Vector2 value = Calc.AngleToVector(((float) i / 8f + ease * 0.25f) * ((float) Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                    mTexture.DrawCentered(position + value + new Vector2(-1f, 0f), Color.Black, new Vector2(num, num));
                    mTexture.DrawCentered(position + value + new Vector2(1f, 0f), Color.Black, new Vector2(num, num));
                    mTexture.DrawCentered(position + value + new Vector2(0f, -1f), Color.Black, new Vector2(num, num));
                    mTexture.DrawCentered(position + value + new Vector2(0f, 1f), Color.Black, new Vector2(num, num));
                }
                for (int j = 0; j < 8; j++) {
                    Vector2 value2 = Calc.AngleToVector(((float) j / 8f + ease * 0.25f) * ((float) Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                    mTexture.DrawCentered(position + value2, color2, new Vector2(num, num));
                }
            }
        }

        private static void modOnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            IsInCurrentMap = false;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            IsInCurrentMap = true;
        }

        private static void modPlayerdie(global::Celeste.Player player) {
            if (ResetOnDeath) {
                player.SceneAs<Level>().Session.Inventory.Dashes = NormalDashAmount;
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            player = player;
            SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
            player.Dashes = NumberOfDashes;
            ResetOnDeath = ResetOnDeathPrivate;
            NormalDashAmount = NormalDashAmountprivate;
            RemoveSelf();
        }
    }
}