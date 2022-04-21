using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/MaskedDecal")]
    public class MaskedDecal : Entity {
        private static BlendState DestinationAlphaBlend = new BlendState {
            ColorSourceBlend = Blend.DestinationAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
        };

        public Decal Decal;

        public MaskedDecal(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Decal = new Decal(data.Attr("texture"), Position, new Vector2(data.Float("scaleX", 1f), data.Float("scaleY", 1f)), Depths.FGDecals);
            Decal.Visible = false;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(Decal);
        }


        public static void Load() {
            IL.Celeste.Level.Render += Level_Render;
        }

        public static void Unload() {
            IL.Celeste.Level.Render -= Level_Render;
        }

        private static void Level_Render(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdfld<Level>("GameplayRenderer"),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchCallvirt<Renderer>("Render"))) {

                Logger.Log("StrawberryJam2021/MaskedDecal", "Added Level.Render IL hook");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(level => {

                    List<Entity> maskedDecals = level.Tracker.GetEntities<MaskedDecal>();
                    if (maskedDecals.Count > 0) {
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, DestinationAlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
                        foreach (MaskedDecal entity in maskedDecals) {
                            entity.Decal?.Render();
                        }
                        Draw.SpriteBatch.End();
                    }

                });
            }
        }
    }
}
