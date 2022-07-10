using System;
using Celeste;
using Celeste.Mod.Entities;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using MonoMod.Utils;
using MonoMod.Cil;
using Celeste.Mod;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked(false)]
    [CustomEntity("SJ2021/HorizontalTempleGate")]
    public class HorizontalTempleGate : Solid {
        public enum Types {
            NearestSwitch,
            TouchSwitches,
            FlagActive
        }
        //for context, this is implemented to allow the gate to open to the right, to the left or from the center of the gate's position. The center direction applies both the left and right door's hitboxes and sprites, modified to halve their extended distance from their respective side.
        public enum OpenDirections {
            Left,
            Right,
            Center
        }
        private static readonly FieldInfo GetPrivateRiders = typeof(Solid).GetField("riders", BindingFlags.NonPublic | BindingFlags.Static);
        private static HashSet<Actor> riders => (HashSet<Actor>)GetPrivateRiders.GetValue(null);

        private string LevelID;

        // 0 - Left; 1 - Right
        private Hitbox[] colliders;
        private Sprite[] sprites;

        private bool Inverted;
        private string Flag;
        public OpenDirections OpenDirection;
        public Types Type;

        // maximum distance between foot of door and edge
        private const float MinEdgeSpace = 4f;

        // how far the foot of the door is from the wall
        private readonly float OpenWidth;
        // distance between foot of door and wall
        private float drawWidth;
        // move drawWidth towards targetWidth;
        private float targetWidth;
        // how fast the door should be moving if it is moving
        private float widthMoveSpeed;
        // either 0.5 or 1. 0.5 to compensate for two doors moving in Center case
        private readonly float moveSpeedMultiplier;

        public bool ClaimedByASwitch;
        private bool open;
        private bool lockState;

        private Shaker shaker;


        //The full section between this comment and the next was written by lilybeevee
        public static void Load() {
            IL.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
        }

        private static void DashSwitch_OnDashed(ILContext il) {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(instr => instr.MatchLdfld<DashSwitch>("allGates"))) {
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out _))) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Action<DashSwitch>>(self => {
                        var data = new DynData<DashSwitch>(self);
                        foreach (HorizontalTempleGate entity in self.Scene.Tracker.GetEntities<HorizontalTempleGate>()) {
                            if (entity.Type == HorizontalTempleGate.Types.NearestSwitch && entity.LevelID == data.Get<EntityID>("id").Level) {
                                entity.SwitchOpen();
                            }
                        }
                    });
                }
            }
            cursor.Index = 0;

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<DashSwitch>("GetGate"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<TempleGate, DashSwitch, TempleGate>>((templeGate, self) => {
                    var data = new DynData<DashSwitch>(self);
                    var entities = self.Scene.Tracker.GetEntities<HorizontalTempleGate>();
                    HorizontalTempleGate hTempleGate = null;
                    float dist = 0f;
                    foreach (HorizontalTempleGate item in entities) {
                        if (item.Type == HorizontalTempleGate.Types.NearestSwitch && !item.ClaimedByASwitch && item.LevelID == data.Get<EntityID>("id").Level) {
                            float currentDist = Vector2.DistanceSquared(self.Position, item.Position);
                            if (hTempleGate == null || currentDist < dist) {
                                hTempleGate = item;
                                dist = currentDist;
                            }
                        }
                    }
                    if (hTempleGate != null && (templeGate == null || dist < Vector2.DistanceSquared(self.Position, templeGate.Position))) {
                        if (templeGate != null) {
                            templeGate.ClaimedByASwitch = false;
                            templeGate = null;
                        }
                        hTempleGate.ClaimedByASwitch = true;
                        hTempleGate.SwitchOpen();
                    }
                    return templeGate;
                });
            }
        }
        public static void Unload() {
            IL.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
        }
        //comment to indicate end of lilybeevee's IL hook

        public HorizontalTempleGate(EntityData data, Vector2 offset)
            : base(data.Position + offset, 48f, 8f, safe: true) {

            LevelID = data.Level.Name;

            Type = data.Enum<Types>("type", Types.FlagActive);
            OpenDirection = data.Enum<OpenDirections>("direction", OpenDirections.Left);
            OpenWidth = data.Int("openWidth", 0);
            Inverted = data.Bool("inverted", false);
            Flag = data.Attr("flag", "");

            Depth = Depths.Solids;
            Add(shaker = new Shaker(on: false));
            Collider = new ColliderList();

            moveSpeedMultiplier = 1f;
            // foot of gate when closed
            float targetX = 48f;
            switch (OpenDirection) {
                case OpenDirections.Left:
                    targetX = 48f;
                    break;
                case OpenDirections.Right:
                    targetX = 0f;
                    break;
                case OpenDirections.Center:
                    targetX = 24f;
                    moveSpeedMultiplier = 0.5f;
                    break;
            }

            this.colliders = new Hitbox[]{
                new Hitbox(targetX, 8f, 0, 0),
                new Hitbox(48f - targetX, 8f, targetX, 0)
            };
            if(OpenDirection != OpenDirections.Right) {
                ((ColliderList) Collider).Add(colliders[0]);
            }
            if (OpenDirection != OpenDirections.Left) {
                ((ColliderList) Collider).Add(colliders[1]);
            }

            this.sprites = new Sprite[2];
            Add(sprites[0] = StrawberryJam2021Module.SpriteBank.Create("horizontalTempleGateLeft"));
            Add(sprites[1] = StrawberryJam2021Module.SpriteBank.Create("horizontalTempleGateRight"));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (Inverted) {
                // Init gate as open
                this.drawWidth = Math.Max(OpenWidth, MinEdgeSpace);
                SetWidth((int) OpenWidth);
                this.open = true;
            } else {
                // init gate as closed
                this.drawWidth = 48f * this.moveSpeedMultiplier;
                SetWidth((int) this.drawWidth);
                this.open = false;
            }

            if (Type == Types.TouchSwitches) {
                Add(new Coroutine(CheckTouchSwitches(), false));
            }
            else if (Type == Types.FlagActive) {
                Add(new Coroutine(CheckFlag(this.Flag), false));
            }
        }

        //applies a delay to utilization of Open in the case the door is opened by a dash switch. I don't know why this was implemented, but for consistency with the vanilla temple gate this is being used.
        public void SwitchOpen(){
            foreach(Sprite s in sprites) {
                s.Play("open");
            }
            if (Inverted) {
                Alarm.Set(this, 0.2f, delegate {
                    shaker.ShakeFor(0.2f, removeOnFinish: false);
                    Alarm.Set(this, 0.2f, Close);
                });
            } else {
                Alarm.Set(this, 0.2f, delegate {
                    shaker.ShakeFor(0.2f, removeOnFinish: false);
                    Alarm.Set(this, 0.2f, Open);
                });
            }
        }

        public void Open() {
            Audio.Play("event:/game/05_mirror_temple/gate_main_open", Position);
            widthMoveSpeed = 200f;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetWidth(0);
            foreach(Sprite s in sprites) {
                s.Play("open");
            }
            this.open = true;
        }

        public void Close() {
            Audio.Play("event:/game/05_mirror_temple/gate_main_close", Position);
            widthMoveSpeed = 300f;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetWidth((int) (48f * moveSpeedMultiplier));

            foreach (Sprite s in sprites) {
                s.Play("hit");
            }

            this.open = false;
        }

        private IEnumerator PerformChange(bool shake = true) {
            if (shake) {
                yield return 0.5f;
                shaker.ShakeFor(0.2f, removeOnFinish: false);
                yield return 0.2f;
            }
            while (lockState) {
                yield return null;
            }

            if (Inverted) {
                Close();
            } else {
                Open();
            }
        }

        private IEnumerator CheckTouchSwitches() {
            while (!Switch.Check(Scene)) {
                yield return null;
            }
            yield return PerformChange();
        }

        private IEnumerator CheckFlag(string flag) {
            Level level = Scene as Level;
            while ((this.open == level.Session.GetFlag(flag)) != this.Inverted) {
                yield return null;
            }
            yield return PerformChange();
        }

        private void SetWidth(int width) {
            this.targetWidth = width;
            float x = X;
            float oldWidth = 0f;
            if (OpenDirection != OpenDirections.Right) {
                oldWidth = colliders[0].Width;
                colliders[0].Width = width;
                // if we're growing, try to push/kill the player
                // there's a separate check if we're in center mode
                if (oldWidth < width && OpenDirection != OpenDirections.Center) {
                    X -= width - oldWidth;
                    MoveHExact((int) (width - oldWidth));
                }
                X = x;
            }
            if (OpenDirection != OpenDirections.Left) {
                oldWidth = colliders[1].Width;
                colliders[1].Width = width;
                colliders[1].Position.X = 48 - width;
                // attempt to push/kill player
                if (oldWidth < width && OpenDirection != OpenDirections.Center) {
                    X -= oldWidth - width;
                    MoveHExact((int) (oldWidth - width));
                }
                X = x;
            }
            if(OpenDirection == OpenDirections.Center && oldWidth < width) {
                MoveRiders((int) (width - oldWidth));
            }
            Collidable = true;
        }

        public void MoveRiders(int width) {
            // this function is based on Solid.MoveHExact

            // Move/Kill riders when we're centered
            GetRiders();
            float right = Right;
            float left = Left;
            Player player = null;
            player = Scene.Tracker.GetEntity<Player>();

            // Drop the player down if they are at a particular height
            // the player gets stuck in the door without this
            if (player != null && CollideCheck(player, Position) &&
                    (player.Top - this.Bottom) <= -6 && (player.Top - this.Bottom) > -12) {
                player.NaiveMove((this.Bottom-player.Top) * Vector2.UnitY);
            }

            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>()) {
                if (entity.AllowPushing) {
                    bool collidable = entity.Collidable;
                    entity.Collidable = true;
                    int move = (entity.CenterX > (left + right) / 2) ? -width : width;
                    if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                        // yeah i kinda gave up here and just decided to hard center the entity
                        entity.CenterX = this.CenterX - 1f;
                        entity.MoveHExact(1, entity.SquishCallback, this);
                    } else if (riders.Contains(entity)) {
                        entity.CenterX = this.CenterX - 1f;
                        if (entity.TreatNaive) {
                            entity.NaiveMove(Vector2.UnitX);
                        } else {
                            entity.MoveHExact(1, null, null);
                        }
                    }
                    entity.Collidable = collidable;
                }
            }
            riders.Clear();
        }

        public override void Update() {
            base.Update();
            float num = Math.Max(targetWidth, MinEdgeSpace);
            if (drawWidth != num) {
                lockState = true;
                drawWidth = Calc.Approach(drawWidth, num, 
                    moveSpeedMultiplier * widthMoveSpeed * Engine.DeltaTime);
            } else {
                lockState = false;
            }
        }

        public override void Render() {
            if (OpenDirection != OpenDirections.Right) {
                Vector2 shake = new Vector2(0f, Math.Sign(shaker.Value.X));
                sprites[0].DrawSubrect(new Vector2(0, -2) + shake, 
                    new Rectangle((int) (sprites[0].Width - drawWidth), 0,
                        (int) drawWidth, (int) sprites[0].Height));
            }
            if (OpenDirection != OpenDirections.Left) {
                Vector2 shake = new Vector2(0f, Math.Sign(shaker.Value.Y));
                sprites[1].DrawSubrect(new Vector2(48f - drawWidth, -3) + shake, 
                    new Rectangle(0, 0, (int) drawWidth, (int) sprites[1].Height));
            }
        }
    }

}