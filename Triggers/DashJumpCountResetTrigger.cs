using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/DashJumpCountResetTrigger")]
    public class DashJumpCountResetTrigger : Trigger {
        private readonly JumpCount jumpCountVariant = ExtendedVariantsModule.Instance.VariantHandlers[ExtendedVariantsModule.Variant.JumpCount] as JumpCount;

        public DashJumpCountResetTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            var session = SceneAs<Level>().Session;

            jumpCountVariant.SetValue(1);
            session.Inventory.Dashes = 1;
        }
    }
}
