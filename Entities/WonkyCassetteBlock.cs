using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
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
        public readonly int ControllerIndex;

        private readonly int OverrideBoostFrames;
        public int boostFrames = 0;

        private DynData<CassetteBlock> cassetteBlockData;

        private string textureDir;

        public WonkyCassetteBlock(Vector2 position, EntityID id, float width, float height, int index, string moveSpec, Color color, string textureDir, int overrideBoostFrames, int controllerIndex)
            : base(position, id, width, height, index, 1.0f) {
            Tag = Tags.FrozenUpdate | Tags.TransitionUpdate;

            OnAtBeats = Regex.Split(moveSpec, @",\s*").Select(int.Parse).Select(i => i - 1).ToArray();

            cassetteBlockData = new DynData<CassetteBlock>(this);
            cassetteBlockData["color"] = color;

            this.textureDir = textureDir;

            if (overrideBoostFrames < 0)
                throw new ArgumentException($"Boost Frames must be 0 or greater, but is set to {overrideBoostFrames}.");

            OverrideBoostFrames = overrideBoostFrames;

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;
        }

        public WonkyCassetteBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Int("index"), data.Attr("onAtBeats"), data.HexColor("color"), data.Attr("textureDirectory", "objects/cassetteblock").TrimEnd('/'), data.Int("boostFrames", 0), data.Int("controllerIndex", 0)) { }

        // We need to reimplement some of our parent's methods because they refer directly to CassetteBlock when fetching entities

        private static readonly MethodInfo m_CassetteBlock_CreateImage = typeof(CassetteBlock).GetMethod("CreateImage", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _groupField = typeof(CassetteBlock).GetField("group", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _sideField = typeof(CassetteBlock).GetField("side", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void NewFindInGroup(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock self, CassetteBlock block) {
            if (self is not WonkyCassetteBlock) {
                orig(self, block);

                return;
            }

            WonkyCassetteBlock selfCast = (WonkyCassetteBlock) self;

            var group = (List<CassetteBlock>) _groupField.GetValue(self);

            foreach (WonkyCassetteBlock entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>()) {
                if (entity != self && entity != block && entity.Index == self.Index &&
                    entity.ControllerIndex == selfCast.ControllerIndex &&
                    (entity.CollideRect(new Rectangle((int) block.X - 1, (int) block.Y, (int) block.Width + 2, (int) block.Height))
                        || entity.CollideRect(new Rectangle((int) block.X, (int) block.Y - 1, (int) block.Width, (int) block.Height + 2))) &&
                    !group.Contains(entity) && entity.OnAtBeats.SequenceEqual(selfCast.OnAtBeats)) {
                    group.Add(entity);
                    NewFindInGroup(orig, self, entity);
                    _groupField.SetValue(entity, group);
                }
            }
        }

        public override void Update() {
            bool activating = Activated && !Collidable;

            base.Update();

            if (Activated && Collidable) {
                if (activating) {
                    // Block has activated, Cassette boost is possible this frame
                    if (OverrideBoostFrames > 0) {
                        boostFrames = OverrideBoostFrames;
                    } else {
                        WonkyCassetteBlockController controller = this.Scene.Tracker.GetEntity<WonkyCassetteBlockController>();
                        if (controller != null) {
                            boostFrames = controller.ExtraBoostFrames;
                        }
                    }

                } else if (boostFrames > 0) {
                    // Provide an extra boost for the duration of the extra boost frames
                    this.LiftSpeed.Y = -1 / Engine.DeltaTime;

                    // Update lift of riders
                    MoveVExact(0);

                    boostFrames -= 1;
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            // Remove the box side, as transparency makes it look very weird
            // This needs to be delayed to the end of the frame, otherwise RemoveSelf does nothing here
            scene.OnEndOfFrame += () => (_sideField.GetValue(this) as Entity).RemoveSelf();
        }

        private static bool NewCheckForSame(On.Celeste.CassetteBlock.orig_CheckForSame origCheckForSame, CassetteBlock self, float x, float y) {
            if (!(self is WonkyCassetteBlock))
                return origCheckForSame(self, x, y);

            WonkyCassetteBlock selfCast = (WonkyCassetteBlock) self;

            return self.Scene.Tracker.GetEntities<WonkyCassetteBlock>()
                .Cast<WonkyCassetteBlock>()
                .Any(entity => entity.Index == self.Index
                               && entity.ControllerIndex == selfCast.ControllerIndex
                               && entity.Collider.Collide(new Rectangle((int) x, (int) y, 8, 8))
                               && entity.OnAtBeats.SequenceEqual(selfCast.OnAtBeats));
        }

        private static void CassetteBlock_SetImage(On.Celeste.CassetteBlock.orig_SetImage orig, CassetteBlock self, float x, float y, int tx, int ty) {
            if (self is WonkyCassetteBlock block) {
                GFX.Game.PushFallback(GFX.Game["objects/cassetteblock/pressed00"]);
                block.cassetteBlockData.Get<List<Image>>("pressed").Add((Image) m_CassetteBlock_CreateImage.Invoke(block, new object[] { x, y, tx, ty, GFX.Game[block.textureDir + "/pressed"] }));
                GFX.Game.PopFallback();

                GFX.Game.PushFallback(GFX.Game["objects/cassetteblock/solid"]);
                block.cassetteBlockData.Get<List<Image>>("solid").Add((Image) m_CassetteBlock_CreateImage.Invoke(block, new object[] { x, y, tx, ty, GFX.Game[block.textureDir + "/solid"] }));
                GFX.Game.PopFallback();
            } else
                orig(self, x, y, tx, ty);
        }

        public static void Load() {
            On.Celeste.CassetteBlock.FindInGroup += NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame += NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage += CassetteBlock_SetImage;
        }

        public static void Unload() {
            On.Celeste.CassetteBlock.FindInGroup -= NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame -= NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage -= CassetteBlock_SetImage;
        }
    }
}
