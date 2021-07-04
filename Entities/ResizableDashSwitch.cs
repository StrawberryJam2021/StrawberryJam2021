using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ResizableDashSwitch")]
    public class ResizableDashSwitch : DashSwitch {
        public Sides Side;
        public Switch Switch;

        private DynamicData baseData;
        private Vector2 spriteOffset;

        private Vector2 pressedTarget {
            get => baseData.Get<Vector2>("pressedTarget");
            set => baseData.Set("pressedTarget", value);
        }
        private float startY {
            get => baseData.Get<float>("startY");
            set => baseData.Set("startY", value);
        }
        private Sprite sprite {
            get => baseData.Get<Sprite>("sprite");
            set => baseData.Set("sprite", value);
        }
        private bool persistent {
            get => baseData.Get<bool>("persistent");
            set => baseData.Set("persistent", value);
        }
        private bool pressed {
            get => baseData.Get<bool>("pressed");
            set => baseData.Set("pressed", value);
        }
        private string FlagName => baseData.Get<string>("FlagName");

        public ResizableDashSwitch(Vector2 position, Sides side, bool persistent, EntityID id, int width, bool actLikeTouchSwitch, bool attachToSolid)
            : base(position, side, persistent, false, id, "default") {
            baseData = new DynamicData(typeof(DashSwitch), this);

            Side = side;
            if (side == Sides.Up || side == Sides.Down) {
                Collider.Width = width;
            } else {
                Collider.Height = width;
            }

            if (attachToSolid) {
                Add(new StaticMover {
                    SolidChecker = IsRiding,
                    OnMove = OnMove,
                    OnAttach = OnAttach,
                    OnShake = OnShake,
                });
            }
            if (actLikeTouchSwitch) {
                Add(Switch = new Switch(groundReset: false));
                Switch.OnStartFinished = () => {
                    // if these are set, in Awake() it'll start out already pressed
                    this.persistent = true;
                    SceneAs<Level>().Session.SetFlag(FlagName);
                };
            }

            sprite.Scale = new Vector2(1f, width / 16f);
            if (side == Sides.Up || side == Sides.Down) {
                spriteOffset = new Vector2((width - 16f) / 2, 0f);
            } else {
                spriteOffset = new Vector2(0f, (width - 16f) / 2);
            }
        }

        public ResizableDashSwitch(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, SwitchSide(data.Enum("orientation", Sides.Up)),
                  data.Bool("persistent"), id, GetWidth(data), data.Bool("actLikeTouchSwitch", true),
                  data.Bool("attachToSolid", true)) { }

        // again copied from FlagDashSwitch :)
        private static Sides SwitchSide(Sides side) => side switch {
            Sides.Up => Sides.Down,
            Sides.Down => Sides.Up,
            Sides.Left => Sides.Right,
            Sides.Right => Sides.Left,
            _ => throw new Exception("Unknown ResizableDashSwitch direction!")
        };

        private static int GetWidth(EntityData data) {
            Sides side = SwitchSide(data.Enum("orientation", Sides.Left));
            return side switch {
                Sides.Up => data.Width,
                Sides.Down => data.Width,
                Sides.Left => data.Height,
                Sides.Right => data.Height,
                _ => throw new Exception("Unknown ResizableDashSwitch direction!")
            };
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (pressed) {
                Switch?.Activate();
            }
        }

        public override void Render() {
            // this looks awful but it's literally what the vanilla game does for spikes
            Vector2 oldPos = Position;
            Position += spriteOffset;
            base.Render();
            Position = oldPos;
        }

        private bool IsRiding(Solid solid) {
            Vector2 point = Side switch {
                Sides.Up => TopCenter - Vector2.UnitY,
                Sides.Down => BottomCenter + Vector2.UnitY,
                Sides.Left => CenterLeft - Vector2.UnitX,
                Sides.Right => CenterRight + Vector2.UnitX,
                _ => Vector2.Zero
            };
            return CollideCheck(solid, point);
        }

        private void OnMove(Vector2 amount) {
            // if currently solid, move player and stuff along
            if (Collidable) {
                MoveH(amount.X);
                MoveV(amount.Y);
            } else { // otherwise, just move without doing that
                Position += amount;
            }
            pressedTarget += amount;
            startY += amount.Y;
        }

        private void OnAttach(Platform platform) {
            Depth = platform.Depth + 1;
        }

        private new void OnShake(Vector2 amount) {
            spriteOffset += amount;
        }

        public static void Load() {
            On.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
        }

        public static void Unload() {
            On.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
        }

        private static DashCollisionResults DashSwitch_OnDashed(
            On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction) {
            if (self is ResizableDashSwitch dashSwitch) {
                bool pressed = dashSwitch.baseData.Get<bool>("pressed");
                Vector2 pressDirection = dashSwitch.baseData.Get<Vector2>("pressDirection");
                if (!pressed && direction == pressDirection)
                    player.RefillDash();
                if (dashSwitch.Switch?.Activate() == true)
                    SoundEmitter.Play(SFX.game_gen_touchswitch_last_oneshot);
            }
            return orig(self, player, direction);
        }
    }
}
