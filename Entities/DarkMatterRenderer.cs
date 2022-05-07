using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [Tracked(false)]
    public class DarkMatterRenderer : Entity {

        public static VirtualRenderTarget DarkMatterLightning;
        public static VirtualRenderTarget BlurTempBuffer;
        
        private class Bolt {

            private DarkMatterRenderer Parent;

            private List<Vector2> nodes = new List<Vector2>();

            private Coroutine routine;

            private bool visible;

            private float size;

            private float gap;

            private float alpha;

            private uint seed;

            private float flash;

            private readonly int colIndex;

            private readonly float scale;

            private readonly int width;

            private readonly int height;

            public Bolt(DarkMatterRenderer parent, int index, float scale, int width, int height) {
                Parent = parent;
                colIndex = index;
                this.width = width;
                this.height = height;
                this.scale = scale;
                routine = new Coroutine(Run());
            }

            public void Update(Scene scene) {
                routine.Update();
                flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 2f);
            }

            private IEnumerator Run() {
                yield return Calc.Random.Range(0f, 4f);
                while (true) {
                    List<Vector2> list = new List<Vector2>();
                    for (int k = 0; k < 3; k++) {
                        Vector2 item = Calc.Random.Choose(new Vector2(0f, Calc.Random.Range(8, height - 16)), new Vector2(Calc.Random.Range(8, width - 16), 0f), new Vector2(width, Calc.Random.Range(8, height - 16)), new Vector2(Calc.Random.Range(8, width - 16), height));
                        Vector2 item2 = ((item.X <= 0f || item.X >= (float) width) ? new Vector2((float) width - item.X, item.Y) : new Vector2(item.X, (float) height - item.Y));
                        list.Add(item);
                        list.Add(item2);
                    }
                    List<Vector2> list2 = new List<Vector2>();
                    for (int l = 0; l < 3; l++) {
                        list2.Add(new Vector2(Calc.Random.Range(0.25f, 0.75f) * (float) width, Calc.Random.Range(0.25f, 0.75f) * (float) height));
                    }
                    nodes.Clear();
                    foreach (Vector2 item4 in list) {
                        nodes.Add(item4);
                        nodes.Add(list2.ClosestTo(item4));
                    }
                    Vector2 item3 = list2[list2.Count - 1];
                    foreach (Vector2 item5 in list2) {
                        nodes.Add(item3);
                        nodes.Add(item5);
                        item3 = item5;
                    }
                    flash = 1f;
                    visible = true;
                    size = 5f;
                    gap = 0f;
                    alpha = 1f;
                    for (int j = 0; j < 4; j++) {
                        seed = (uint) Calc.Random.Next();
                        yield return 0.1f;
                    }
                    for (int j = 0; j < 5; j++) {
                        if (!Settings.Instance.DisableFlashes) {
                            visible = false;
                        }
                        yield return 0.05f + (float) j * 0.02f;
                        float num = (float) j / 5f;
                        visible = true;
                        size = (1f - num) * 5f;
                        gap = num;
                        alpha = 1f - num;
                        visible = true;
                        seed = (uint) Calc.Random.Next();
                        yield return 0.025f;
                    }
                    visible = false;
                    yield return Calc.Random.Range(4f, 8f);
                }
            }

            public void Render() {
                if (flash > 0f && !Settings.Instance.DisableFlashes) {
                    Draw.Rect(0f, 0f, width, height, Color.Black * flash * 0.65f * scale);
                }
                if (visible) {
                    for (int i = 0; i < nodes.Count; i += 2) {
                        DrawFatStrawberryJamDarkMatter(seed, nodes[i], nodes[i + 1], size * scale, gap, colorSets[Parent.mode][colIndex] * alpha);
                    }
                }
            }
        }

        private class Edge {
            public DarkMatter Parent;

            public bool Visible;

            public Vector2 A;

            public Vector2 B;

            public Vector2 Min;

            public Vector2 Max;

            public Edge(DarkMatter parent, Vector2 a, Vector2 b) {
                Parent = parent;
                Visible = true;
                A = a;
                B = b;
                Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            }

            public bool InView(ref Rectangle view) {
                if ((float) view.Left < Parent.X + Max.X && (float) view.Right > Parent.X + Min.X && (float) view.Top < Parent.Y + Max.Y) {
                    return (float) view.Bottom > Parent.Y + Min.Y;
                }
                return false;
            }
        }

        public enum Mode {
            Kill = 0,
            Zoomies = 1
        }

        private List<DarkMatter> list = new List<DarkMatter>();

        private List<Edge> edges = new List<Edge>();

        private List<Bolt> bolts = new List<Bolt>();

        private VertexPositionColor[] edgeVerts;

        private VirtualMap<bool> tiles;

        private Rectangle levelTileBounds;

        private uint edgeSeed;

        private uint leapSeed;

        private bool dirty;

        public Mode mode;

        private static Dictionary<Mode, Color[]> colorSets = new Dictionary<Mode, Color[]> {
            { Mode.Kill, new Color[2] { Calc.HexToColor("7800b5"), Calc.HexToColor("663fA0") } },
            { Mode.Zoomies, new Color[2] { Calc.HexToColor("3b0c5c"), Calc.HexToColor("5e0864") } },
        };


        private Color[] colorsLerped;

        public float Fade;

        public bool UpdateSeeds = true;

        public const int BoltBufferSize = 160;

        public bool DrawEdges = true;

        public SoundSource AmbientSfx;

        public DarkMatterRenderer() {
            base.Tag = (int) Tags.Global | (int) Tags.TransitionUpdate;
            base.Depth = -1000100;
            colorsLerped = new Color[2];
            Add(new BeforeRenderHook(OnBeforeRender));
            Add(AmbientSfx = new SoundSource());
            AmbientSfx.DisposeOnTransition = false;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            for (int i = 0; i < 4; i++) {
                bolts.Add(new Bolt(this, 0, 1f, 160, 160));
                bolts.Add(new Bolt(this, 1, 0.35f, 160, 160));
            }
            UpdateMode(scene as Level);
        }
        public void StartAmbience() {
            if (!AmbientSfx.Playing) {
                AmbientSfx.Play("event:/strawberry_jam_2021/env/darkMatter");
            }
        }

        public void StopAmbience() {
            AmbientSfx.Stop();
        }

        public void Reset() {
            UpdateSeeds = true;
            Fade = 0f;
        }

        public void Track(DarkMatter block) {
            list.Add(block);
            if (tiles == null) {
                levelTileBounds = (base.Scene as Level).TileBounds;
                tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: false);
            }
            for (int i = (int) block.X / 8; i < ((int) block.X + block.VisualWidth) / 8; i++) {
                for (int j = (int) block.Y / 8; j < ((int) block.Y + block.VisualHeight) / 8; j++) {
                    tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = true;
                }
            }
            dirty = true;
            block.renderer = this;
        }

        public void Untrack(DarkMatter block) {
            list.Remove(block);
            if (list.Count <= 0) {
                tiles = null;
            } else {
                for (int i = (int) block.X / 8; (float) i < block.Right / 8f; i++) {
                    for (int j = (int) block.Y / 8; (float) j < block.Bottom / 8f; j++) {
                        tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = false;
                    }
                }
            }
            dirty = true;
            block.renderer = null;
        }

        public override void Update() {
            UpdateMode();
            if (dirty) {
                RebuildEdges();
            }
            ToggleEdges();
            if (list.Count <= 0) {
                return;
            }

            foreach (Bolt bolt in bolts) {
                bolt.Update(base.Scene);
            }
            if (UpdateSeeds) {
                if (base.Scene.OnInterval(0.1f)) {
                    edgeSeed = (uint) Calc.Random.Next();
                }
                if (base.Scene.OnInterval(0.7f)) {
                    leapSeed = (uint) Calc.Random.Next();
                }
            }
        }

        public void UpdateMode(Level level = null) {
            if(level == null && Scene is Level) {
                level = Scene as Level;
            }
            mode = level.Session.GetFlag("SJ2021/DarkMatterRenderer") ? Mode.Zoomies : Mode.Kill;
        }

        public void ToggleEdges(bool immediate = false) {
            Camera camera = (base.Scene as Level).Camera;
            Rectangle view = new Rectangle((int) camera.Left - 4, (int) camera.Top - 4, (int) (camera.Right - camera.Left) + 8, (int) (camera.Bottom - camera.Top) + 8);
            for (int i = 0; i < edges.Count; i++) {
                if (immediate) {
                    edges[i].Visible = edges[i].InView(ref view);
                } else if (!edges[i].Visible && base.Scene.OnInterval(0.05f, (float) i * 0.01f) && edges[i].InView(ref view)) {
                    edges[i].Visible = true;
                } else if (edges[i].Visible && base.Scene.OnInterval(0.25f, (float) i * 0.01f) && !edges[i].InView(ref view)) {
                    edges[i].Visible = false;
                }
            }
        }

        private void RebuildEdges() {
            dirty = false;
            edges.Clear();
            if (list.Count <= 0) {
                return;
            }
            Point[] array = new Point[4]
            {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0)
            };
            foreach (DarkMatter item in list) {
                for (int i = (int) item.X / 8; (float) i < item.Right / 8f; i++) {
                    for (int j = (int) item.Y / 8; (float) j < item.Bottom / 8f; j++) {
                        Point[] array2 = array;
                        for (int k = 0; k < array2.Length; k++) {
                            Point point = array2[k];
                            Point point2 = new Point(-point.Y, point.X);
                            if (Inside(i + point.X, j + point.Y) || (Inside(i - point2.X, j - point2.Y) && !Inside(i + point.X - point2.X, j + point.Y - point2.Y))) {
                                continue;
                            }
                            Point point3 = new Point(i, j);
                            Point point4 = new Point(i + point2.X, j + point2.Y);
                            Vector2 vector = new Vector2(4f) + new Vector2(point.X - point2.X, point.Y - point2.Y) * 4f;
                            int num = 1;
                            while (Inside(point4.X, point4.Y) && !Inside(point4.X + point.X, point4.Y + point.Y)) {
                                point4.X += point2.X;
                                point4.Y += point2.Y;
                                num++;
                                if (num > 8) {
                                    Vector2 a = new Vector2(point3.X, point3.Y) * 8f + vector - item.Position;
                                    Vector2 b = new Vector2(point4.X, point4.Y) * 8f + vector - item.Position;
                                    edges.Add(new Edge(item, a, b));
                                    num = 0;
                                    point3 = point4;
                                }
                            }
                            if (num > 0) {
                                Vector2 a = new Vector2(point3.X, point3.Y) * 8f + vector - item.Position;
                                Vector2 b = new Vector2(point4.X, point4.Y) * 8f + vector - item.Position;
                                edges.Add(new Edge(item, a, b));
                            }
                        }
                    }
                }
            }
            if (edgeVerts == null) {
                edgeVerts = new VertexPositionColor[1024];
            }
        }

        private bool Inside(int tx, int ty) {
            return tiles[tx - levelTileBounds.X, ty - levelTileBounds.Y];
        }

        private void OnBeforeRender() {
            if (list.Count <= 0) {
                return;
            }
            Engine.Graphics.GraphicsDevice.SetRenderTarget(DarkMatterLightning);
            Engine.Graphics.GraphicsDevice.Clear(Color.Lerp(Calc.HexToColor("470076") * 0.125f, Color.Black, Fade));
            Draw.SpriteBatch.Begin();
            foreach (Bolt bolt in bolts) {
                bolt.Render();
            }
            Draw.SpriteBatch.End();
        }

        public override void Render() {
            if (list.Count <= 0) {
                return;
            }
            Camera camera = (base.Scene as Level).Camera;
            new Rectangle((int) camera.Left, (int) camera.Top, (int) (camera.Right - camera.Left), (int) (camera.Bottom - camera.Top));
            //Acts as a "color filter"
            
            foreach (DarkMatter item in list) {
                if (item.Visible) {
                    Draw.SpriteBatch.Draw(DarkMatterLightning, item.Position, new Rectangle((int) item.X, (int) item.Y, item.VisualWidth, item.VisualHeight), Color.White);
                }
            }
            if (edges.Count <= 0 || !DrawEdges) {
                return;
            }
            for (int i = 0; i < colorsLerped.Length; i++) {
                colorsLerped[i] = Color.Lerp(colorSets[mode][i], Color.White, 4 * Fade);
            }
            int index = 0;
            uint seed = leapSeed;
            foreach (Edge edge in edges) {
                if (edge.Visible) {
                    DrawSimpleStrawberryJamDarkMatter(ref index, ref edgeVerts, edgeSeed, edge.Parent.Position, edge.A, edge.B, colorsLerped[0], 1f + Fade * 3f);
                    DrawSimpleStrawberryJamDarkMatter(ref index, ref edgeVerts, edgeSeed + 1, edge.Parent.Position, edge.A, edge.B, colorsLerped[1], 1f + Fade * 3f);
                    if (PseudoRand(ref seed) % 30u == 0) {
                        DrawBezierStrawberryJamDarkMatter(ref index, ref edgeVerts, edgeSeed, edge.Parent.Position, edge.A, edge.B, 24f, 10, colorsLerped[1]);
                    }
                }
            }
            if (index > 0) {
                GameplayRenderer.End();
                GFX.DrawVertices(camera.Matrix, edgeVerts, index);
                GameplayRenderer.Begin();
            }
        }

        private static void DrawSimpleStrawberryJamDarkMatter(ref int index, ref VertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a, Vector2 b, Color color, float thickness = 1f) {
            seed += (uint) (a.GetHashCode() + b.GetHashCode());
            a += pos;
            b += pos;
            float num = (b - a).Length();
            Vector2 vector = (b - a) / num;
            Vector2 vector2 = vector.TurnRight();
            a += vector2;
            b += vector2;
            Vector2 vector3 = a;
            int num2 = ((PseudoRand(ref seed) % 2u != 0) ? 1 : (-1));
            float num3 = PseudoRandRange(ref seed, 0f, (float) Math.PI * 2f);
            float num4 = 0f;
            float num5 = (float) index + ((b - a).Length() / 4f + 1f) * 6f;
            while (num5 >= (float) verts.Length) {
                Array.Resize(ref verts, verts.Length * 2);
            }
            for (int i = index; (float) i < num5; i++) {
                verts[i].Color = color;
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

        private static void DrawBezierStrawberryJamDarkMatter(ref int index, ref VertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a, Vector2 b, float anchor, int steps, Color color) {
            seed += (uint) (a.GetHashCode() + b.GetHashCode());
            a += pos;
            b += pos;
            Vector2 vector = (b - a).SafeNormalize().TurnRight();
            SimpleCurve simpleCurve = new SimpleCurve(a, b, (b + a) / 2f + vector * anchor);
            int num = index + (steps + 2) * 6;
            while (num >= verts.Length) {
                Array.Resize(ref verts, verts.Length * 2);
            }
            Vector2 vector2 = simpleCurve.GetPoint(0f);
            for (int i = 0; i <= steps; i++) {
                Vector2 point = simpleCurve.GetPoint((float) i / (float) steps);
                if (i != steps) {
                    point += new Vector2(PseudoRandRange(ref seed, -2f, 2f), PseudoRandRange(ref seed, -2f, 2f));
                }
                verts[index].Position = new Vector3(vector2 - vector, 0f);
                verts[index++].Color = color;
                verts[index].Position = new Vector3(point - vector, 0f);
                verts[index++].Color = color;
                verts[index].Position = new Vector3(point, 0f);
                verts[index++].Color = color;
                verts[index].Position = new Vector3(vector2 - vector, 0f);
                verts[index++].Color = color;
                verts[index].Position = new Vector3(point, 0f);
                verts[index++].Color = color;
                verts[index].Position = new Vector3(vector2, 0f);
                verts[index++].Color = color;
                vector2 = point;
            }
        }

        private static void DrawFatStrawberryJamDarkMatter(uint seed, Vector2 a, Vector2 b, float size, float gap, Color color) {
            seed += (uint) (a.GetHashCode() + b.GetHashCode());
            float num = (b - a).Length();
            Vector2 vector = (b - a) / num;
            Vector2 vector2 = vector.TurnRight();
            Vector2 vector3 = a;
            int num2 = 1;
            PseudoRandRange(ref seed, 0f, (float) Math.PI * 2f);
            float num3 = 0f;
            do {
                num3 += PseudoRandRange(ref seed, 10f, 14f);
                Vector2 vector4 = a + vector * num3;
                if (num3 < num) {
                    vector4 += num2 * vector2 * PseudoRandRange(ref seed, 0f, 6f);
                } else {
                    vector4 = b;
                }
                Vector2 vector5 = vector4;
                if (gap > 0f) {
                    vector5 = vector3 + (vector4 - vector3) * (1f - gap);
                    Draw.Line(vector3, vector4 + vector, color, size * 0.5f);
                }
                Draw.Line(vector3, vector5 + vector, color, size);
                vector3 = vector4;
                num2 = -num2;
            }
            while (num3 < num);
        }

        private static uint PseudoRand(ref uint seed) {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            return seed;
        }

        public static float PseudoRandRange(ref uint seed, float min, float max) {
            return min + (float) (PseudoRand(ref seed) & 0x3FFu) / 1024f * (max - min);
        }
    }
}
