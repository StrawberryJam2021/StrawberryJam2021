using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/NodedCloud")]
    class NodedCloud : Cloud {
        private readonly Vector2[] nodes;
        private readonly DynData<Cloud> base_Entity;
        private readonly Vector2 RoomOffset;
        private Image outline;

        private int nextNode = 0;
        private float moveTime;

        private bool fragile { set => base_Entity.Set("fragile", value); }
        private float startY { set => base_Entity.Set("startY", value); }
        private bool returning { get => base_Entity.Get<bool>("returning"); }
        private Sprite sprite { get => base_Entity.Get<Sprite>("sprite"); }

        public NodedCloud(EntityData data, Vector2 offset) : base(data.Position + offset, true) {
            RoomOffset = offset;
            moveTime = data.Float("moveTime", 0.5f);
            nodes = data.Nodes;

            base_Entity = new DynData<Cloud>(this);
            Add(outline = new Image(GFX.Game["objects/StrawberryJam2021/nodedCloud/outline"]));
            outline.CenterOrigin();
            Add(new Coroutine(moveRoutine()));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            fragile = false;
        }

        public override void Render() {
            if (nextNode  < nodes.Length) {
                outline.RenderPosition = nodes[nextNode] + RoomOffset + sprite.Position;
            } else {
                outline.Visible = false;
            }
            base.Render();
        }

        private IEnumerator moveRoutine() {
            while (nextNode + 1 <= nodes.Length) {
                while (!returning) {
                    yield return null;
                }

                float progress = 0f;
                Vector2 oldPos = Position;
                startY = (nodes[nextNode] + RoomOffset).Y;

                while (progress < moveTime) {
                    MoveTo(Vector2.Lerp(oldPos, nodes[nextNode] + RoomOffset, Ease.CubeIn(progress * (1 / moveTime))));
                    progress += Engine.DeltaTime;
                    yield return null;
                }
                Position = nodes[nextNode] + RoomOffset;
                if (++nextNode == nodes.Length) {
                    fragile = true;
                }

                while (returning) {
                    yield return null;
                }
            }
            while (Collidable) {
                yield return null;
            }
            yield return 1f;
            RemoveSelf();
        }
    }
}
