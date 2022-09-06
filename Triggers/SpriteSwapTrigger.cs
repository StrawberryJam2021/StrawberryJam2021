using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/SpriteSwapTrigger")]
    public class SpriteSwapTrigger : Trigger {
        private static readonly char[] separators = { ',' };
        private readonly string[] fromIds;
        private readonly string[] toIds;
        private readonly Dictionary<MTexture, string> texToSwapId = new();

        public SpriteSwapTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            fromIds = data.Attr("fromIds", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();

            toIds = data.Attr("toIds", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();

            if (fromIds.Length == toIds.Length) {
                for (int i = 0; i < fromIds.Length; i++) {
                    if (!GFX.SpriteBank.Has(fromIds[i]) || !GFX.SpriteBank.Has(toIds[i])) {
                        Logger.Log(LogLevel.Warn, "SJ2021/SpriteSwapTrigger", $"Couldn't find ID in swap {fromIds[i]} -> {toIds[i]}");
                        continue;
                    }
                    MTexture fromTex = GFX.SpriteBank.SpriteData[fromIds[i]].Sprite.Texture;
                    texToSwapId[fromTex] = toIds[i];
                }
            } else {
                Logger.Log(LogLevel.Warn, "SJ2021/SpriteSwapTrigger", "ID lists are not the same length, cancelling swap");
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (texToSwapId.Count > 0) {
                Sprite[] swappableSprites = scene.Entities
                    .Where(e => e is not Decal)
                    .SelectMany(e => e.Components.OfType<Sprite>())
                    .ToArray();

                foreach (Sprite sprite in swappableSprites) {
                    if (texToSwapId.TryGetValue(sprite.Texture, out string id)) {
                        GFX.SpriteBank.CreateOn(sprite, id);
                    }
                }
            }
        }
    }
}