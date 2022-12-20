using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021 {
    internal static class TogglePlaybackHandler {
        public static void Load() {
            On.Celeste.PlayerPlayback.ctor_EntityData_Vector2 += PlayerPlayback_ctor_EntityData_Vector2;
        }
        public static void Unload() {
            On.Celeste.PlayerPlayback.ctor_EntityData_Vector2 -= PlayerPlayback_ctor_EntityData_Vector2;
        }

        private static void PlayerPlayback_ctor_EntityData_Vector2(On.Celeste.PlayerPlayback.orig_ctor_EntityData_Vector2 orig, PlayerPlayback self, EntityData e, Vector2 offset) {
            orig(self, e, offset);
            self.PreUpdate += _PreUpdate;
        }

        //Is always called regardless of Active state so this just works.
        private static void _PreUpdate(Entity obj) {
            if (StrawberryJam2021Module.Settings.TogglePlaybacks.Released) {
                if (obj.Active) {
                    obj.Active = false;
                    obj.Visible = false;
                } else {
                    obj.Active = true;
                    (obj as PlayerPlayback).Restart();
                }
            }
        }
    }
}
