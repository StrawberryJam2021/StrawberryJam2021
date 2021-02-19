using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/DashZipMover")]
    public class DashZipMover : Solid {

        private class DashZipMoverPathRenderer : Entity {
            public DashZipMover zipMover;

            private MTexture cog;

            private Vector2 from;
            private Vector2 to;

            private Vector2 sparkAdd;
            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            private float length;

            public DashZipMoverPathRenderer(DashZipMover zipMover) {
                Depth = Depths.SolidsBelow;
                this.zipMover = zipMover;

                from = zipMover.start + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);
                to = zipMover.target + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);

                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float num = (from - to).Angle();
                length = (to - from).Length();

                sparkDirFromA = num + (float) Math.PI / 8f;
                sparkDirFromB = num - (float) Math.PI / 8f;
                sparkDirToA = num + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = num + (float) Math.PI + (float) Math.PI / 8f;

                cog = GFX.Game["objects/StrawberryJam2021/dashZipMover/cog"];
            }

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render() {
                DrawCogs(Vector2.UnitY, ropeShadow);
                DrawCogs(Vector2.Zero);
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;

                float rotation = zipMover.percent * (float) Math.PI * 2f;
                Vector2 perp = vector.Perpendicular();
                Vector2 perpNormalized = vector.Perpendicular();

                for (float num = 4f - zipMover.percent * (float) Math.PI * 8f % 4f; num < length; num += 4f) {
                    float prevNum = num - 4;

                    float progress = length == 0f ? 0 : num / length;
                    float sinAmount = progress * (1 - progress) * 8;

                    Vector2 sinOffset = perpNormalized * (float) Math.Sin(num) * sinAmount;
                    Vector2 prevSinOffset = perpNormalized * (float) Math.Sin(prevNum) * sinAmount;

                    Vector2 p1to = from + value + perp + vector * num + sinOffset;
                    Vector2 p2to = to + value2 - vector * num + sinOffset;
                    Vector2 p1from = from + value + perp + vector * prevNum + prevSinOffset;
                    Vector2 p2from = to + value2 - vector * prevNum + prevSinOffset;

                    Draw.Line(p1from + offset, p1to + offset, colorOverride ?? ropeColor);
                    Draw.Line(p2from + offset, p2to + offset, colorOverride ?? ropeColor);
                    if (colorOverride != null) {
                        Draw.Line(p1from + offset, p1to + offset, (Color) colorOverride, 3);
                        Draw.Line(p2from + offset, p2to + offset, (Color) colorOverride, 3);
                    }

                    Draw.Line(p1to + offset, p1to + vector * 4f + offset, colorOverride ?? ropeLightColor);
                    Draw.Line(p2to + offset, p2to - vector * 4f + offset, colorOverride ?? ropeLightColor);
                }

                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
            }
        }

        private MTexture[,] edges = new MTexture[3, 3];

        private Sprite streetlight;
        private BloomPoint bloom;

        private DashZipMoverPathRenderer pathRenderer;
        private List<MTexture> innerCogs;
        private MTexture temp = new MTexture();

        private Vector2 start;
        private Vector2 target;
        private float percent;
        private bool triggered;

        private Vector2 scale = Vector2.One;

        private static readonly Color ropeColor = Calc.HexToColor("046e19");
        private static readonly Color ropeLightColor = Calc.HexToColor("329415");
        private static readonly Color ropeShadow = Calc.HexToColor("003622");

        private SoundSource sfx = new SoundSource();

        public DashZipMover(Vector2 position, int width, int height, Vector2 target)
            : base(position, width, height, safe: false) {
            Depth = Depths.FGTerrain + 1;
            start = Position;
            this.target = target;

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());

            string path = "objects/zipmover/moon/light";
            string id = "objects/zipmover/moon/block";
            string key = "objects/StrawberryJam2021/dashZipMover/innercog";

            innerCogs = GFX.Game.GetAtlasSubtextures(key);

            Add(streetlight = new Sprite(GFX.Game, path));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);

            Add(bloom = new BloomPoint(1f, 6f));
            bloom.Position = new Vector2(Width / 2f, 10f);

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game[id].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            SurfaceSoundIndex = SurfaceIndex.Girder;

            OnDashCollide = OnDashed;

            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);
        }

        public DashZipMover(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset) {
        }

        public DashCollisionResults OnDashed(Player player, Vector2 dir) {
            if (!triggered) {
                triggered = true;

                scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);

                //Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_1", Center);
                // Was a test sound (for the smash vibe), cannot use because of never ending event with unrelated SFX.
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new DashZipMoverPathRenderer(this));
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update() {
            base.Update();

            scale = Calc.Approach(scale, Vector2.One, 3f * Engine.DeltaTime);

            streetlight.Scale = scale;
            Vector2 zeroCenter = new Vector2(Width, Height) / 2f;
            streetlight.Position = zeroCenter + (new Vector2(zeroCenter.X - streetlight.Width / 2f, 0) - zeroCenter) * scale;
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;

            Rectangle rect = new Rectangle(
                (int) (Center.X + (X + 2 - Center.X) * scale.X),
                (int) (Center.Y + (Y + 2 - Center.Y) * scale.Y),
                (int) ((Width - 4) * scale.X),
                (int) ((Height - 4) * scale.Y));

            Draw.Rect(rect, Color.Black);

            int num = 1;
            float num2 = 0f;
            int count = innerCogs.Count;

            for (int i = 4; i <= Height - 4f; i += 8) {
                int num3 = num;
                for (int j = 4; j <= Width - 4f; j += 8) {
                    int index = (int) (Mod((num2 + (num * percent * (float) Math.PI * 4f)) / ((float) Math.PI / 2f), 1f) * count);
                    
                    MTexture mTexture = innerCogs[index];
                    Rectangle rectangle = new Rectangle(0, 0, mTexture.Width, mTexture.Height);
                    Vector2 zero = Vector2.Zero;

                    if (j <= 4) {
                        zero.X = 2f;
                        rectangle.X = 2;
                        rectangle.Width -= 2;
                    } else if (j >= Width - 4f) {
                        zero.X = -2f;
                        rectangle.Width -= 2;
                    }

                    if (i <= 4) {
                        zero.Y = 2f;
                        rectangle.Y = 2;
                        rectangle.Height -= 2;
                    } else if (i >= Height - 4f) {
                        zero.Y = -2f;
                        rectangle.Height -= 2;
                    }

                    mTexture = mTexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
                    Vector2 pos = Center + ((Position + new Vector2(j, i) + zero) - base.Center) * scale;
                    mTexture.DrawCentered(pos, Color.White * ((num < 0) ? 0.5f : 1f), scale);
                    
                    num = -num;
                    num2 += (float) Math.PI / 3f;
                }
                if (num3 == num) {
                    num = -num;
                }
            }

            for (int k = 0; k < Width / 8f; k++) {
                for (int l = 0; l < Height / 8f; l++) {
                    int num4 = ((k != 0) ? ((k != Width / 8f - 1f) ? 1 : 2) : 0);
                    int num5 = ((l != 0) ? ((l != Height / 8f - 1f) ? 1 : 2) : 0);

                    if (num4 != 1 || num5 != 1) {
                        Vector2 pos = Center + (new Vector2(X + k * 8 + 4, Y + l * 8 + 4) - base.Center) * scale;
                        edges[num4, num5].DrawCentered(pos, Color.White, scale);
                    }
                }
            }

            base.Render();

            Position = position;
        }

        private void ScrapeParticlesCheck(Vector2 to) {
            if (!Scene.OnInterval(0.03f)) {
                return;
            }

            bool flag = to.Y != ExactPosition.Y;
            bool flag2 = to.X != ExactPosition.X;

            if (flag && !flag2) {
                int num = Math.Sign(to.Y - ExactPosition.Y);
                int num2 = 4;
                Vector2 value = ((num != 1) ? TopLeft : BottomLeft);

                if (num == 1) {
                    num2 = Math.Min((int) Height - 12, 20);
                }

                int num3 = (int) Height;

                if (num == -1) {
                    num3 = Math.Max(16, (int) Height - 16);
                }

                if (Scene.CollideCheck<Solid>(value + new Vector2(-2f, num * -2))) {
                    for (int i = num2; i < num3; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + num * 2f), (num == 1) ? (-(float) Math.PI / 4f) : ((float) Math.PI / 4f));
                    }
                }

                if (Scene.CollideCheck<Solid>(value + new Vector2(Width + 2f, num * -2))) {
                    for (int j = num2; j < num3; j += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, j + num * 2f), (num == 1) ? ((float) Math.PI * -3f / 4f) : ((float) Math.PI * 3f / 4f));
                    }
                }

            } else {
                if (!flag2 || flag) {
                    return;
                }

                int num4 = Math.Sign(to.X - ExactPosition.X);
                Vector2 value2 = ((num4 != 1) ? TopLeft : TopRight);
                int num5 = 4;

                if (num4 == 1) {
                    num5 = Math.Min((int) Width - 12, 20);
                }

                int num6 = (int) Width;

                if (num4 == -1) {
                    num6 = Math.Max(16, (int) Width - 16);
                }

                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, -2f))) {
                    for (int k = num5; k < num6; k += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(k + num4 * 2f, -1f), (num4 == 1) ? ((float) Math.PI * 3f / 4f) : ((float) Math.PI / 4f));
                    }
                }

                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, Height + 2f))) {
                    for (int l = num5; l < num6; l += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(l + num4 * 2f, 0f), (num4 == 1) ? ((float) Math.PI * -3f / 4f) : (-(float) Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;

            while (true) {
                if (!triggered) {
                    yield return null;
                    continue;
                }


                sfx.Play("event:/new_content/game/10_farewell/zip_mover");
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;

                float at2 = 0f;

                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at2);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    ScrapeParticlesCheck(vector);
                    if (Scene.OnInterval(0.1f)) {
                        pathRenderer.CreateSparks();
                    }
                    MoveTo(vector);
                }

                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;

                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                at2 = 0f;

                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                    percent = 1f - Ease.SineIn(at2);
                    Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at2));
                    MoveTo(position);
                }

                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                streetlight.SetAnimationFrame(1);
                triggered = false;
                yield return 0.5f;
            }
        }

        // Works with negatives, might be useful to move this in some util class?
        private float Mod(float x, float m) {
            return (x % m + m) % m;
        }
    }
}
