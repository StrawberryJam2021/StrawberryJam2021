using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class LightOptionBadelineDummy : BadelineDummy {
        private bool renderLight;
        public LightOptionBadelineDummy(Vector2 position, bool renderLight) 
            : base(position) 
        {
            this.renderLight = renderLight;
        }

        public override void Update() {
            base.Update();
            Light.Visible = renderLight;
        }
    }
}
