using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Reflection;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

        [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {
        int NumberOfDashes = 0;
        private static bool IsInCurrentMap = false;
        private static int NormalDashAmount = 0;
        private static bool ResetOnDeath = false;
        bool ResetOnDeathPrivate = false;
        private static Player player;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes");
            ResetOnDeathPrivate = data.Bool("ResetOnDeath");
        }

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            if (self.Entity is Player) {
                player = (Player) self.Entity;
                if (player.Dashes > 0 && player != null && IsInCurrentMap) {
                    return Player.NormalHairColor;
                } else {
                    return orig(self, 1);
                }
            }
            else {
                return orig(self, 1);
            }
        }

        private static Color modPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (self.Dashes > 0 && IsInCurrentMap) {
                return Player.NormalHairColor;

            } else {
                return orig(self);
            }
               
        }
        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
        }

        public static void Load() {
            using (new DetourContext { After = { "*" } }) { 
                On.Celeste.PlayerHair.GetHairColor += modPlayerGetHairColor;
                On.Celeste.Player.GetCurrentTrailColor += modPlayerGetTrailColor;
                On.Celeste.Player.Die += modDie;
            }
            Everest.Events.Level.OnExit += modOnExit;
            Everest.Events.Player.OnDie += modPlayerdie;
        }
        public static void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor -= modPlayerGetTrailColor;
            Everest.Events.Player.OnDie -= modPlayerdie;
            On.Celeste.Player.Die -= modDie;
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
            NormalDashAmount = SceneAs<Level>().Session.Inventory.Dashes;
            SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
            player.Dashes = NumberOfDashes;
            ResetOnDeath = ResetOnDeathPrivate;
            RemoveSelf();
        }
    }
}