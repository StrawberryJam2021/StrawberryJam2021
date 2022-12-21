using Microsoft.Xna.Framework;
using System;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/FlagDashSwitch")]
    class FlagDashSwitch : DashSwitch {

        private string flag;
        private bool target;

        private Vector2 spriteOffset;
        private StaticMover mover;

        public FlagDashSwitch(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, chooseFacing(data.Enum<Sides>("orientation")), data.Bool("persistent", false), false, id, data.Attr("sprite", "default")) {
            target = data.Bool("flagTargetValue", true);
            flag = data.Attr("flag");
            if (data.Bool("attach", false)) {
                Add(mover = new StaticMover {
                    OnMove = new Action<Vector2>(staticMoverMove),
                    OnEnable = new Action(onEnable) ,
                    OnAttach = delegate (Platform p) { Depth = p.Depth + 1; },
                    OnShake = new Action<Vector2>(onShake),
                    SolidChecker = new Func<Solid, bool>((s) =>
                        side switch {
                            Sides.Down => CollideCheckOutside(s, Position + Vector2.UnitY * 4),
                            Sides.Up => CollideCheckOutside(s, Position - Vector2.UnitY * 4),
                            Sides.Left => CollideCheckOutside(s, Position - Vector2.UnitX * 2),
                            Sides.Right => CollideCheckOutside(s, Position + Vector2.UnitX * 2),
                            _ => false,
                        }
                )
                });
            }
            if (side == Sides.Up || side == Sides.Down) {
                Collider.Width = 16f;
                Collider.Height = 6f;
            } else {
                Collider.Width = 7f;
                Collider.Height = 16f;
            }

            if (side == Sides.Left) {
                Collider.Position.X += 1;
            } else if (side == Sides.Up) {
                Collider.Position.Y += 1;
            } else if (side == Sides.Down) {
                Collider.Height -= 1;
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (SceneAs<Level>().Session.GetFlag(flag) == target) {
                if (!persistent) {
                    SceneAs<Level>().Session.SetFlag(flag, false);
                } else {
                    pressed = true;
                }
            }
        }

        public static void Load() {
            On.Celeste.DashSwitch.GetGate += DashSwitch_GetGate;
            On.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
        }

        public static void Unload() {
            On.Celeste.DashSwitch.GetGate -= DashSwitch_GetGate;
            On.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
        }

        public override void Render() {
            Vector2 oldPos = Position;
            Position = Position + spriteOffset;
            base.Render();
            Position = oldPos;
        }

        private static Sides chooseFacing(Sides side) => // In order to keep consistent naming in Ahorn, the direction names are reversed, which means they must be un-reversed here.
            side switch {
                Sides.Up => Sides.Down,
                Sides.Down => Sides.Up,
                Sides.Left => Sides.Right,
                Sides.Right => Sides.Left,
                _ => throw new Exception("Unknown FlagDashSwitch direction!")
            };

        private static TempleGate DashSwitch_GetGate(On.Celeste.DashSwitch.orig_GetGate orig, DashSwitch self) {
            if (self is FlagDashSwitch fds) {
                fds.SceneAs<Level>().Session.SetFlag(fds.flag, fds.target);
                return null;
            }
            return orig(self);
        }

        private static DashCollisionResults DashSwitch_OnDashed(On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction) {
            if (!self.pressed && direction == self.pressDirection && self is FlagDashSwitch fds && fds.mover != null) {
                fds.mover.TriggerPlatform();
            }
            return orig(self, player, direction);
        }

        private void onShake(Vector2 amount) {
            spriteOffset += amount;
        }

        private void staticMoverMove(Vector2 amount) {
            if (side == Sides.Down && !pressed) {
                startY += amount.Y;
            }
            Position += amount;
            pressedTarget += amount;
            if (GetPlayerRider() is Player p) {
                p.MoveV(amount.Y);
                p.MoveH(amount.X);
            }
        }

        private void onEnable() {
            Collidable = !pressed;
            Active = Visible = true;
            Speed = Vector2.Zero;
            Player p = Scene.Tracker.GetEntity<Player>();
            if (p != null && p.CollideCheck(this)) {
                stopClip(p);
            }
        }

        private float stopClip(Player p, int offset = 0) =>
            side switch {
                Sides.Down => p.Bottom = Top + (offset * Math.Sign(Top)),
                Sides.Up => p.Top = Bottom + (offset * Math.Sign(Bottom)),
                Sides.Left => p.Left = Right + (offset * Math.Sign(Right)),
                Sides.Right => p.Right = Left + (offset * Math.Sign(Left)),
                _ => 0
            };
    }
}
