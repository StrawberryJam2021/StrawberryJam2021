using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/FlagDashSwitch")]
    class FlagDashSwitch : DashSwitch {

        private string flag;
        private bool persistent, target;
        private static FieldInfo ds_pressed, ds_pressDirection, ds_side, ds_pressedTarget, ds_startY;

        private Vector2 spriteOffset;
        private StaticMover mover;

        public FlagDashSwitch(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, chooseFacing(data.Bool("horizontal", false), data.Bool("leftSide", false), data.Bool("ceiling", false)), data.Bool("persistent", false), false, id, data.Attr("sprite", "default")) {
            persistent = data.Bool("persistent", false);
            target = data.Bool("flagTargetValue", true);
            flag = data.Attr("flag");
            if (data.Bool("attach", false)) {
                Add(mover = new StaticMover {
                    OnMove = new Action<Vector2>(staticMoverMove),
                    OnEnable = new Action(onEnable) ,
                    OnAttach = delegate (Platform p) { Depth = p.Depth + 1; },
                    OnShake = new Action<Vector2>(onShake),
                    SolidChecker = new Func<Solid, bool>((s) =>
                        (Sides) ds_side.GetValue(this) switch {
                            Sides.Down => CollideCheckOutside(s, Position + Vector2.UnitY * 4),
                            Sides.Up => CollideCheckOutside(s, Position - Vector2.UnitY * 4),
                            Sides.Left => CollideCheckOutside(s, Position - Vector2.UnitX * 2),
                            Sides.Right => CollideCheckOutside(s, Position + Vector2.UnitX * 2),
                            _ => false,
                        }
                )
                });
            }
            var v = (Sides) ds_side.GetValue(this);
            if (v == Sides.Up || v == Sides.Down) {
                Collider.Width = 16f;
                Collider.Height = 6f;
            } else {
                Collider.Width = 7f;
                Collider.Height = 16f;
            }

            if (v == Sides.Left) {
                Collider.Position.X += 1;
            } else if (v == Sides.Up) {
                Collider.Position.Y += 1;
            } else if (v == Sides.Down) {
                Collider.Height -= 1;
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Logger.Log("SJ2021/FDS", "added");
            if (SceneAs<Level>().Session.GetFlag(flag) == target) {
                Logger.Log("SJ2021/FDS", $"getflag:{flag}");
                if (!persistent) {
                    Logger.Log("SJ2021/FDS", "non persist");
                    SceneAs<Level>().Session.SetFlag(flag, false);
                }
            }
        }

        public static void Load() {
            ds_pressed = typeof(DashSwitch).GetField("pressed", BindingFlags.Instance | BindingFlags.NonPublic);
            ds_pressDirection = typeof(DashSwitch).GetField("pressDirection", BindingFlags.Instance | BindingFlags.NonPublic);
            ds_side = typeof(DashSwitch).GetField("side", BindingFlags.Instance | BindingFlags.NonPublic);
            ds_pressedTarget = typeof(DashSwitch).GetField("pressedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            ds_startY = typeof(DashSwitch).GetField("startY", BindingFlags.Instance | BindingFlags.NonPublic);

            On.Celeste.DashSwitch.GetGate += DashSwitch_GetGate;
            On.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;

            typeof(DashSwitch).GetField("pressedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public override void Update() {
            base.Update();
            if (Scene.OnInterval(1f)) {
                //Logger.Log("SJ2021/fds", $"speed: {Speed}");
            }
        }

        private static Sides chooseFacing(bool horizontal, bool leftSide, bool ceiling) {
            if (!horizontal) {
                return leftSide ? Sides.Left : Sides.Right;
            }
            return ceiling ? Sides.Up : Sides.Down;
        }

        private static TempleGate DashSwitch_GetGate(On.Celeste.DashSwitch.orig_GetGate orig, DashSwitch self) {
            if (self is FlagDashSwitch fds) {
                fds.SceneAs<Level>().Session.SetFlag(fds.flag, fds.target);
                return null;
            }
            return orig(self);
        }

        private static DashCollisionResults DashSwitch_OnDashed(On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction) {
            if (!(bool) ds_pressed.GetValue(self) && direction == (Vector2) ds_pressDirection.GetValue(self) && self is FlagDashSwitch fds && fds.mover != null) {
                fds.mover.TriggerPlatform();
            }
            return orig(self, player, direction);
        }

        private void onShake(Vector2 amount) {
            spriteOffset += amount;
        }

        private void staticMoverMove(Vector2 amount) {
            //Logger.Log("SJ2021/FDS", $"before: {Position}, move: {amount}, after: {Position + amount}");
            if ((Sides) ds_side.GetValue(this) == Sides.Down && !(bool) ds_pressed.GetValue(this)) {
                // up facing dashswitch must be handled seperately
                // manually tracking origpos, adjusting this.starty as needed?
                float v = (float) ds_startY.GetValue(this);
                ds_startY.SetValue(this, v + amount.Y);
            }
            Position += amount;
            Vector2 target = (Vector2) ds_pressedTarget.GetValue(this);
            ds_pressedTarget.SetValue(this, target + amount);
            if (GetPlayerRider() is Player p) {
                p.MoveV(amount.Y);
                p.MoveH(amount.X);
                /*if (HasPlayerOnTop()) {
                    movePlayerOnTop(p, amount);
                    return;
                }
                movePlayerOnSide(p, amount);*/
            }
        }

        private void onEnable() {
            Collidable = !(bool) ds_pressed.GetValue(this);
            Active = Visible = true;
            Speed = Vector2.Zero;
            Player p = Scene.Tracker.GetEntity<Player>();
            if (p != null && p.CollideCheck(this)) {
                stopClip(p);
            }
        }

        private float stopClip(Player p, int offset = 0) =>
            (Sides) ds_side.GetValue(this) switch {
                Sides.Down => p.Bottom = Top + (offset * Math.Sign(Top)),
                Sides.Up => p.Top = Bottom + (offset * Math.Sign(Bottom)),
                Sides.Left => p.Left = Right + (offset * Math.Sign(Right)),
                Sides.Right => p.Right = Left + (offset * Math.Sign(Left)),
                _ => 0
            };
}
}
