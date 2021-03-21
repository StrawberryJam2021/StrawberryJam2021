using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

        [CustomEntity("SJ2021/DashCountTrigger")]
    [Tracked]
    public class DashCountTrigger : Trigger {

        private static bool HasCollided = false;
        int NumberOfDashes = 0;
        private static bool IsInCurrentMap = false;

        public DashCountTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            NumberOfDashes = data.Int("NumberOfDashes");
        }

        private static Color modPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            if (self.Entity is Player) {
                Player player = (Player) self.Entity;
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
            }
            Everest.Events.Level.OnExit += modOnExit;
        }
        public static void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= modPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor -= modPlayerGetTrailColor;
            Everest.Events.Level.OnExit -= modOnExit;
        }

        private static void modOnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            IsInCurrentMap = false;
        }
        public override void Added(Scene scene) {
            base.Added(scene);
            IsInCurrentMap = true;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (!HasCollided) {
                SceneAs<Level>().Session.Inventory.Dashes = NumberOfDashes;
                player.Dashes = NumberOfDashes;
                HasCollided = true;
            }
        }
    }
}