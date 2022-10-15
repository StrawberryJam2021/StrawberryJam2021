using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/CassetteMusicTransitionController")]
    public class CassetteMusicTransitionController : Entity {

        public CassetteMusicTransitionController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            CassetteBlockManager manager = scene.Tracker.GetEntity<CassetteBlockManager>();

            if (manager != null) {
                TransitionListener listener = manager.Get<TransitionListener>();

                if (listener != null) {
                    listener.OnOut = (time) => {
                        if (manager.Scene != null) {
                            manager.Update();
                        }
                    };
                }
            }
        }
    }
}
