using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SineDustSpinner")]
    class SineDustSpinner : DustStaticSpinner {
        private SineWave xSine, ySine;
        private Tween xTween, yTween;
        private Vector2 origPos;
        private float xAmplitude, yAmplitude;
        private bool xLinear, yLinear;
        public SineDustSpinner(EntityData data, Vector2 offset) : base(data, offset) {

            float xFrequency = 1 / data.Float("xPeriod", 1f), xPhase = data.Float("xPhaseDeg", 1f) * (float) (Math.PI / 180),
                yFrequency = 1 / data.Float("yPeriod", 1f), yPhase = data.Float("yPhaseDeg", 1f) * (float) (Math.PI / 180);

            xLinear = data.Bool("xLinear", false);
            yLinear = data.Bool("yLinear", false);
            xAmplitude = data.Width / 2;
            yAmplitude = data.Height / 2;

            if (xLinear) {
                float xPeriod = data.Float("xPeriod", 1f);
                Add(xTween = Tween.Create(Tween.TweenMode.Looping, duration: Math.Abs(xPeriod)));
                xTween.Start(xPeriod < 0);
            } else if (data.Float("xPeriod", 1f) != 0) {
                Add(xSine = new SineWave(xFrequency, xPhase));
            }
            if (yLinear) {
                float yPeriod = data.Float("yPeriod", 1f);
                Add(yTween = Tween.Create(Tween.TweenMode.Looping, duration: Math.Abs(yPeriod)));
                yTween.Start(yPeriod < 0);
            } else if (data.Float("yPeriod", 1f) != 0) {
                Add(ySine = new SineWave(yFrequency, yPhase));
            }

            Position = Position + Vector2.UnitX * data.Width / 2 + Vector2.UnitY * data.Height / 2;
            origPos = Position;
        }

        public override void Update() {
            base.Update();
            Position = origPos + getXAdjust() + getYAdjust();
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
