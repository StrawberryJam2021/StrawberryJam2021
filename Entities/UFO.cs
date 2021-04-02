using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/UFO")]
    public class UFO : FlingBird {
        EntityData entityData;
        Sprite sprite;
        public UFO(Vector2[] nodes, bool skippable) : base(nodes, skippable) {
            Add(new PlayerCollider(OnPlayer, new Hitbox(24f, 12f, 0f, -20f)));
        }
        public UFO(EntityData data, Vector2 levelOffset) : this(data.NodesWithPosition(levelOffset), data.Bool("waiting")) {
            entityData = data;
        }

        public override void Awake(Scene scene) {
            List<UFO> list = base.Scene.Entities.FindAll<UFO>();
            for (int num = list.Count - 1; num >= 0; num--) {
                if (list[num].entityData.Level.Name != entityData.Level.Name) {
                    list.RemoveAt(num);
                }
            }
            list.Sort((UFO a, UFO b) => Math.Sign(a.X - b.X));
            if (list[0] == this) {
                for (int i = 1; i < list.Count; i++) {
                    NodeSegments.Add(list[i].NodeSegments[0]);
                    SegmentsWaiting.Add(list[i].SegmentsWaiting[0]);
                    list[i].RemoveSelf();
                }
            }
            if (SegmentsWaiting[0]) {
                sprite.Play("hoverStressed");
                sprite.Scale.X = 1f;
            }
            Player entity = scene.Tracker.GetEntity<Player>();
            if (entity != null && entity.X > base.X) {
                RemoveSelf();
            }
        }
        private void OnPlayer(Player player) {
            if (player.Position.Y < Position.Y) {
                player.Bounce(base.Top);
            }

        }
    }

}
