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
        // ReSharper disable once InconsistentNaming
        public readonly int BPM;

        public readonly int Bars;

        // The top number in the time signature
        public readonly int BarLength;

        // The bottom number in the time signature
        public readonly int BeatLength;

        public readonly int[] OnAtBeats;

        public readonly string Param;

        public WonkyCassetteBlock(Vector2 position, EntityID id, float width, float height, int index, int bpm, int bars, string timeSignature, string moveSpec, string param)
            : base(position, id, width, height, index, 1.0f) {
            BPM = bpm;
            Bars = bars;
            Param = param;
            var timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;

            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            BarLength = int.Parse(timeSignatureParsed[1].Value);
            BeatLength = int.Parse(timeSignatureParsed[2].Value);

            OnAtBeats = Regex.Split(moveSpec, @",\s*").Select(int.Parse).Select(i => i - 1).ToArray();
        }

        public WonkyCassetteBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Int("index"), data.Int("bpm"), data.Int("bars"),
                data.Attr("timeSignature"), data.Attr("onAtBeats"), data.Attr("sixteenthNoteParam", "sixteenth_note")) {
        }

        // We need to reimplement some of our parent's methods because they refer directly to CassetteBlock when fetching entities

        private static FieldInfo _groupField = typeof(CassetteBlock).GetField("group", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void NewFindInGroup(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock self, CassetteBlock block) {
            if (self is not WonkyCassetteBlock) {
                orig(self, block);

                return;
            }

            var group = (List<CassetteBlock>) _groupField.GetValue(self);

            foreach (var entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>()) {
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
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            On.Celeste.CassetteBlock.FindInGroup += NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame += NewCheckForSame;
        }

        public static void Unload() {
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            On.Celeste.CassetteBlock.FindInGroup -= NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame -= NewCheckForSame;
        }

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            if (entityData.Name != "SJ2021/WonkyCassetteBlock") {
                return false;
            }
            
            // Much of this is from Level.LoadLevel, in the part where it loads CassetteBlocks
            var wonkyBlock = new WonkyCassetteBlock(entityData, offset, new EntityID(levelData.Name, entityData.ID));
            level.Add(wonkyBlock);

            //level.Entities.UpdateLists();
            if (level.Tracker.GetEntities<WonkyCassetteBlockManager>().Count == 0)
                level.Add(new WonkyCassetteBlockManager());

            return true;
        }
    }
}
