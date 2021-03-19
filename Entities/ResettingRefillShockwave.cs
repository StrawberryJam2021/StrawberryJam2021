using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class ResettingRefillShockwave : Entity {
        private readonly MTexture origTexture;
        private MTexture distortionTexture;

        private readonly int width;
        private readonly int height;

        private int x;

        public bool Enabled = true;

        public ResettingRefillShockwave(Vector2 position, int width, int height) {
            this.height = height;
            this.width = width;

            Depth = -1000000;
            Position = position;
            origTexture = GFX.Game["util/displacementcirclehollow"];
            distortionTexture = origTexture.GetSubtexture(0, 0, width, height);
            Add(new DisplacementRenderHook(RenderDisplacement));
        }

        public override void Update() {
            base.Update();
            distortionTexture = origTexture.GetSubtexture(x++, 0, width, height);

            Logger.Log("shockwave", $"x: {x} width: {width} height: {height}");

            if (x > 200) {
                x = 0;
            }
        }

        private void RenderDisplacement() {
            if (Enabled) distortionTexture.DrawCentered(Position, Color.White * 0.8f);
        }
    }
}
