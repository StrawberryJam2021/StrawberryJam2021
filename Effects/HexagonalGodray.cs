using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Adapted from Celeste.Godrays, most base code taken from said file
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Effects {
    public class HexagonalGodray : Backdrop {
        private class HexRay {
            public float X;

            public float Y;

            public float Percent;

            public float Duration;

            public float[] angles = new float[3];

            public int[] lengths = new int[3];

            public void Reset() {
                Percent = 0f;
                X = Calc.Random.NextFloat(384f);
                Y = Calc.Random.NextFloat(244f);
                Duration = 4f + Calc.Random.NextFloat() * 8f;
                int randRotate = Calc.Random.Next(0, 25);
                angles[0] = 3.14159F / 180F * (20 + randRotate);
                angles[1] = 3.14159F / 180F * (75 + randRotate);
                angles[2] = 3.14159F / 180F * (135 + randRotate);
                lengths[0] = Calc.Random.Next(15, 22);
                lengths[1] = lengths[0];
                lengths[2] = lengths[1];
            }
        }

        private const int RayCount = 6;

        private VertexPositionColor[] vertices;

        private int vertexCount;

        private Color rayColor = Calc.HexToColor("f52b63") * 0.5f;

        private Color fadeToColor;

        private HexRay[] rays = new HexRay[6];

        private float fade;

        private Color fadeColor;

        public HexagonalGodray(string color, string fadeToColor, int numRays) {
            vertices = new VertexPositionColor[12 * numRays];
            rayColor = Calc.HexToColor(color) * 0.5f;
            this.fadeToColor = Calc.HexToColor(fadeToColor) * 0.5f;
            UseSpritebatch = false;
            for (int i = 0; i < rays.Length; i++) {
                rays[i] = new();
                rays[i].Reset();
                rays[i].Percent = Calc.Random.NextFloat();
            }
        }

        public override void Update(Scene scene) {
            Level level = scene as Level;
            bool flag = IsVisible(level);
            fade = Calc.Approach(fade, flag ? 1 : 0, Engine.DeltaTime);
            Visible = fade > 0f;
            if (!Visible) {
                return;
            }
            Player entity = level.Tracker.GetEntity<Player>();
            Vector2 vector = Calc.AngleToVector(-1.67079639f, 1f); //probably supposed to be -pi/2. but isn't that isn't that just <0,-1>?
            Vector2 vector2 = new Vector2(0f - vector.Y, vector.X);
            int num = 0;
            for (int i = 0; i < rays.Length; i++) {
                if (rays[i].Percent >= 1f) {
                    rays[i].Reset();
                }
                rays[i].Percent += Engine.DeltaTime / rays[i].Duration;
                rays[i].Y += 8f * Engine.DeltaTime;
                float percent = rays[i].Percent;
                float num2 = -32f + Mod(rays[i].X - level.Camera.X * 0.9f, 384f);
                float num3 = -32f + Mod(rays[i].Y - level.Camera.Y * 0.9f, 244f);
                float[] angles = rays[i].angles;
                int[] lengths = rays[i].lengths;
                Vector2 vector3 = new Vector2((int) num2, (int) num3);
                Color color = rayColor * Ease.CubeInOut(Calc.Clamp(((percent < 0.5f) ? percent : (1f - percent)) * 2f, 0f, 1f)) * fade;
                if (entity != null) {
                    float num4 = (vector3 + level.Camera.Position - entity.Position).Length();
                    if (num4 < 64f) {
                        color *= 0.25f + 0.75f * (num4 / 64f);
                    }
                }

                Vector2 v0 = new((float) Math.Cos(angles[0]), (float) Math.Sin(angles[0]));
                Vector2 v1 = new((float) Math.Cos(angles[1]), (float) Math.Sin(angles[1]));
                Vector2 v2 = new((float) Math.Cos(angles[2]), (float) Math.Sin(angles[2]));

                VertexPositionColor vertexPositionColor = new VertexPositionColor(new Vector3(vector3 + v0 * lengths[0], 0f), color);
                VertexPositionColor vertexPositionColor2 = new VertexPositionColor(new Vector3(vector3 + v1 * lengths[1], 0f), color);
                VertexPositionColor vertexPositionColor3 = new VertexPositionColor(new Vector3(vector3 + v2 * lengths[2], 0f), color);
                VertexPositionColor vertexPositionColor4 = new VertexPositionColor(new Vector3(vector3 - v0 * lengths[0], 0f), color);
                VertexPositionColor vertexPositionColor5 = new VertexPositionColor(new Vector3(vector3 - v1 * lengths[1], 0f), color);
                VertexPositionColor vertexPositionColor6 = new VertexPositionColor(new Vector3(vector3 - v2 * lengths[2], 0f), color);

                vertices[num++] = vertexPositionColor;
                vertices[num++] = vertexPositionColor2;
                vertices[num++] = vertexPositionColor3;

                vertices[num++] = vertexPositionColor;
                vertices[num++] = vertexPositionColor3;
                vertices[num++] = vertexPositionColor4;

                vertices[num++] = vertexPositionColor;
                vertices[num++] = vertexPositionColor4;
                vertices[num++] = vertexPositionColor5;

                vertices[num++] = vertexPositionColor;
                vertices[num++] = vertexPositionColor5;
                vertices[num++] = vertexPositionColor6;
            }
            vertexCount = num;
        }

        private float Mod(float x, float m) {
            return (x % m + m) % m;
        }

        public override void Render(Scene scene) {
            if (vertexCount > 0 && fade > 0f) {
                GFX.DrawVertices(Matrix.Identity, vertices, vertexCount);
            }
        }
    }

}
