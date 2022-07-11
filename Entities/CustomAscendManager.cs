using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CustomAscendManager")]
    public class CustomAscendManager : Entity {
        private static char[] separators = { ',' };

        public Color[] StreakColors;

        private Color backgroundColor;
        private bool introLaunch;
        private float fade;
        private bool launchCompleted;
        private Level level;

        public CustomAscendManager(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Tag = Tags.TransitionUpdate;
            Depth = 8900;

            backgroundColor = Calc.HexToColor(data.Attr("backgroundColor", "75a0ab"));
            introLaunch = data.Bool("introLaunch");
            StreakColors = data.Attr("streakColors", "ffffff,e69ecb")
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => Calc.HexToColor(str.Trim()))
                .ToArray();
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            Add(new Coroutine(Routine(), true));
        }

        private IEnumerator Routine() {
            Player player = Scene.Tracker.GetEntity<Player>();
            while (player == null || player.Y > Y) {
                player = Scene.Tracker.GetEntity<Player>();
                yield return null;
            }

            Scene.Add(new Streaks(this));

            player.Sprite.Play("launch", false, false);
            player.Speed = Vector2.Zero;
            player.StateMachine.State = Player.StDummy;
            player.DummyGravity = false;
            player.DummyAutoAnimate = false;

            if (introLaunch) {
                FadeSnapTo(1f);
                level.Camera.Position = player.Center + new Vector2(-160f, -90f);
                yield return 2.3f;
            } else {
                yield return FadeTo(1f, 0.8f);
                yield return 0.5f;
            }

            level.CanRetry = false;
            player.Sprite.Play("launch", false, false);
            Audio.Play("event:/char/madeline/summit_flytonext", player.Position);
            yield return 0.25f;
            Vector2 from = player.Position;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 1f) {
                player.Position = Vector2.Lerp(from, from + new Vector2(0f, 60f), Ease.CubeInOut(p)) + Calc.Random.ShakeVector();
                Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
                yield return null;
            }

            Fader fader = new Fader();
            Scene.Add(fader);
            player.X = from.X;
            from = player.Position;
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);

            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 0.5f) {
                float y = player.Y;
                player.Position = Vector2.Lerp(from, from + new Vector2(0f, -160f), Ease.SineIn(p));
                if (p == 0f || Calc.OnInterval(player.Y, y, 16f)) {
                    level.Add(Engine.Pooler.Create<SpeedRing>().Init(player.Center, new Vector2(0f, -1f).Angle(), Color.White));
                }
                if (p >= 0.5f) {
                    fader.Fade = (p - 0.5f) * 2f;
                } else {
                    fader.Fade = 0f;
                }
                yield return null;
            }

            level.CanRetry = true;
            launchCompleted = true;
            player.Y = level.Bounds.Top;
            player.SummitLaunch(player.X);
            player.DummyGravity = true;
            player.DummyAutoAnimate = true;
            level.NextTransitionDuration = 0.05f;
            yield break;
        }

        public override void Render() {
            Draw.Rect(level.Camera.X - 10f, level.Camera.Y - 10f, 340f, 200f, backgroundColor * fade);
        }

        public override void Removed(Scene scene) {
            FadeSnapTo(0f);
            if (launchCompleted) {
                ScreenWipe.WipeColor = Color.White;
                AreaData.Get(level.Session).DoScreenWipe(Scene, wipeIn: true);
                ScreenWipe.WipeColor = Color.Black;
            }
            base.Removed(scene);
        }

        private IEnumerator FadeTo(float target, float duration = 0.8f) {
            while ((fade = Calc.Approach(fade, target, Engine.DeltaTime / duration)) != target) {
                FadeSnapTo(fade);
                yield return null;
            }
            FadeSnapTo(target);
            yield break;
        }

        private void FadeSnapTo(float target) {
            fade = target;
            SetSnowAlpha(1f - fade);
            SetBloom(fade * 0.1f);
        }

        private void SetBloom(float add) {
            level.Bloom.Base = AreaData.Get(level).BloomBase + add;
        }

        private void SetSnowAlpha(float value) {
            Snow snow = level.Foreground.Get<Snow>();
            if (snow != null) {
                snow.Alpha = value;
            }
            RainFG rainFG = level.Foreground.Get<RainFG>();
            if (rainFG != null) {
                rainFG.Alpha = value;
            }
            WindSnowFG windSnowFG = level.Foreground.Get<WindSnowFG>();
            if (windSnowFG != null) {
                windSnowFG.Alpha = value;
            }
        }

        private static float Mod(float x, float m) {
            return (x % m + m) % m;
        }

        public class Streaks : Entity {

            public float Alpha;

            private const float MinSpeed = 600f;
            private const float MaxSpeed = 2000f;

            private Particle[] particles;
            private List<MTexture> textures;
            private Color[] colors;
            private Color[] alphaColors;
            private CustomAscendManager manager;

            private class Particle {
                public Vector2 Position;
                public float Speed;
                public int Index;
                public int Color;
            }

            public Streaks(CustomAscendManager manager) {
                Alpha = 1f;
                particles = new Particle[80];
                this.manager = manager;
                if (manager != null) {
                    colors = manager.StreakColors;
                } else {
                    colors = new Color[]
                    {
                        Color.White,
                        Calc.HexToColor("e69ecb")
                    };
                }
                Depth = 20;
                textures = GFX.Game.GetAtlasSubtextures("scenery/launch/slice");
                alphaColors = new Color[colors.Length];
                for (int i = 0; i < particles.Length; i++) {
                    float num = 160f + Calc.Random.Range(24f, 144f) * Calc.Random.Choose(-1, 1);
                    float y = Calc.Random.NextFloat(436f);
                    float speed = Calc.ClampedMap(Math.Abs(num - 160f), 0f, 160f, 0.25f, 1f) * Calc.Random.Range(MinSpeed, MaxSpeed);
                    particles[i] = new Particle {
                        Position = new Vector2(num, y),
                        Speed = speed,
                        Index = Calc.Random.Next(textures.Count),
                        Color = Calc.Random.Next(colors.Length)
                    };
                }
            }

            public override void Update() {
                base.Update();
                for (int i = 0; i < particles.Length; i++) {
                    Particle particle = particles[i];
                    particle.Position.Y = particle.Position.Y + particles[i].Speed * Engine.DeltaTime;
                }
            }

            public override void Render() {
                float scale = Ease.SineInOut(((manager != null) ? manager.fade : 1f) * Alpha);
                Vector2 position = (Scene as Level).Camera.Position;
                for (int i = 0; i < colors.Length; i++) {
                    alphaColors[i] = colors[i] * scale;
                }
                for (int j = 0; j < particles.Length; j++) {
                    Vector2 vector = particles[j].Position;
                    vector.X = Mod(vector.X, 320f);
                    vector.Y = -128f + Mod(vector.Y, 436f);
                    vector += position;
                    Vector2 scale2 = new Vector2 {
                        X = Calc.ClampedMap(particles[j].Speed, MinSpeed, MaxSpeed, 1f, 0.25f),
                        Y = Calc.ClampedMap(particles[j].Speed, MinSpeed, MaxSpeed, 1f, 2f)
                    } * Calc.ClampedMap(particles[j].Speed, MinSpeed, MaxSpeed, 1f, 4f);
                    MTexture mtexture = textures[particles[j].Index];
                    Color color = alphaColors[particles[j].Color];
                    mtexture.DrawCentered(vector, color, scale2);
                }
                Draw.Rect(position.X - 10f, position.Y - 10f, 26f, 200f, alphaColors[0]);
                Draw.Rect(position.X + 320f - 16f, position.Y - 10f, 26f, 200f, alphaColors[0]);
            }
        }

        public class Fader : Entity {

            public float Fade;

            public Fader() {
                Depth = -1000010;
            }

            public override void Render() {
                if (Fade > 0f) {
                    Vector2 position = (Scene as Level).Camera.Position;
                    Draw.Rect(position.X - 10f, position.Y - 10f, 340f, 200f, Color.White * Fade);
                }
            }
        }
    }
}