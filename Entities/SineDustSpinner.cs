using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SineDustSpinner")]
    class SineDustSpinner : DustStaticSpinner {
        private SineWave xSine, ySine;
        private Tween xTween, yTween;
        private Vector2 origPos, lastPos;
        private float xAmplitude, yAmplitude;
        private bool xLinear, yLinear;

        private static MethodInfo get_TimeLeft;

        public static void Load() {
            get_TimeLeft = typeof(Tween).GetMethod("set_TimeLeft", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public SineDustSpinner(EntityData data, Vector2 offset) : base(data, offset) {
            Collider = new Circle(6, 0, 0);

            // change the sprite to be the same as for moving dusties
            Remove(Sprite);
            Sprite.RemoveSelf();

            DustEdge edge;
            Remove(edge = Get<DustEdge>());
            edge.RemoveSelf();

            Add(Sprite = new DustGraphic(true, false, false));

            float xPeriod = data.Float("xPeriod", 1f), xPhase = data.Float("xPhase", 0f) * (float) Math.PI,
                yPeriod = data.Float("yPeriod", 1f), yPhase = data.Float("yPhase", 0f) * (float) Math.PI;

            xLinear = data.Bool("xLinear", false);
            yLinear = data.Bool("yLinear", false);
            xAmplitude = data.Width / 2;
            yAmplitude = data.Height / 2;

            if (xLinear) {
                Add(xTween = Tween.Create(Tween.TweenMode.Looping, duration: getAdjustedPeriodTime(Math.Abs(xPeriod))));
                xTween.Start(xPeriod < 0);
                get_TimeLeft.Invoke(xTween, new object[] { xTween.Duration * xPhase });
            } else if (data.Float("xPeriod", 1f) != 0) {
                Add(xSine = new SineWave(1 / xPeriod, xPhase));
            }
            if (yLinear) {
                Add(yTween = Tween.Create(Tween.TweenMode.Looping, duration: getAdjustedPeriodTime(Math.Abs(yPeriod))));
                yTween.Start(yPeriod < 0);
                get_TimeLeft.Invoke(yTween, new object[] { yTween.Duration * yPhase });
            } else if (data.Float("yPeriod", 1f) != 0) {
                Add(ySine = new SineWave(1 / yPeriod, yPhase));
            }
            Logger.Log("SDS", $"ydur {Math.Abs(yPeriod)}, xdur {Math.Abs(xPeriod)}");
            Position = Position + Vector2.UnitX * data.Width / 2 + Vector2.UnitY * data.Height / 2;
            origPos = Position;
        }

        /* there is some weird desync issue with some period values (e.g. 4) where, because (n*Engine.DeltaTime) != (x += Engine.DeltaTime n times), the dusty desyncs.
         * this function makes sure that *if* this difference is big enough to cause a tick difference from what it should be, the period gets adjusted to a different 
         * period that results in the intended amount of ticks.
         */
        private float getAdjustedPeriodTime(float origTime) {
            int actualTicks = 0;
            int intendedTicks = (int) Math.Ceiling(origTime / Engine.DeltaTime);
            float time = origTime;
            for (; !(time <= 0); actualTicks++) {
                time -= Engine.DeltaTime;
            }
            if (actualTicks > intendedTicks) {
                return origTime - (Engine.DeltaTime - 0.01f);
            } else if (actualTicks < intendedTicks) {
                return origTime + (Engine.DeltaTime - 0.01f);
            }
            return origTime;
        }

        public override void Update() {
            base.Update();
            lastPos = Position;
            Position = origPos + getXAdjust() + getYAdjust();
            Sprite.EyeDirection = Vector2.Normalize(Position - lastPos);
        }

        private Vector2 getYAdjust() {
            if (yLinear) {
                return Vector2.UnitY * (-yAmplitude + 2 * yAmplitude * yTween.Percent);
            }
            return (ySine == null) ? Vector2.Zero : (Vector2.UnitY * ySine.Value * yAmplitude);
        }

        private Vector2 getXAdjust() {
            if (xLinear) {
                return Vector2.UnitX * (-xAmplitude + 2 * xAmplitude * xTween.Percent);
            }
            return (xSine == null) ? Vector2.Zero : (Vector2.UnitX * xSine.Value * xAmplitude);
        }
    }
}
