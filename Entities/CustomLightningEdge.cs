using System;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity(
        "SJ2021/CustomLightningEdge")]
    public class CustomLightningEdge : Entity {
        public enum Directions { Right, Up, Left, Down }

        private Color[] _electricityColors;

        private float Fade;
        private Vector2 _start = Vector2.Zero;
        private Vector2 _end;
        private VertexPositionColor[] _edgeVerts = new VertexPositionColor[1024];
        private uint _edgeSeed;
        private int size;
        private float interval;

        public CustomLightningEdge(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Depth = data.Int("Depth", 10);
            Vector2? node = data.FirstNodeNullable(offset);
            if (node.HasValue) {
                _end = node.Value - Position;
            } else {

                Directions direction = data.Enum<Directions>("direction", Directions.Up);
                Vector2 _offset;
                switch (direction) {
                    default:
                    case Directions.Up:
                    case Directions.Down:
                        size = data.Width;
                        _offset = Vector2.UnitX * size;
                        break;
                    case Directions.Left:
                    case Directions.Right:
                        size = data.Height;
                        _offset = Vector2.UnitY * size;
                        break;
                }
                _end = _offset;
            }
            _electricityColors = new Color[] { Utilities.HexOrNameToColor(data.Attr("color1", "fcf579")), Utilities.HexOrNameToColor(data.Attr("color2", "8cf7e2")) };
            interval = Math.Max(data.Float("interval", 0.05f), 0.016f);
            
        }

        public override void Update() {
            base.Update();
            if (Scene.OnInterval(interval)) {
                _edgeSeed = (uint) Calc.Random.Next();
            }
        }

        public override void Render() {
            base.Render();
            Camera camera = (Scene as Level).Camera;
            if (camera != null) {
                int index = 0;
                DrawSimpleLightning(ref index, ref _edgeVerts, _edgeSeed, Position, _start, _end, _electricityColors[0], 1f + Fade * 1f);
                DrawSimpleLightning(ref index, ref _edgeVerts, _edgeSeed + 1, Position, _start, _end, _electricityColors[1], 1f + Fade * 1f);
                if (index > 0) {
                    GameplayRenderer.End();
                    GFX.DrawVertices(camera.Matrix, _edgeVerts, index);
                    GameplayRenderer.Begin();
                }
            }
        }

        private static void DrawSimpleLightning(ref int index, ref VertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a, Vector2 b, Color color, float thickness = 1f) {
            seed = (uint) ((int) seed + (a.GetHashCode() + b.GetHashCode()));
            a += pos;
            b += pos;
            float num = (b - a).Length();
            Vector2 vector = (b - a) / num;
            Vector2 vector2 = vector.TurnRight();
            a += vector2;
            b += vector2;
            Vector2 vector3 = a;
            int num2 = (PseudoRand(ref seed) % 2u != 0) ? 1 : (-1);
            float num3 = PseudoRandRange(ref seed, 0f, (float) Math.PI * 2f);
            float num4 = 0f;
            float num5 = (float) index + ((b - a).Length() / 4f + 1f) * 6f;
            while (num5 >= (float) verts.Length) {
                Array.Resize(ref verts, verts.Length * 2);
            }
            for (int i = index; (float) i < num5; i++) {
                verts[i].Color = color * 0.75f;
            }
            do {
                float num6 = PseudoRandRange(ref seed, 0f, 4f);
                num3 += 0.1f;
                num4 += 4f + num6;
                Vector2 vector4 = a + vector * num4;
                if (num4 < num) {
                    vector4 += num2 * vector2 * num6 - vector2;
                } else {
                    vector4 = b;
                }
                verts[index++].Position = new Vector3(vector3 - vector2 * thickness, 0f);
                verts[index++].Position = new Vector3(vector4 - vector2 * thickness, 0f);
                verts[index++].Position = new Vector3(vector4 + vector2 * thickness, 0f);
                verts[index++].Position = new Vector3(vector3 - vector2 * thickness, 0f);
                verts[index++].Position = new Vector3(vector4 + vector2 * thickness, 0f);
                verts[index++].Position = new Vector3(vector3, 0f);
                vector3 = vector4;
                num2 = -num2;
            }
            while (num4 < num);
        }

        private static uint PseudoRand(ref uint seed) {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            return seed;
        }

        public static float PseudoRandRange(ref uint seed, float min, float max) {
            return min + (float) (double) (PseudoRand(ref seed) & 0x3FF) / 1024f * (max - min);
        }
    }
}
