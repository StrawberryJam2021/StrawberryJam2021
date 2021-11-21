using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using FourWayDirection = Celeste.Mod.StrawberryJam2021.Entities.ZeroGBarrier.FourWayDirection;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    public class ZeroGTrigger : Trigger {


        public FourWayDirection direction;

        public ZeroGTrigger(EntityData data, Vector2 offset, FourWayDirection d) : base(data, offset) {
            direction = d;
        }

        public override void OnLeave(Player player) {
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
        public void SetZeroG(bool on) {
            if (on) {
                ExtendedVariants.Module.ExtendedVariantsModule.Settings.Gravity = 0;
                ExtendedVariants.Module.ExtendedVariantsModule.Settings.AirFriction = -1;
                StrawberryJam2021Module.Session.ZeroG = true;
            } else {
                ExtendedVariants.Module.ExtendedVariantsModule.Settings.Gravity = 10;
                ExtendedVariants.Module.ExtendedVariantsModule.Settings.AirFriction = 10;
                StrawberryJam2021Module.Session.ZeroG = false;
            }
        }
    }
}
