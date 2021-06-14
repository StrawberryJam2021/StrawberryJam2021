using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WonkyCassetteBlock")]
    [Tracked]
    public class WonkyCassetteBlock : CassetteBlock {

        public readonly int[] OnAtBeats;

        public WonkyCassetteBlock(Vector2 position, EntityID id, float width, float height, int index, string moveSpec)
            : base(position, id, width, height, index, 1.0f) {
            OnAtBeats = Regex.Split(moveSpec, @",\s*").Select(int.Parse).Select(i => i - 1).ToArray();
        }

        public WonkyCassetteBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Int("index"), data.Attr("onAtBeats")) { }

        // We need to reimplement some of our parent's methods because they refer directly to CassetteBlock when fetching entities

        private static readonly FieldInfo _groupField = typeof(CassetteBlock).GetField("group", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void NewFindInGroup(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock self, CassetteBlock block) {
            if (self is not WonkyCassetteBlock) {
                orig(self, block);

                return;
            }

            var group = (List<CassetteBlock>) _groupField.GetValue(self);

            foreach (WonkyCassetteBlock entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>()) {
                if (entity != self && entity != block && entity.Index == self.Index &&
                    (entity.CollideRect(new Rectangle((int) block.X - 1, (int) block.Y, (int) block.Width + 2, (int) block.Height))
                        ? 1
                        : entity.CollideRect(new Rectangle((int) block.X, (int) block.Y - 1, (int) block.Width, (int) block.Height + 2))
                            ? 1
                            : 0) != 0 &&
                    !group.Contains(entity)) {
                    group.Add(entity);
                    NewFindInGroup(orig, self, entity);
                    _groupField.SetValue(entity, group);
                }
            }
        }

        private static bool NewCheckForSame(On.Celeste.CassetteBlock.orig_CheckForSame origCheckForSame, CassetteBlock self, float x, float y) {
            if (!(self is WonkyCassetteBlock))
                return origCheckForSame(self, x, y);

            return self.Scene.Tracker.GetEntities<WonkyCassetteBlock>()
                .Cast<WonkyCassetteBlock>()
                .Any(entity => entity.Index == self.Index
                               && entity.Collider.Collide(new Rectangle((int) x, (int) y, 8, 8)));
        }

        public static void Load() {
            On.Celeste.CassetteBlock.FindInGroup += NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame += NewCheckForSame;
        }

        public static void Unload() {
            On.Celeste.CassetteBlock.FindInGroup -= NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame -= NewCheckForSame;
        }
    }
}
