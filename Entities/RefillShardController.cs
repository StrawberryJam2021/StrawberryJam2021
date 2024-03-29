﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/RefillShard")]
    public class RefillShardController : Entity {
        public const float RespawnTime = 3600f;

        public List<RefillShard> Shards;
        public Refill Refill;

        private bool spawnRefill;
        private bool twoDashes;
        private bool resetOnGround;
        private bool oneUse;
        private int collectAmount;
        private Vector2[] nodes;

        private bool finished;

        public RefillShardController(EntityData data, Vector2 offset) 
            : base(data.Position + offset) {
            spawnRefill = data.Bool("spawnRefill");
            twoDashes = data.Bool("twoDashes");
            resetOnGround = data.Bool("resetOnGround");
            oneUse = data.Bool("oneUse");
            collectAmount = data.Int("collectAmount");

            // old flcc behavior
            if (!spawnRefill && !data.Has("collectAmount"))
                oneUse = true;

            nodes = spawnRefill ? data.NodesOffset(offset) : data.NodesWithPosition(offset);
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            Shards = new List<RefillShard>();

            for (int i = 0; i < nodes.Length; i++) {
                var shard = new RefillShard(this, nodes[i], i, twoDashes, resetOnGround);
                Shards.Add(shard);
                scene.Add(shard);
            }

            if (spawnRefill) {
                Refill = new Refill(Position, twoDashes, oneUse);
                scene.Add(Refill);

                Refill.Collidable = false;
                Refill.Depth = Depths.BGDecals - 1;
                Refill.sprite.Visible = Refill.flash.Visible = Refill.outline.Visible = false;
                Refill.respawnTimer = RespawnTime;
            }
        }

        public override void Update() {
            base.Update();
            if (!finished && spawnRefill)
                Refill.respawnTimer = RespawnTime;
        }

        public void CheckCollection() {
            int collectedShards = Shards.Count(shard => shard.Follower.HasLeader);
            if (!finished && collectedShards >= (collectAmount > 0 ? collectAmount : Shards.Count)) {
                if (spawnRefill || (oneUse && collectedShards == Shards.Count))
                    finished = true;

                if (!spawnRefill) {
                    List<RefillShard> toRemove = new List<RefillShard>();
                    foreach (RefillShard shard in Shards) {
                        if (shard.Follower.HasLeader) {
                            shard.Collect(!oneUse);
                            if (oneUse)
                                toRemove.Add(shard);
                        }
                    }
                    foreach (RefillShard shard in toRemove)
                        Shards.Remove(shard);

                    Player player = Scene.Tracker.GetEntity<Player>();
                    Audio.Play(twoDashes ? SFX.game_10_pinkdiamond_touch : SFX.game_gen_diamond_touch, player.Position);
                    player.UseRefill(twoDashes);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    Celeste.Freeze(0.05f);
                    SceneAs<Level>().Shake();
                } else {
                    float maxDist = 0f;

                    foreach (RefillShard shard in Shards) {
                        shard.Finished = true;
                        if (shard.Follower.HasLeader)
                            shard.Follower.Leader.LoseFollower(shard.Follower);

                        Vector2 startPos = shard.Position;
                        Vector2 targetPos = Refill.Position;

                        float dist = (targetPos - startPos).Length();
                        maxDist = Math.Max(dist, maxDist);

                        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, dist / 200f, true);
                        tween.OnUpdate = (t) => shard.Position = Vector2.Lerp(startPos, targetPos, t.Eased);
                        shard.Add(tween);
                    }

                    Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () => {
                        Scene.Remove(Shards);
                        Shards.Clear();
                        SpawnRefill();
                    }, maxDist / 200f, true));
                }
            }
        }

        public void SpawnRefill() {
            Refill.respawnTimer = RespawnTime;
            Refill.Respawn();
        }
    }
}
