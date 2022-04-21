using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/NodedCloud")]
    public class NodedCloud : Cloud {
        private static ParticleType P_Spawn, P_Ghost;
        private static byte BaseGhostOpacity = 0x62; // (0.385 * 255)

        private readonly Vector2[] nodes;
        private readonly DynData<Cloud> base_Entity;
        private readonly Vector2 RoomOffset;
        private Image ghost;
        private float GhostAlphaOffset { get => 1 + 0.2f * (float) Math.Sin(timer * 4); }

        private int nextNode;
        private float moveTime;
        private float fadeInProgress;

        private float timer { get => base_Entity.Get<float>("timer"); }
        private float startY { set => base_Entity.Set("startY", value); }
        private float respawnTimer { set => base_Entity.Set<float>("respawnTimer", value); get => base_Entity.Get<float>("respawnTimer"); }

        public NodedCloud(EntityData data, Vector2 offset) : base(data.Position + offset, true) {
            RoomOffset = offset;
            moveTime = data.Float("moveTime", 0.5f);
            nodes = data.Nodes;
            fadeInProgress = 1;

            base_Entity = new DynData<Cloud>(this);
            Add(ghost = new Image(GFX.Game["objects/clouds/fragile00"]));
            ghost.CenterOrigin();
            ghost.Color = Color.Black;
            ghost.Color.A = BaseGhostOpacity;
            Add(new Coroutine(moveRoutine()));
        }

        public static void LoadParticles() {
            P_Spawn = new ParticleType(P_FragileCloud) {
                Color = Calc.HexToColor("ff9ae0"),
            };
            P_Ghost = new ParticleType(P_FragileCloud) {
                Color = Calc.HexToColor("00000062"),
            };
        }

        public override void Render() {
            if (nextNode  < nodes.Length) {
                ghost.RenderPosition = nodes[nextNode] + RoomOffset;
                ghost.Color.A = (byte) (fadeInProgress * BaseGhostOpacity * GhostAlphaOffset);
            } else {
                ghost.Visible = false;
            }
            base.Render();
        }

        private IEnumerator moveRoutine() {
            while (nextNode + 1 <= nodes.Length) {
                while (Collidable) {
                    yield return null;
                }
                // let the fade animation play in the original position.
                // this effectively puts a speed limit on how fast the cloud can move between positions
                yield return 0.4f;

                startY = (nodes[nextNode] + RoomOffset).Y;

                respawnTimer = Math.Max(moveTime - 0.4f, 0f);
                Position = nodes[nextNode] + RoomOffset;
                fadeInProgress = 0;
                Add(new Coroutine(ghostFadeInRoutine(), true));

                nextNode++;
                while (!Collidable) {
                    if (Engine.Scene.OnInterval(0.05f)) {
                        if (nextNode <= nodes.Length) {
                            SceneAs<Level>().ParticlesBG.Emit(P_Spawn, 5, nodes[nextNode - 1] + RoomOffset, new Vector2(Collider.Width / 2f, 5f), (float) Math.PI / 2);
                        }
                    }
                    yield return null;
                }
            }
            while (Collidable) {
                yield return null;
            }
            yield return 1f;
            RemoveSelf();
        }

        private IEnumerator ghostFadeInRoutine() {
            yield return moveTime / 2;

            while (fadeInProgress < 1) {
                fadeInProgress += Engine.DeltaTime * 2f * moveTime;
                if (nextNode + 1 <= nodes.Length) {
                    if (Engine.Scene.OnInterval(0.05f)) {
                        SceneAs<Level>().ParticlesBG.Emit(P_Ghost, 5, nodes[nextNode] + RoomOffset, new Vector2(Collider.Width / 2f, 5f), (float) Math.PI / 2);
                    }
                }
                yield return null;
            }
            fadeInProgress = 1;
        }
    }
}
