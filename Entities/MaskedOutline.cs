using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/MaskedOutline")]
    public class MaskedOutline : Entity {
        private enum OutlineType {
            Booster,
            Refill,
            DoubleRefill,
        }

        private static Vector2[] BoosterDots, RefillDots, DoubleRefillDots;
        private static MTexture dot_texture;
        private static Color SingleColor, DoubleColor;

        private bool Controller;
        private EntityData data;
        private MaskedOutline[] children;
        private Entity parent;
        private SineWave RefillSine;
        private OutlineType type;
        private List<Vector2> dot_positions;
        private Tween tween;
        private Color color;

        public static void Load() {
            // I know this looks like black magic, thats because it is
            BoosterDots = DotsFromOffsets(new[] { new[] { 9, 8, 7, 5, 3, 1 }, new[] { 2, 4, 6, 8, 9, 9 } });
            RefillDots = DotsFromOffsets(new[] { new[] { 5, 3, 1 }, new[] { 1, 3, 5 } });
            DoubleRefillDots = DotsFromOffsets(new[] { new[] { 3, 4, 4, 2, 1 }, new[] { 1, 2, 3, 5, 6 } });
            SingleColor = Calc.HexToColor("2ad257");
            DoubleColor = Calc.HexToColor("d47df9");
        }

        public static void LoadTexture() {
            dot_texture = GFX.Game["util/pixel"].GetSubtexture(1, 1, 1, 1);
        }

        private static Vector2[] DotsFromOffsets(int[][] offsets) {
            Vector2[] result = new Vector2[offsets[0].Length * 4];
            for (int i = 0; i < offsets[0].Length; i++) {
                int index = i * 4;
                result[index] = new Vector2(offsets[0][i], offsets[1][i]);
                result[index + 1] = new Vector2(1 - offsets[0][i], offsets[1][i]);
                result[index + 2] = new Vector2(1 - offsets[0][i], 1 - offsets[1][i]);
                result[index + 3] = new Vector2(offsets[0][i], 1 - offsets[1][i]);
            }
            return reorderArray(result);
        }

        private static Vector2[] reorderArray(Vector2[] arr) {
            int groups = arr.Length / 4;
            int offset = 0;
            Vector2[] result = new Vector2[arr.Length];
            int index = 0;
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < groups; j++) {
                    result[index++] = arr[j * 4 + offset];
                }
                offset++;
                for (int j = groups - 1; j >= 0; j--) {
                    result[index++] = arr[j * 4 + offset];
                }
                offset++;
            }
            return result;
        }

        public MaskedOutline(EntityData data, Vector2 Position, Entity entity) : base(Position) {
            Depth = Depths.Solids - 1;
            Visible = true;
            dot_positions = new();
            parent = entity;
            Add(tween = Tween.Create(Tween.TweenMode.Looping, null, 3, true));
        }

        public MaskedOutline(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Controller = true;
            this.data = data;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (Controller) {
                List<Entity> candidates = new();
                candidates.AddRange(scene.Entities.FindAll<Booster>());
                candidates.AddRange(scene.Entities.FindAll<Refill>());
                children = new MaskedOutline[candidates.Count];
                for (int i = 0; i < candidates.Count; i++) {
                    Scene.Add(children[i] = new MaskedOutline(data, Position, candidates[i]));
                }
                return;
            }
            if (parent is Booster) {
                type = OutlineType.Booster;
            } else if (parent is Refill r){
                type = r.Get<Image>().Texture.AtlasPath.Contains("Two") ? OutlineType.DoubleRefill : OutlineType.Refill; // yeah I know but this is faster
            }
            Setup();
        }

        private void Setup() {
            Position = parent.Position - Vector2.One;
            switch (type) {
                case OutlineType.Booster:
                    dot_positions.AddRange(BoosterDots);
                    color = parent.Get<Sprite>().Texture.AtlasPath.Contains("Red") ? Color.Red : Color.White;
                    break;
                case OutlineType.Refill:
                    dot_positions.AddRange(RefillDots);
                    RefillSine = parent.Get<SineWave>();
                    color = SingleColor;
                    break;
                case OutlineType.DoubleRefill:
                    dot_positions.AddRange(DoubleRefillDots);
                    RefillSine = parent.Get<SineWave>();
                    color = DoubleColor;
                    break;
            }
        }

        private bool IsInBounds(Entity solid, Vector2 dot) {
            Vector2 coord = dot + Position;
            return solid.Bottom >= coord.Y && solid.Top <= coord.Y &&
                        solid.Left <= coord.X && solid.Right >= coord.X;
        }

        private float IndexToScale(int index) {
            float target = tween.Percent * dot_positions.Count;
            float result = Math.Min(Math.Abs(index - target), Math.Abs(target + dot_positions.Count - index));
            return Math.Max(0, 1.5f - result) + 1;
        }

        public override void Render() {
            if (Controller || parent.Scene == null) {
                RemoveSelf();
                return;
            }
            base.Render();
            List<Entity> solids = parent.CollideAll<Solid>();
            for (int j = 0; j < solids.Count; j++) {
                for (int i = 0; i < dot_positions.Count; i++) {
                    if (IsInBounds(solids[j], dot_positions[i])) {
                        dot_texture.Draw(dot_positions[i] + Position + ((type == OutlineType.Booster) ? Vector2.Zero : GetRefillOffset()), Vector2.Zero, color, IndexToScale(i));
                    }
                }
            }
        }

        private Vector2 GetRefillOffset() {
            return parent.Collidable? RefillSine.Value * 2 * Vector2.UnitY : Vector2.Zero;
        }
    }
}
