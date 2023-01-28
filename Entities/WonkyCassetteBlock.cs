using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WonkyCassetteBlock")]
    [Tracked]
    public class WonkyCassetteBlock : CassetteBlock {
        private static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        public readonly int[] OnAtBeats;
        public readonly int ControllerIndex;

        private readonly int OverrideBoostFrames;
        public int boostFrames = 0;
        public bool boostActive = false;

        private string textureDir;

        private List<Image> _pressed, _solid; // we'll use these instead of pressed and solid, to make `UpdateVisualState` not enumerate through them for no reason.

        public WonkyCassetteBlock(Vector2 position, EntityID id, float width, float height, int index, string moveSpec, Color color, string textureDir, int overrideBoostFrames, int controllerIndex)
            : base(position, id, width, height, index, 1.0f) {
            Tag = Tags.FrozenUpdate | Tags.TransitionUpdate;

            OnAtBeats = OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).ToArray();

            base.color = color;

            this.textureDir = textureDir;

            OverrideBoostFrames = overrideBoostFrames;

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;

            _pressed = new();
            _solid = new();
        }

        public WonkyCassetteBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Int("index"), data.Attr("onAtBeats"), data.HexColor("color"), data.Attr("textureDirectory", "objects/cassetteblock").TrimEnd('/'), data.Int("boostFrames", -1), data.Int("controllerIndex", 0)) { }

        // We need to reimplement some of our parent's methods because they refer directly to CassetteBlock when fetching entities

        private static void NewFindInGroup(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock self, CassetteBlock block) {
            if (self is not WonkyCassetteBlock) {
                orig(self, block);

                return;
            }

            WonkyCassetteBlock selfCast = (WonkyCassetteBlock) self;

            foreach (WonkyCassetteBlock entity in self.Scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>()) {
                if (entity != self && entity != block && entity.Index == self.Index &&
                    entity.ControllerIndex == selfCast.ControllerIndex &&
                    (entity.CollideRect(new Rectangle((int) block.X - 1, (int) block.Y, (int) block.Width + 2, (int) block.Height))
                        || entity.CollideRect(new Rectangle((int) block.X, (int) block.Y - 1, (int) block.Width, (int) block.Height + 2))) &&
                    !self.group.Contains(entity) && entity.OnAtBeats.SequenceEqual(selfCast.OnAtBeats)) {
                    self.group.Add(entity);
                    NewFindInGroup(orig, self, entity);
                }
            }
        }

        public override void Update() {
            bool activating = groupLeader && Activated && !Collidable;

            base.Update();

            if (Activated && Collidable) {
                if (activating) {
                    // Block has activated, Cassette boost is possible this frame
                    if (OverrideBoostFrames > 0) {
                        boostFrames = OverrideBoostFrames - 1;
                        boostActive = true;
                    } else if (OverrideBoostFrames < 0) {
                        WonkyCassetteBlockController controller = this.Scene.Tracker.GetEntity<WonkyCassetteBlockController>();
                        if (controller != null) {
                            boostFrames = controller.ExtraBoostFrames;
                            boostActive = true;
                        }
                    }

                    foreach (CassetteBlock cassetteBlock in group) {
                        WonkyCassetteBlock wonkyBlock = (WonkyCassetteBlock) cassetteBlock;
                        wonkyBlock.boostFrames = boostFrames;
                        wonkyBlock.boostActive = boostActive;
                    }
                }

                if (boostActive) {
                    // Vanilla lift boost is active this frame, do nothing
                    boostActive = false;
                } else if (boostFrames > 0) {
                    // Provide an extra boost for the duration of the extra boost frames
                    this.LiftSpeed.Y = -1 / Engine.DeltaTime;

                    // Update lift of riders
                    MoveVExact(0);

                    boostFrames -= 1;
                }
            }
        }

        public override void Render() {
            if (Utilities.IsRectangleVisible(Position.X, Position.Y, Width, Height)) {
                List<Image> images = Collidable ? _solid : _pressed;

                foreach (Image item in images) {
                    item.Texture.Draw(item.Position + Position, item.Origin, item.Color, item.Scale, item.Rotation, item.Effects);
                }
            }
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
                Image img = block.CreateImage(x, y, tx, ty, GFX.Game[block.textureDir + "/pressed"]);
                // we don't want to have the image in the component list, because then the entity.Get<> function becomes much more expensive,
                // and some modded hooks call it each frame, for each entity...
                // this makes a huge difference in GMHS flag 1.
                img.RemoveSelf();
                block._pressed.Add(img);
                GFX.Game.PopFallback();

                GFX.Game.PushFallback(GFX.Game["objects/cassetteblock/solid"]);
                img = block.CreateImage(x, y, tx, ty, GFX.Game[block.textureDir + "/solid"]);
                img.RemoveSelf();
                block._solid.Add(img);
                GFX.Game.PopFallback();
            } else
                orig(self, x, y, tx, ty);
        }

        private static void CassetteBlock_Awake(ILContext il) {
            ILCursor cursor = new(il);

            // Don't add the BoxSide, as it breaks rendering due to transparency
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<Scene>("Add"))) {
                ILLabel afterAdd = cursor.DefineLabel();

                // skip the Add call if this is a wonky cassette
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<Scene, object, CassetteBlock, bool>>(IsWonky);
                cursor.Emit(OpCodes.Brtrue, afterAdd);

                // restore the args for the Add call
                cursor.Emit(OpCodes.Ldarg_1); // Scene
                cursor.Emit(OpCodes.Ldloc_2); // side
                // Scene.Add will be called here

                cursor.Index++;
                cursor.MarkLabel(afterAdd);
            }
        }

        private static void CassetteBlock_ShiftSize(ILContext il) {
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt<Platform>("MoveV"))) {
                ILLabel beforeMoveV = cursor.DefineLabel();
                ILLabel afterMoveV = cursor.DefineLabel();

                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<CassetteBlock, bool>>(IsWonkyWithoutBoost);
                cursor.Emit(OpCodes.Brfalse, beforeMoveV); // Only run if boostless

                cursor.EmitDelegate<Action<CassetteBlock, float>>(MoveVWithoutBoost);
                cursor.Emit(OpCodes.Br, afterMoveV);

                cursor.MarkLabel(beforeMoveV);
                cursor.GotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<Platform>("MoveV"));
                cursor.MarkLabel(afterMoveV);
            }
        }

        private static bool IsWonky(Scene scene, object side, CassetteBlock self) => self is WonkyCassetteBlock;
        private static bool IsWonkyWithoutBoost(CassetteBlock self) => self is WonkyCassetteBlock block && block.OverrideBoostFrames == 0;

        private static void MoveVWithoutBoost(CassetteBlock self, float amount) => self.MoveV(amount, 0);

        public static void Load() {
            On.Celeste.CassetteBlock.FindInGroup += NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame += NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage += CassetteBlock_SetImage;
            IL.Celeste.CassetteBlock.Awake += CassetteBlock_Awake;
            IL.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
        }

        public static void Unload() {
            On.Celeste.CassetteBlock.FindInGroup -= NewFindInGroup;
            On.Celeste.CassetteBlock.CheckForSame -= NewCheckForSame;
            On.Celeste.CassetteBlock.SetImage -= CassetteBlock_SetImage;
            IL.Celeste.CassetteBlock.Awake -= CassetteBlock_Awake;
            IL.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
        }
    }
}
