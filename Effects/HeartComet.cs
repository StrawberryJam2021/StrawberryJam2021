using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Effects {
    public class HeartComet : Backdrop{
        private List<MTexture> texture;
        private float timer;
        private float rateFPS;
        public bool Activated;
        private Vector2 position;

        public HeartComet(int x, int y) {
            texture = GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/heartComet");
            timer = 0f;
            rateFPS = 10f;
            Activated = false;
            position = new Vector2(x, y);
        }

        public override void Render(Scene scene) {
            base.Render(scene);
            
            if (Activated) {
                timer += Engine.DeltaTime;

                int frame = (int) (timer * rateFPS);
                if (frame < texture.Count) {
                    texture[frame].DrawCentered(position);
                }
            }
        }
    }
}
