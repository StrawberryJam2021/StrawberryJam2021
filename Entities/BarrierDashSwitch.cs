using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/BarrierDashSwitch")]
    public class BarrierDashSwitch : DashSwitch {

        public BarrierDashSwitch(Vector2 position, Sides side, bool persistent, EntityID id, string spritePath)
            : base(position, side, persistent, false, id, "default") {
            if (!string.IsNullOrEmpty(spritePath)) {
                DynamicData baseData = new DynamicData(typeof(DashSwitch), this);
                Sprite sprite = baseData.Get<Sprite>("sprite");
                Remove(sprite);
                //sprites.xml cringe
                sprite = new Sprite(GFX.Game, $"{spritePath}/dashButton");
                sprite.CenterOrigin();
                sprite.Justify = new Vector2(0.5f, 0.5f);
                int[] idleFrames = new int[21];
                for (int i = 0; i <= 20; i++) {
                    idleFrames[i] = i;
                }
                sprite.AddLoop("idle", "", 0.08f, idleFrames);
                sprite.AddLoop("pushed", "", 0.08f, 27);
                sprite.Add("push", "", 0.08f, "pushed", 21, 22, 23, 24, 25, 26, 27);
                switch (side) {
                    case Sides.Right:
                        sprite.Position = new Vector2(8f, 8f);
                        sprite.Rotation = 0f;
                        break;
                    case Sides.Left:
                        sprite.Position = new Vector2(0f, 8f);
                        sprite.Rotation = (float) Math.PI;
                        break;
                }
                Add(sprite);
                sprite.Play("idle");
                baseData.Set("sprite", sprite);
            }
        }

        public BarrierDashSwitch(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, SwitchSide(data.Enum("orientation", Sides.Left)), 
                  data.Bool("persistent"), id, data.Attr("spritePath", "")) { }

        // bless FlagDashSwitch for already existing so i didn't have to figure this out on my own
        private static Sides SwitchSide(Sides side) => side switch {
            Sides.Left => Sides.Right,
            Sides.Right => Sides.Left,
            _ => Sides.Right
        };

        public static void Load() {
            IL.Celeste.Glider.OnCollideH += IL_Glider_OnCollideH;
        }

        public static void Unload() {
            IL.Celeste.Glider.OnCollideH -= IL_Glider_OnCollideH;
        }

        private static void IL_Glider_OnCollideH(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, 
                instr => instr.MatchCallOrCallvirt<DashCollision>("Invoke"),
                instr => instr.MatchPop())) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit<CollisionData>(OpCodes.Ldfld, "Hit");
                cursor.EmitDelegate<Action<Glider, Platform>>(DestroyIfBarrierDashSwitch);
            }
        }

        private static void DestroyIfBarrierDashSwitch(Glider glider, Platform hit) {
            if (hit is BarrierDashSwitch) {
                DynamicData gliderData = new DynamicData(glider);
                gliderData.Set("destroyed", true);
                glider.Collidable = false;
                glider.Add(new Coroutine(gliderData.Invoke<IEnumerator>("DestroyAnimationRoutine")));
            }
        }
    }
}
