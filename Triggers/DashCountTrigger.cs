﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using System;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {
        private static Player player;
        private static bool IsInCurrentMap = false;
        private static int NormalDashAmount = 1;
        private static bool ResetOnDeath = false;
        int NormalDashAmountprivate = 1;
        int NumberOfDashes = 1;
        bool ResetOnDeathPrivate = false;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes",1);
            NormalDashAmountprivate = data.Int("DashAmountOnReset",1);
            ResetOnDeathPrivate = data.Bool("ResetOnDeath");
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            IsInCurrentMap = true;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            DashCountTrigger.player = player;
            SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
            player.Dashes = NumberOfDashes;
            ResetOnDeath = ResetOnDeathPrivate;
            NormalDashAmount = NormalDashAmountprivate;
            RemoveSelf();
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

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            if (self.Entity is Player player2) {
                player = player2;
                if (player.Dashes > 0 && IsInCurrentMap) {
                    return Player.NormalHairColor;
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

        private static PlayerDeadBody modDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
            if (IsInCurrentMap) {
                PlayerDeadBody Deadbody = orig(self, Vector2.Zero, false, false);
                if (player.Dashes > 0) {
                    new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.NormalHairColor;
                } else {
                    new DynData<PlayerDeadBody>(Deadbody)["initialHairColor"] = Player.UsedHairColor;
                }
                return Deadbody;
            }
            return orig(self, direction, false, false);
        }

        private static void modDraw(On.Celeste.DeathEffect.orig_Draw orig, Vector2 position, Color color, float ease) {
            if (IsInCurrentMap) {
                if (player.Dashes > 0) {
                    color = Player.NormalHairColor;
                } else {
                    color = Player.UsedHairColor;
                }
            }
            orig(position, color, ease);
        }

        private static void modOnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            IsInCurrentMap = false;
        }

        private static void modPlayerdie(global::Celeste.Player player) {
            if (ResetOnDeath && IsInCurrentMap) {
                player.SceneAs<Level>().Session.Inventory.Dashes = NormalDashAmount;
                player.Dashes = NormalDashAmount;
            }
        }
    }
}
