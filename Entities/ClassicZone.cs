using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ClassicZone")]
    [Tracked(false)]
    public class ClassicZone : Entity {
        private ClassicZoneController controller;

        private struct Cloud {
            public Vector2 Position;

            public float Speed;

            public float Width;
        }

        private static readonly Color ActiveBackColor = Color.Black;

        private static readonly Color DisabledBackColor = Calc.HexToColor("1f2e2d");

        private static readonly Color ActiveLineColor = Color.White;

        private static readonly Color DisabledLineColor = Calc.HexToColor("6a8480");

        public bool PlayerHasDreamDash { get; private set; }

        private Vector2? node;

        private Cloud[] clouds;

        private float whiteFill = 0f;

        private float whiteHeight = 1f;

        private bool fastMoving;

        private float wobbleFrom = Calc.Random.NextFloat((float) Math.PI * 2f);

        private float wobbleTo = Calc.Random.NextFloat((float) Math.PI * 2f);

        private float wobbleEase = 0f;

        public ClassicZone(Vector2 position, float width, float height, Vector2? node, bool fastMoving)
            : base(position) {
            Depth = Depths.Below;
            Collider = new Hitbox(width, height);
            this.node = node;
            this.fastMoving = fastMoving;
        }

        public ClassicZone(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.FirstNodeNullable(offset),
                data.Bool("fastMoving")) { }

        public override void Added(Scene scene) {
            base.Added(scene);
            controller = scene.Tracker.GetEntity<ClassicZoneController>();
            if (controller == null) {
                controller = scene.CreateAndAdd<ClassicZoneController>();
            }

            PlayerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
            if (PlayerHasDreamDash && node.HasValue) {
                Vector2 start = Position;
                Vector2 end = node.Value;
                float num = Vector2.Distance(start, end) / 12f;
                if (fastMoving) {
                    num /= 3f;
                }

                Tween tween = Tween.Create(Tween.TweenMode.YoyoLooping, Ease.SineInOut, num, start: true);
                tween.OnUpdate = delegate(Tween t) {
                    Vector2 target = Vector2.Lerp(start, end, t.Eased);
                    Position = target.Round();
                };
                Add(tween);
            }

            Setup();
        }

        public void Setup() {
            clouds = new Cloud[17];
            for (int i = 0; i < 17; i++) {
                clouds[i] = new Cloud() {
                    Position = new Vector2(Calc.Random.NextFloat(128), Calc.Random.NextFloat(128)),
                    Speed = 1 + Calc.Random.NextFloat(4),
                    Width = 32 + Calc.Random.NextFloat(32)
                };
            }
        }


        public override void Update() {
            base.Update();
            if (PlayerHasDreamDash) {
                wobbleEase += Engine.DeltaTime * 2f;
                if (wobbleEase > 1f) {
                    wobbleEase = 0f;
                    wobbleFrom = wobbleTo;
                    wobbleTo = Calc.Random.NextFloat((float) Math.PI * 2f);
                }
            }
        }

        public override void Render() {
            Camera camera = SceneAs<Level>().Camera;
            if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom) {
                return;
            }

            Draw.Rect(X, Y, Width, Height,
                PlayerHasDreamDash ? ActiveBackColor : DisabledBackColor);
            Vector2 position = SceneAs<Level>().Camera.Position;
            // TODO: Look into stenciling, Y axis also needs clipping
            for (int i = 0; i < clouds.Length; i++) {
                if (!Scene.Paused && !controller.SkipFrame)
                    clouds[i].Position.X += PlayerHasDreamDash ? clouds[i].Speed : clouds[i].Speed / 8f;

                if (clouds[i].Position.X + clouds[i].Width > 0 && clouds[i].Position.X < Width) {
                    Vector2 pos;
                    float width;
                    if (clouds[i].Position.X < 0) {
                        pos = new Vector2(Position.X, Position.Y + clouds[i].Position.Y);
                        width = clouds[i].Width + clouds[i].Position.X;
                    } else if (clouds[i].Position.X + clouds[i].Width > Width) {
                        pos = Position + clouds[i].Position;
                        width = Width - clouds[i].Position.X;
                    } else {
                        pos = Position + clouds[i].Position;
                        width = clouds[i].Width;
                    }

                    Draw.Rect(pos, width, 4 + (1 - clouds[i].Width / 64) * 12,
                        PlayerHasDreamDash ? new Color(29, 43, 83) : DisabledLineColor);
                }

                if (!Scene.Paused && clouds[i].Position.X > Width && !controller.SkipFrame) {
                    clouds[i].Position.X = -clouds[i].Width;
                    clouds[i].Position.Y = Calc.Random.NextFloat(Height - 8);
                }
            }

            if (whiteFill > 0f) {
                Draw.Rect(X, Y, Width, Height * whiteHeight,
                    Color.White * whiteFill);
            }

            WobbleLine(new Vector2(X, Y), new Vector2(X + Width, Y), 0f);
            WobbleLine(new Vector2(X + Width, Y),
                new Vector2(X + Width, Y + Height), 0.7f);
            WobbleLine(new Vector2(X + Width, Y + Height),
                new Vector2(X, Y + Height), 1.5f);
            WobbleLine(new Vector2(X, Y + Height), new Vector2(X, Y), 2.5f);
            Draw.Rect(new Vector2(X, Y), 2f, 2f,
                PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor);
            Draw.Rect(new Vector2(X + Width - 2f, Y), 2f, 2f,
                PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor);
            Draw.Rect(new Vector2(X, Y + Height - 2f), 2f, 2f,
                PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor);
            Draw.Rect(new Vector2(X + Width - 2f, Y + Height - 2f), 2f, 2f,
                PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor);
        }

        private void WobbleLine(Vector2 from, Vector2 to, float offset) {
            float num = (to - from).Length();
            Vector2 value = Vector2.Normalize(to - from);
            Vector2 vector = new Vector2(value.Y, 0f - value.X);
            Color color = (PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor);
            Color color2 = (PlayerHasDreamDash ? ActiveBackColor : DisabledBackColor);
            if (whiteFill > 0f) {
                color = Color.Lerp(color, Color.White, whiteFill);
                color2 = Color.Lerp(color2, Color.White, whiteFill);
            }

            float scaleFactor = 0f;
            int num2 = 16;
            for (int i = 2; (float) i < num - 2f; i += num2) {
                float num3 = Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i),
                    wobbleEase);
                if ((float) (i + num2) >= num) {
                    num3 = 0f;
                }

                float num4 = Math.Min(num2, num - 2f - (float) i);
                Vector2 vector2 = from + value * i + vector * scaleFactor;
                Vector2 vector3 = from + value * ((float) i + num4) + vector * num3;
                Draw.Line(vector2 - vector, vector3 - vector, color2);
                Draw.Line(vector2 - vector * 2f, vector3 - vector * 2f, color2);
                Draw.Line(vector2, vector3, color);
                scaleFactor = num3;
            }
        }

        private float LineAmplitude(float seed, float index) {
            return (float) (Math.Sin((double) (seed + index / 16f) +
                                     Math.Sin(seed * 2f + index / 32f) * 6.2831854820251465) + 1.0) * 1.5f;
        }

        private float Lerp(float a, float b, float percent) {
            return a + (b - a) * percent;
        }
    }
}