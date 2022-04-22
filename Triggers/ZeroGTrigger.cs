using Microsoft.Xna.Framework;
using FourWayDirection = Celeste.Mod.StrawberryJam2021.Entities.ZeroGBarrier.FourWayDirection;
using Celeste.Mod.Entities;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/ZeroGTrigger = Load")]
    public class ZeroGTrigger : Trigger {

        public static Entity Load(Level level, LevelData levelData, Vector2 offset, EntityData entityData) => new ZeroGTrigger(entityData, offset);

        public FourWayDirection direction;
        public int OnlyOnJustRespawn; // 0 = used for barrier only, 1 = turn off always, 2 = turn on always

        public ZeroGTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            OnlyOnJustRespawn = data.Bool("state", true) ? 2 : 1;
            direction = FourWayDirection.Up;
        }

        public ZeroGTrigger(EntityData data, Vector2 offset, FourWayDirection d) : base(data, offset) {
            direction = d;
            OnlyOnJustRespawn = 0;
        }

        public override void OnEnter(Player player) {
            if (OnlyOnJustRespawn > 0 && player.JustRespawned) {
                base.OnEnter(player);
                Triggered = true;
                SetZeroG(OnlyOnJustRespawn == 2);
                RemoveSelf();
            }
        }

        public override void OnLeave(Player player) {
            if (OnlyOnJustRespawn == 0) {
                base.OnLeave(player);
                switch (direction) {
                    case FourWayDirection.Right:
                        if (player.Right <= Left) {
                            SetZeroG(false);
                        } else if (player.Left >= Right) {
                            SetZeroG(true);
                        }
                        break;
                    case FourWayDirection.Left:
                        if (player.Right <= Left) {
                            SetZeroG(true);
                        } else if (player.Left >= Right) {
                            SetZeroG(false);
                        }
                        break;
                    case FourWayDirection.Up:
                        if (player.Bottom <= Top) {
                            SetZeroG(true);
                        } else if (player.Top >= Bottom) {
                            SetZeroG(false);
                        }
                        break;
                    case FourWayDirection.Down:
                        if (player.Bottom <= Top) {
                            SetZeroG(false);
                        } else if (player.Top >= Bottom) {
                            SetZeroG(true);
                        }
                        break;
                }
            }
        }

        public void SetZeroG(bool on) {
            ExtendedVariants.ExtendedVariantTriggerManager manager = ExtendedVariants.Module.ExtendedVariantsModule.Instance.TriggerManager;
            if (on) {
                manager.OnEnteredInTrigger(ExtendedVariants.Module.ExtendedVariantsModule.Variant.Gravity, 0f, false, false, false, false);
                manager.OnEnteredInTrigger(ExtendedVariants.Module.ExtendedVariantsModule.Variant.AirFriction, 0f, false, false, false, false);
                StrawberryJam2021Module.Session.ZeroG = true;
            } else {
                manager.OnEnteredInTrigger(ExtendedVariants.Module.ExtendedVariantsModule.Variant.Gravity, 1f, false, false, false, false);
                manager.OnEnteredInTrigger(ExtendedVariants.Module.ExtendedVariantsModule.Variant.AirFriction, 1f, false, false, false, false);
                StrawberryJam2021Module.Session.ZeroG = false;
            }
        }
    }
}
