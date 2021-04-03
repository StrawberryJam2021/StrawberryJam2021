﻿using System;
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
        Image sprite;
        private Vector2 scale;
        private SineWave idleSine;
        private Wiggler bounceWiggler;
        private Vector2 lastSinePosition;
        private Vector2 anchorPosition;
        public UFO(Vector2[] nodes, bool skippable) : base(nodes, skippable) {
            scale = Vector2.One;
            Add(new PlayerCollider(OnPlayer, new Hitbox(24f, 12f, -20f, -20f)));
            base.Collider = new Hitbox(24f, 48f, -20f, 4f);
            idleSine = new SineWave(0.5f, 0f);
            idleSine.Randomize();
            Add(idleSine);
            Remove(Get<Sprite>());
            Add(sprite = new Image(GFX.Game["objects/StrawberryJam2021/UFO/UFO"]));
            sprite.CenterOrigin();
            bounceWiggler = Wiggler.Create(0.6f, 2.5f, delegate (float v)
            {
                sprite.Rotation = v * 20f * ((float) Math.PI / 180f);
            });
            Add(bounceWiggler);
            anchorPosition = Position;
        }
        public UFO(EntityData data, Vector2 levelOffset) : this(data.NodesWithPosition(levelOffset), data.Bool("waiting")) {
            entityData = data;
        }

        public override void Update() {
            base.Update();
            if (Position != lastSinePosition) {
                anchorPosition += Position - lastSinePosition;
            }
            lastSinePosition = Position;

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
            Player entity = scene.Tracker.GetEntity<Player>();
            if (entity != null && entity.X > base.X) {
                RemoveSelf();
            }
        }

        private void GotoHit(Vector2 from) {
            scale = new Vector2(1.2f, 0.8f);
            //hitSpeed = Vector2.UnitY * 200f;
            bounceWiggler.Start();
            //Alert(restart: true, playSfx: false);
            Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
        }

        private void OnPlayer(Player player) {
            if (player.Position.Y < Position.Y) {
                player.Bounce(base.Top);
                GotoHit(player.Center);
               //MoveToX(anchorPosition.X);
                idleSine.Reset();
                anchorPosition = (lastSinePosition = Position);
                //eyeSpin = 1f;
            }

        }
    }

}
