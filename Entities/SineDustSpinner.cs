using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SineDustSpinner")]
    public class SineDustSpinner : DustStaticSpinner {
        private readonly Vector2 origPos;
        private Vector2 lastPos;
        private readonly float xAmplitude;
        private readonly float yAmplitude;
        private readonly float xPeriod;
        private readonly float xPhase;
        private readonly float yPeriod;
        private readonly float yPhase;
        private readonly bool xLinear;
        private readonly bool yLinear;

        private float TimeSinceAwake;

        public SineDustSpinner(EntityData data, Vector2 offset) : base(data, offset) {
            Collider = new Circle(6, 0, 0);

            // change the sprite to be the same as for moving dusties
            Remove(Sprite);
            Remove(Get<DustEdge>());
            Remove(Get<TransitionListener>());

            Add(Sprite = new DustGraphic(true, false, false));

            xPeriod = data.Float("xPeriod", 1f);
            xPhase = data.Float("xPhase", 0f);
            yPeriod = data.Float("yPeriod", 1f);
            yPhase = data.Float("yPhase", 0f);

            xLinear = data.Bool("xLinear", false);
            yLinear = data.Bool("yLinear", false);
            xAmplitude = data.Width / 2f;
            yAmplitude = data.Height / 2f;

            origPos = Position + Vector2.UnitX * data.Width / 2 + Vector2.UnitY * data.Height / 2;
            Vector2 p = origPos
                + Vector2.UnitY * getOffset(yPeriod, yPhase, yAmplitude, yLinear, -0.1f)
                + Vector2.UnitX * getOffset(xPeriod, xPhase, xAmplitude, xLinear, -0.1f);
            Position = origPos
                + Vector2.UnitY * getOffset(yPeriod, yPhase, yAmplitude, yLinear, 0f)
                + Vector2.UnitX * getOffset(xPeriod, xPhase, xAmplitude, xLinear, 0f);
            Sprite.EyeDirection = Vector2.Normalize(Position - p);
        }

        public override void Update() {
            TimeSinceAwake += Engine.DeltaTime;
            base.Update();
            lastPos = Position;
            Position = origPos + getXAdjust() + getYAdjust();
            Sprite.EyeDirection = Vector2.Normalize(Position - lastPos);
            if (Scene.OnInterval(0.02f)) {
                SceneAs<Level>().ParticlesBG.Emit(P_Move, 1, Position, Vector2.One * 4f);
            }
        }

        private Vector2 getYAdjust() {
            return Vector2.UnitY * getOffset(yPeriod, yPhase, yAmplitude, yLinear);
        }

        private Vector2 getXAdjust() {
            return Vector2.UnitX * getOffset(xPeriod, xPhase, xAmplitude, xLinear);
        }


        private float getOffset(float period, float phase, float amplitude, bool linear, float? timeOverride = null) {
            if (period == 0)
                return 0;

            float time = (timeOverride is not null) ? timeOverride.Value : TimeSinceAwake;
            int adjust = linear ? 1 : 2;

            phase %= adjust;
            float completion = ((time + (phase / adjust) * period) % Math.Abs(period)) / period;
            if (completion > 1 || completion < 0) {
                completion -= Math.Sign(completion) * (int) (Math.Abs(completion) + 1);
            }
            return linear ? -amplitude + 2 * amplitude * completion : (float) Math.Sin(completion * Math.PI * 2) * amplitude;
        }
    }
}
