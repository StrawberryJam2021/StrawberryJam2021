using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {
        //the static ones are the active settings, and the non-static ones are the settings of the trigger
        private static bool IsInCurrentMap = false;
        private static int NormalDashAmount = 1;
        private static bool ResetOnDeath = false;
        int NormalDashAmountSetting = 1;
        int NumberOfDashesSetting = 1;
        bool ResetOnDeathSetting = false;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashesSetting = data.Int("NumberOfDashes",1);
            NormalDashAmountSetting = data.Int("DashAmountOnReset",1);
            ResetOnDeathSetting = data.Bool("ResetOnDeath");
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            IsInCurrentMap = true;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashesSetting;
            player.Dashes = NumberOfDashesSetting;
            ResetOnDeath = ResetOnDeathSetting;
            NormalDashAmount = NormalDashAmountSetting;
            RemoveSelf();
        }

        public static void Load() {
            using (new DetourContext { After = { "*" } }) {
                On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
                On.Celeste.Player.GetCurrentTrailColor += modPlayerGetTrailColor;
                On.Celeste.Player.Die += modDie;
            }
            Everest.Events.Level.OnExit += modOnExit;
            On.Celeste.DeathEffect.Draw += modDraw;
            On.Celeste.Level.LoadLevel += modPlayerRespawn;
        }

        public static void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor -= modPlayerGetTrailColor;
            On.Celeste.Player.Die -= modDie;
            Everest.Events.Level.OnExit -= modOnExit;
            On.Celeste.DeathEffect.Draw -= modDraw;
            On.Celeste.Level.LoadLevel -= modPlayerRespawn;
        }

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            if (self.Entity is Player player) {
                if (IsInCurrentMap) {
                    if (player.Dashes > 0) {
                        return Player.NormalHairColor;
                    }
                    return Player.UsedHairColor;
                }
            }
            return orig(self, index);
        }

        private static Color modPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (self.Dashes > 0 && IsInCurrentMap) {
                return Player.NormalHairColor;
            } else {
                return orig(self);
            } 
        }

        private static PlayerDeadBody modDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
            if (IsInCurrentMap) {
                PlayerDeadBody Deadbody = orig(self, direction, evenIfInvincible, registerDeathInStats);
                if (Deadbody != null) {
                    if (self.Dashes > 0) {
                        new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.NormalHairColor;
                    } else {
                        new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.UsedHairColor;
                    }
                    
                }
                return Deadbody;
            }
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        private static void modDraw(On.Celeste.DeathEffect.orig_Draw orig, Vector2 position, Color color, float ease) {
            if (IsInCurrentMap) {
                Player player = Engine.Scene.Tracker.GetEntity<Player>();
                if(player != null) { 
                    if (player.Dashes > 0) {
                        color = Player.NormalHairColor;
                    } else {
                        color = Player.UsedHairColor;
                    }
                }
            }
            orig(position, color, ease);
        }

        private static void modOnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            IsInCurrentMap = false;
            ResetOnDeath = false;
        }

        private static void modPlayerRespawn(On.Celeste.Level.orig_LoadLevel orig, global::Celeste.Level level, global::Celeste.Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(level, playerIntro, isFromLoader);
            if (ResetOnDeath && IsInCurrentMap && playerIntro == Player.IntroTypes.Respawn) {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null) {
                    player.SceneAs<Level>().Session.Inventory.Dashes = NormalDashAmount;
                    player.Dashes = NormalDashAmount;
                }
            }
        }
    }
}