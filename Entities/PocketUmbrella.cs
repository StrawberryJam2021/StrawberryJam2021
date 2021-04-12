using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    class PocketUmbrella : Glider {
        private float staminaCost;

        public PocketUmbrella(Vector2 position, bool bubble, bool tutorial, float cost) : base(position, bubble, tutorial) {
            staminaCost = cost;
        }

        public override void Update() {
            base.Update();
            if (Hold.IsHeld) {
                Hold.Holder.Stamina -= staminaCost * Engine.DeltaTime;
                if (Hold.Holder.Stamina <= 0) {
                    Hold.Holder.Drop();
                }
                return;
            }
            if (!(bool) PocketUmbrellaController.gliderDestroyed_FI.GetValue(this)) {
                Collidable = false;
                Hold.Active = false;
                PocketUmbrellaController.gliderDestroyed_FI.SetValue(this, true);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Add(new Coroutine((System.Collections.IEnumerator) PocketUmbrellaController.destroy_coroutine_MI.Invoke(this, new object[] { })));
            }
        }
    }
}
