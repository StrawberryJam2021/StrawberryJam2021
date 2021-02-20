﻿using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class RefillShard : Entity {
        public bool Finished;
        public Follower Follower;

        private int index;
        private bool twoDashes;
        private bool resetOnGround;
        private RefillShardController controller;

        private Vector2 start;
        private Platform attached;
        private float canLoseTimer;
        private float loseTimer;
        private bool losing;

        private Sprite sprite;
        private Sprite flash;
        private ParticleType p_shatter;
        private ParticleType p_regen;
        private ParticleType p_glow;
        private Wiggler wiggler;
        private BloomPoint bloom;
        private VertexLight light;
        private SineWave sine;

        public RefillShard(RefillShardController controller, Vector2 position, int index, bool two, bool groundReset) : base(position) {
            this.index = index;
            this.controller = controller;

            twoDashes = two;
            resetOnGround = groundReset;

            start = Position;

            Add(Follower = new Follower(OnGainLeader, OnLoseLeader));
            Follower.FollowDelay = 0.2f;
            Follower.PersistentFollow = false;
            Add(new PlayerCollider(OnPlayer));
            Add(new StaticMover {
                SolidChecker = solid => solid.CollideCheck(this),
                OnAttach = platform => {
                    Depth = Depths.Top;
                    Collider = new Hitbox(18f, 18f, -9f, -9f);
                    attached = platform;
                    start = Position - platform.Position;
                }
            });

            p_shatter = two ? Refill.P_ShatterTwo : Refill.P_Shatter;
            p_regen = two ? Refill.P_RegenTwo : Refill.P_Regen;
            p_glow = two ? Refill.P_GlowTwo : Refill.P_Glow;

            Add(sprite = new Sprite(GFX.Game, $"objects/StrawberryJam2021/refillShard/{(two ? "two" : "one")}"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle", false, false);
            sprite.CenterOrigin();

            Add(flash = new Sprite(GFX.Game, "objects/StrawberryJam2021/refillShard/flash"));
            flash.Add("flash", "", 0.05f);
            flash.OnFinish = (anim) => flash.Visible = false;
            flash.CenterOrigin();

            sprite.Rotation = flash.Rotation = Calc.Random.Next(4) * ((float) Math.PI / 2f);

            Add(wiggler = Wiggler.Create(1f, 4f, value => sprite.Scale = flash.Scale = Vector2.One * (1f + value * 0.2f)));

            Add(bloom = new BloomPoint(0.8f, 8f));
            Add(light = new VertexLight(Color.White, 1f, 8, 32));
            Add(sine = new SineWave(0.6f));
            sine.Randomize();

            UpdateY();
            Depth = Depths.Pickups;
            Collider = new Hitbox(12f, 12f, -6f, -6f);
        }

        public override void Update() {
            base.Update();

            if (!Finished && resetOnGround) {
                if (canLoseTimer > 0f) {
                    canLoseTimer -= Engine.DeltaTime;
                } else if (Follower.HasLeader && (Follower.Leader.Entity as Player).LoseShards) {
                    losing = true;
                }
                if (losing) {
                    var player = Follower.Leader.Entity as Player;
                    if (loseTimer <= 0f || player.Speed.Y < 0f) {
                        player.Leader.LoseFollower(Follower);
                        losing = false;
                    } else if (player.LoseShards) {
                        loseTimer -= Engine.DeltaTime;
                    } else {
                        loseTimer = 0.15f;
                        losing = false;
                    }
                }
            }

            if (!Finished && Visible && Scene.OnInterval(0.1f))
                SceneAs<Level>().ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 4f);

            if (Scene.OnInterval(2f) && Visible) {
                flash.Play("flash", true);
                flash.Visible = true;
            }

            light.Alpha = Calc.Approach(light.Alpha, Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
            UpdateY();
        }

        public override void Render() {
            if (sprite.Visible)
                sprite.DrawOutline();
            base.Render();
        }

        public void OnCollectCutscene() {
            Finished = true;
            Follower.Leader.LoseFollower(Follower);
            Depth = Depths.FormationSequences - 2;
            Tag = Tags.FrozenUpdate;
        }

        public void Collect(bool respawn) {
            Finished = true;
            Follower.Leader.LoseFollower(Follower);
            SceneAs<Level>().ParticlesFG.Emit(p_shatter, 8, Position, Vector2.One * 3f, Calc.Random.NextFloat((float) Math.PI * 2f));
            if (!respawn) {
                RemoveSelf();
            } else {
                Collidable = false;
                Visible = false;
                Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () => Respawn(), 3.6f + (index * 0.1f), true));
            }
        }

        private void UpdateY() {
            flash.Y = sprite.Y = bloom.Y = sine.Value * 2f;
        }

        private void OnGainLeader() {
            controller.CheckCollection();
            canLoseTimer = 0.25f;
            loseTimer = 0.15f;
        }

        private void OnLoseLeader() {
            if (!Finished) {
                Add(new Coroutine(ReturnRoutine()));
            }
        }

        private void OnPlayer(Player player) {
            Audio.Play("event:/game/general/seed_touch", Position, "count", index % 5);
            Collidable = false;
            Depth = Depths.Top;
            player.Leader.GainFollower(Follower);
        }

        private IEnumerator ReturnRoutine() {
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = flash.Scale = Vector2.One * 2f;
            yield return 0.05f;

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            for (int i = 0; i < 6; i++) {
                float dir = Calc.Random.NextFloat((float) Math.PI * 2f);
                SceneAs<Level>().ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
            }
            Visible = false;
            yield return 0.3f + (index * 0.1f);

            Respawn();
            yield break;
        }

        private void Respawn() {
            Position = start;
            if (attached != null)
                Position += attached.Position;
            FMOD.Studio.EventInstance sound = Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", Position);
            sound.setVolume(0.75f);
            sound.setPitch(2f);
            SceneAs<Level>().ParticlesFG.Emit(p_regen, 8, Position, Vector2.One * 2f);
            sprite.Scale = flash.Scale = Vector2.One;
            wiggler.Start();
            Visible = true;
            Collidable = true;
            Finished = false;
        }
    }
}