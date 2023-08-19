using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/SpriteSwapTrigger")]
    public class SpriteSwapTrigger : Trigger {
        private static readonly Dictionary<string, string> spriteSwaps = new(StringComparer.OrdinalIgnoreCase);
        private static readonly char[] separators = { ',' };

        public SpriteSwapTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
        }

        internal static void Load() {
            Everest.Events.Level.OnExit += Level_OnExit;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Monocle.SpriteBank.Create += SpriteBank_Create;
            On.Monocle.SpriteBank.CreateOn += SpriteBank_CreateOn;
        }

        internal static void Unload() {
            Everest.Events.Level.OnExit -= Level_OnExit;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Monocle.SpriteBank.Create -= SpriteBank_Create;
            On.Monocle.SpriteBank.CreateOn -= SpriteBank_CreateOn;
        }

        private static void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            spriteSwaps.Clear();
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            spriteSwaps.Clear();
            foreach (EntityData data in self.Session.LevelData.Triggers.FindAll(e => e.Name == "SJ2021/SpriteSwapTrigger")) {
                string[] fromIds = data.Attr("fromIds", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(str => str.Trim())
                    .ToArray();

                string[] toIds = data.Attr("toIds", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(str => str.Trim())
                    .ToArray();

                if (fromIds.Length == toIds.Length) {
                    for (int i = 0; i < fromIds.Length; i++) {
                        if (!GFX.SpriteBank.Has(fromIds[i]) || !GFX.SpriteBank.Has(toIds[i])) {
                            Logger.Log(LogLevel.Warn, "SJ2021/SpriteSwapTrigger", $"Couldn't find ID in swap {fromIds[i]} -> {toIds[i]}");
                            continue;
                        }
                        spriteSwaps[fromIds[i]] = toIds[i];
                    }
                } else {
                    Logger.Log(LogLevel.Warn, "SJ2021/SpriteSwapTrigger", "ID lists are not the same length, cancelling swap");
                }
            }

            orig(self, playerIntro, isFromLoader);
        }

        private static Sprite SpriteBank_Create(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id) {
            if (self == GFX.SpriteBank && spriteSwaps.TryGetValue(id, out string newId)) {
                return orig(self, newId);
            }

            return orig(self, id);
        }

        private static Sprite SpriteBank_CreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            if (self == GFX.SpriteBank && spriteSwaps.TryGetValue(id, out string newId)) {
                return orig(self, sprite, newId);
            }

            return orig(self, sprite, id);
        }
    }
}
