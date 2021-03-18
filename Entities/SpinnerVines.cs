using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class SpinnerVinesModule : EverestModule
{

    // Only one alive module instance can exist at any given time.
    public static SpinnerVinesModule Instance;

    public SpinnerVinesModule()
    {
        Instance = this;
    }

    public override void Load()
    {
        Console.WriteLine("loadieload");
    }

    public override void Unload()
    {
    }
}
namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SpinnerVines")]
    [Tracked(false)]
    public class SpinnerVines : Entity {
        private struct Tentacle {
            public Vector2 Position;

            public float Width;

            public float Length;

            public float Approach;

            public float WaveOffset;

            public int TexIndex;

            public int FillerTexIndex;

            public Vector2 LerpPositionFrom;

            public float LerpPercent;

            public float LerpDuration;
            public float LerpDuratiodn;
        }

        public int Index;

        public List<Vector2> Nodes = new List<Vector2>();

        private Vector2 outwards;

        private Vector2 lastOutwards;

        private float ease;

        private Vector2 p;

        private Player player;

        private float fearDistance;

        private float offset;

        private bool createdFromLevel;

        private int slideUntilIndex;

        private int layer;

        private const int NodesPerTentacle = 10;

        private Tentacle[] tentacles;

        private int tentacleCount;

        private VertexPositionColorTexture[] vertices;

        private int vertexCount;

        private Color color = Calc.HexToColor("3f7a3b");

        private float soundDelay = 0.25f;

        private List<MTexture[]> arms = new List<MTexture[]>();

        private List<MTexture> fillers;

        private Vector2[] SpinnerGrowthPos = new Vector2[2];

        Vector2[] SpinnerPositionsSmooth;

        int CurrentDirNum = 2;

        CrystalStaticSpinner CurrentSpinner;

        Image currenspinnerimg;

        float SpinnerWaitTime = 1f;

        Vector2[] SpinnerPositions;

        int CurrentSpinnerNum = 1;

        int CurrentGrowthCycle = 0;

        bool HasStartedGrowing = false;

        Vector2 GrowDir;

        int t = 0;

        bool DisableHitBox = false;

        float AverageVineLength = 0f;

        float MaxThin = 2f;

        float TentacleWidth = 5f;

        Color SpinnerColor;

        Vector2[] WidthVecarr;

        public SpinnerVines() {
        }

        public SpinnerVines(EntityData data, Vector2 offset) : base(data.Position + offset) {
            float SpinnerGrowthTime = (float) SpinnerWaitTime;
            Add(new Coroutine(SpawnGrowingSpinner(SpinnerGrowthTime / 7f)));
            SpinnerWaitTime = data.Int("NewSpinnerWaitTime");
            TentacleWidth = data.Float("TentacleWidth");
            MaxThin = data.Float("MaxThicknessDecrease");
            color = Calc.HexToColor(data.Attr("TentacleColor", "3f7a3b"));
            SpinnerColor = Calc.HexToColor(data.Attr("SpinnerColor", "ff003c"));
            SpinnerPositions = data.NodesOffset(Position);

            Vector2[] vecarr = new Vector2[SpinnerPositions.Length + 1];
            vecarr[0] = Position;
            for (int i = 1; i < vecarr.Length; i++) {
                vecarr[i] = SpinnerPositions[i - 1];
            }
            SpinnerPositions = vecarr;

            SpinnerPositionsSmooth = new Vector2[SpinnerPositions.Length * 2 - 1];

            for (int i = 0; i < SpinnerPositions.Length; i++) {
                Console.WriteLine(SpinnerPositions[i]);
            }

            int r = 0;
            for (int i = 0; i < SpinnerPositionsSmooth.Length; i += 2, r++) {
                SpinnerPositionsSmooth[i] = SpinnerPositions[r];
            }

            float CurveOffset;

            for (int i = 1; i <= SpinnerPositionsSmooth.Length - 2; i += 2) {
                if (Math.Abs(Vector2.Distance(SpinnerPositionsSmooth[i - 1], SpinnerPositionsSmooth[i + 1])) > 1.5) {
                    if (i <= 2) {
                        CurveOffset = (SpinnerPositionsSmooth[i + 1].X - SpinnerPositionsSmooth[i - 1].X) / 30;
                    } else {
                        CurveOffset = (SpinnerPositionsSmooth[i + -1].X - SpinnerPositionsSmooth[i - 3].X) / 30;
                    }
                    SpinnerPositionsSmooth[i] = new Vector2(((SpinnerPositionsSmooth[i - 1].X + SpinnerPositionsSmooth[i + 1].X) / 2) + (8 * CurveOffset), (SpinnerPositionsSmooth[i - 1].Y + SpinnerPositionsSmooth[i + 1].Y) / 2);
                } else {
                    SpinnerPositionsSmooth[i] = new Vector2(((SpinnerPositionsSmooth[i - 1].X + SpinnerPositionsSmooth[i + 1].X) / 2), (SpinnerPositionsSmooth[i - 1].Y + SpinnerPositionsSmooth[i + 1].Y) / 2);
                }
            }

            SpinnerGrowthPos[0] = SpinnerPositionsSmooth[0];
            SpinnerGrowthPos[1] = SpinnerPositionsSmooth[1];



            for (int i = 0; i < SpinnerPositionsSmooth.Length - 1; i++) {
                AverageVineLength += Vector2.Distance(SpinnerPositionsSmooth[i], SpinnerPositionsSmooth[i + 1]);
            }

            AverageVineLength = AverageVineLength / (SpinnerPositionsSmooth.Length - 1);


            Vector2[] nodes = data.Nodes;
            foreach (Vector2 value in nodes) {
                Nodes.Add(offset + value);
            }
            switch (data.Attr("fear_distance")) {
                case "close":
                    fearDistance = 16f;
                    break;
                case "medium":
                    fearDistance = 40f;
                    break;
                case "far":
                    fearDistance = 80f;
                    break;
            }
            int num = data.Int("slide_until");
            Create(fearDistance, num, 0, Nodes);
            createdFromLevel = true;

            WidthVecarr = new Vector2[SpinnerPositionsSmooth.Length];

            for(int i = SpinnerPositionsSmooth.Length-1; i >= 0; i--) 
            {
                if(i == SpinnerPositionsSmooth.Length - 1 || i == SpinnerPositionsSmooth.Length - 2) {
                    WidthVecarr[i] = SpinnerPositionsSmooth[SpinnerPositionsSmooth.Length - 1] - SpinnerPositionsSmooth[SpinnerPositionsSmooth.Length - 2];
                    WidthVecarr[i].Y = WidthVecarr[i].Y * -1;
                    float X = WidthVecarr[i].X;
                    WidthVecarr[i].X = WidthVecarr[i].Y;
                    WidthVecarr[i].Y = X;
                    WidthVecarr[i] = Vector2.Normalize(WidthVecarr[i]);
                }

                else if(i == 0) {
                    WidthVecarr[i] = SpinnerPositionsSmooth[i+1] - SpinnerPositionsSmooth[i];
                    WidthVecarr[i].Y = WidthVecarr[i].Y * -1;
                    float X = WidthVecarr[i].X;
                    WidthVecarr[i].X = WidthVecarr[i].Y;
                    WidthVecarr[i].Y = X;
                    WidthVecarr[i] = Vector2.Normalize(WidthVecarr[i]);
                }
                else {
                    WidthVecarr[i] = SpinnerPositionsSmooth[i] - SpinnerPositionsSmooth[i - 1];
                    WidthVecarr[i].Y = WidthVecarr[i].Y * -1;
                    float X = WidthVecarr[i].X;
                    WidthVecarr[i].X = WidthVecarr[i].Y;
                    WidthVecarr[i].Y = X;
                    WidthVecarr[i] = Vector2.Normalize(WidthVecarr[i]);
                    Vector2 Vec = WidthVecarr[i];
                    WidthVecarr[i] = SpinnerPositionsSmooth[i+1] - SpinnerPositionsSmooth[i];
                    WidthVecarr[i].Y = WidthVecarr[i].Y * -1;
                    X = WidthVecarr[i].X;
                    WidthVecarr[i].X = WidthVecarr[i].Y;
                    WidthVecarr[i].Y = X;
                    WidthVecarr[i] = Vector2.Normalize(WidthVecarr[i]);
                    WidthVecarr[i] = (WidthVecarr[i] + Vec) / 2;
                }
            }
            for(int i = 1; i < WidthVecarr.Length-1; i++) {
                WidthVecarr[i] = (WidthVecarr[i - 1] + WidthVecarr[i + 1]) / 2;
            }
        }

        IEnumerator SpawnGrowingSpinner(float WaitTime) {
            while (CurrentSpinnerNum < SpinnerPositions.Length) {
                yield return WaitTime;
                //if (currenspinnerimg != null)
                //{
                // Remove(currenspinnerimg);
                //}
                switch (CurrentGrowthCycle) {
                    case 0:
                            CurrentSpinner = new CrystalStaticSpinner(SpinnerPositions[CurrentSpinnerNum], false, CrystalColor.Blue);
                            DynData<CrystalStaticSpinner> CurrentSpinnerData1 = new DynData<CrystalStaticSpinner>(CurrentSpinner);
                            CurrentSpinnerData1.Set<bool>("expanded", true);
                            Scene.Add(CurrentSpinner);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner1"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = false;
                            DisableHitBox = true;
                        
                        CurrentGrowthCycle++;

                        break;
                    case 1:
                   
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner2"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = false;
                        
                        CurrentGrowthCycle++;
                        break;
                    case 2:
                      
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner3"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);

                            CurrentSpinner.Collidable = false;
                        
                        CurrentGrowthCycle++;
                        break;
                    case 3:
                        
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner4"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = false;
                        
                        CurrentGrowthCycle++;
                        break;

                    case 4:
                       
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner5"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = false;
                        
                        CurrentGrowthCycle++;
                        break;
                    case 5:
                       
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner6"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = false;
                        
                        CurrentGrowthCycle++;
                        break;
                    case 6:
                        
                            CurrentSpinner.Remove(currenspinnerimg);
                            CurrentSpinner.Add(currenspinnerimg = new Image(GFX.Game["objects/StrawberryJam2021/spinnerVine/WhiteSpinner7"]));
                            currenspinnerimg.Color = SpinnerColor;
                            currenspinnerimg.Position += new Vector2(-12, -12);
                            CurrentSpinner.Collidable = true;
                            DisableHitBox = false;
                        
                        CurrentSpinnerNum++;
                        CurrentGrowthCycle = 0;
                        break;
                }



            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            GrowDir = ((SpinnerPositionsSmooth[CurrentDirNum] - SpinnerPositionsSmooth[CurrentDirNum - 1]) / 60) / SpinnerWaitTime;
            Vector2[] SpinnerVec = new Vector2[SpinnerGrowthPos.Length + 1];
            SpinnerVec[SpinnerGrowthPos.Length] = SpinnerGrowthPos[SpinnerGrowthPos.Length - 1];

            for (int i = 0; i < SpinnerGrowthPos.Length; i++) {
                SpinnerVec[i] = SpinnerGrowthPos[i];
            }
            SpinnerGrowthPos = SpinnerVec;

            HasStartedGrowing = true;
        }

        public void Create(float fearDistance, int slideUntilIndex, int layer, List<Vector2> startNodes) {
            Nodes = new List<Vector2>();
            foreach (Vector2 startNode in startNodes) {
                Nodes.Add(startNode + new Vector2(Calc.Random.Range(-8, 8), Calc.Random.Range(-8, 8)));
            }
            base.Tag = Tags.TransitionUpdate;
            Position = Nodes[0];
            outwards = (Nodes[0] - Nodes[1]).SafeNormalize();
            this.fearDistance = fearDistance;
            this.slideUntilIndex = slideUntilIndex;



            fillers = GFX.Game.GetAtlasSubtextures("objects/StrawberryJam2021/spinnerVine/filler");
            tentacles = new Tentacle[1];
            float num2 = 0f;
            int num3 = 0;
            while (num3 < tentacles.Length && num2 < 440f) {
                tentacles[num3].Approach = 0.25f + Calc.Random.NextFloat() * 0.75f;
                tentacles[num3].Length = 32f + Calc.Random.NextFloat(64f);
                tentacles[num3].Width = 4f + Calc.Random.NextFloat(16f);
                tentacles[num3].Position = Position;
                tentacles[num3].WaveOffset = Calc.Random.NextFloat();
                tentacles[num3].TexIndex = Calc.Random.Next(arms.Count);
                tentacles[num3].FillerTexIndex = Calc.Random.Next(fillers.Count);
                tentacles[num3].LerpDuration = 0.5f + Calc.Random.NextFloat(0.25f);
                num2 += tentacles[num3].Width;
                num3++;
                tentacleCount++;
            }
            vertices = new VertexPositionColorTexture[tentacleCount * 12 * 6];
            for (int j = 0; j < vertices.Length; j++) {
                vertices[j].Color = color;
            }
        }

        private Vector2 TargetTentaclePosition(Tentacle tentacle, Vector2 position, float along) {
            Vector2 value;
            Vector2 value2 = (value = position - outwards * offset);
            if (player != null) {
                Vector2 value3 = outwards.Perpendicular();
                value = Calc.ClosestPointOnLine(value - value3 * 200f, value + value3 * 200f, player.Position);
            }
            Vector2 vector = value2 + outwards.Perpendicular() * (-220f + along + tentacle.Width * 0.5f);
            float scaleFactor = (value - vector).Length();
            return vector + outwards * scaleFactor * 0.6f;
        }



        public override void Update() {
            base.Update();
            if (DisableHitBox) {
                CurrentSpinner.Collidable = false;
            }
            Console.WriteLine("//////////////");
            if (HasStartedGrowing) {
                SpinnerGrowthPos[CurrentDirNum] += GrowDir;
                if (SpinnerGrowthPos[CurrentDirNum].X >= SpinnerPositionsSmooth[CurrentDirNum].X - 0.1 && SpinnerPositionsSmooth[CurrentDirNum].X <= SpinnerPositionsSmooth[CurrentDirNum].X + 0.1 && SpinnerGrowthPos[CurrentDirNum].Y >= SpinnerPositionsSmooth[CurrentDirNum].Y - 0.1 && SpinnerGrowthPos[CurrentDirNum].Y <= SpinnerPositionsSmooth[CurrentDirNum].Y + 0.1) {
                    if (SpinnerPositionsSmooth.Length - 2 >= CurrentDirNum) {
                        CurrentDirNum++;
                        SpinnerGrowthPos[SpinnerGrowthPos.Length - 1] = SpinnerPositionsSmooth[SpinnerGrowthPos.Length - 1];
                        GrowDir = ((SpinnerPositionsSmooth[CurrentDirNum] - SpinnerPositionsSmooth[CurrentDirNum - 1]) / 60) * SpinnerWaitTime;
                        Vector2[] SpinnerVec = new Vector2[SpinnerGrowthPos.Length + 1];
                        SpinnerVec[SpinnerGrowthPos.Length] = SpinnerGrowthPos[SpinnerGrowthPos.Length - 1];

                        for (int i = 0; i < SpinnerGrowthPos.Length; i++) {
                            SpinnerVec[i] = SpinnerGrowthPos[i];
                        }
                        SpinnerGrowthPos = SpinnerVec;
                        SpinnerGrowthPos[CurrentDirNum] += GrowDir;
                        Console.WriteLine("neww Dir");
                    } else {
                        HasStartedGrowing = false;
                    }
                }

            }
        }


        private void Quad(ref int n, Vector2 a, Vector2 b, Vector2 c, Vector2 d, MTexture subtexture = null) {
            if (subtexture == null) {
                subtexture = GFX.Game["util/pixel"];
            }
            float num = 1f / (float) subtexture.Texture.Texture_Safe.Width;
            float num2 = 1f / (float) subtexture.Texture.Texture_Safe.Height;
            Vector2 textureCoordinate = new Vector2((float) subtexture.ClipRect.Left * num, (float) subtexture.ClipRect.Top * num2);
            Vector2 textureCoordinate2 = new Vector2((float) subtexture.ClipRect.Right * num, (float) subtexture.ClipRect.Top * num2);
            Vector2 textureCoordinate3 = new Vector2((float) subtexture.ClipRect.Left * num, (float) subtexture.ClipRect.Bottom * num2);
            Vector2 textureCoordinate4 = new Vector2((float) subtexture.ClipRect.Right * num, (float) subtexture.ClipRect.Bottom * num2);
            vertices[n].Position = new Vector3(a, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate;
            vertices[n].Position = new Vector3(b, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate2;
            vertices[n].Position = new Vector3(d, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate3;
            vertices[n].Position = new Vector3(d, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate3;
            vertices[n].Position = new Vector3(b, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate2;
            vertices[n].Position = new Vector3(c, 0f);
            vertices[n++].TextureCoordinate = textureCoordinate4;
        }

        public override void Render() {

            foreach (MTexture atlasSubtexture in GFX.Game.GetAtlasSubtextures("scenery/tentacles/arms")) {
                MTexture[] array = new MTexture[10];
                int num = atlasSubtexture.Width / 10;
                for (int i = 0; i < 10; i++) {
                    array[i] = atlasSubtexture.GetSubtexture(num * (10 - i - 1), 0, num, atlasSubtexture.Height);
                }

                arms.Add(array);

            }
            Vector2 value5 = -outwards.Perpendicular();
            int n = 0;

            Vector2 position = tentacles[0].Position;
            Vector2 vector = value5 * (tentacles[0].Width * 0.5f + 2f);
            MTexture[] array2 = arms[tentacles[0].TexIndex];
            MTexture Tex123 = (GFX.Game["util/pixel"]);


            float LastWidthOffset = TentacleWidth;

            Vector2 LastWidthOffsetVec = TentacleWidth * WidthVecarr[0];

            float WidthOffset;

            for (int j = 0; j < SpinnerGrowthPos.Length-1; j++) {

                WidthOffset = TentacleWidth - ((MaxThin / SpinnerGrowthPos.Length) * (Vector2.Distance(SpinnerPositionsSmooth[CurrentDirNum], SpinnerPositionsSmooth[CurrentDirNum - 1]) / AverageVineLength))*j;
                Vector2 WidthOffsetVec = WidthOffset * WidthVecarr[j];

                Quad(ref n, SpinnerGrowthPos[j] - LastWidthOffsetVec, SpinnerGrowthPos[j] + LastWidthOffsetVec, SpinnerGrowthPos[j + 1] + WidthOffsetVec, SpinnerGrowthPos[j + 1] - WidthOffsetVec, Tex123);

                LastWidthOffsetVec = WidthOffsetVec;

                LastWidthOffset = WidthOffset;
            }


            vertexCount = n;

            if (vertexCount > 0) {
                GameplayRenderer.End();
                Engine.Graphics.GraphicsDevice.Textures[0] = arms[0][0].Texture.Texture_Safe;
                GFX.DrawVertices((base.Scene as Level).Camera.Matrix, vertices, vertexCount, GFX.FxTexture);
                GameplayRenderer.Begin();
            }



        }
    }
}


