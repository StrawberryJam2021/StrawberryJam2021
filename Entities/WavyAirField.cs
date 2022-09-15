using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WavyAirField")]
    public class WavyAirField : Entity {
        private float fieldWidth = 0f;
        private float fieldHeight = 0f;
        public WavyAirField(EntityData data, Vector2 offset) : base(data.Position + offset) {
            fieldWidth = data.Width;
            fieldHeight = data.Height;
            Add(new DisplacementRenderHook(new Action(RenderDisplacement)));
        }
        public void RenderDisplacement() {
            Color color = new Color(0.5f, 0.5f, 0.1f, 1f);
            Draw.Rect(X, Y, fieldWidth, fieldHeight, color);
        }

        public override void DebugRender(Camera camera) {
            base.DebugRender(camera);
            Draw.HollowRect(X, Y, fieldWidth, fieldHeight, Color.Red);
        }
    }
}
