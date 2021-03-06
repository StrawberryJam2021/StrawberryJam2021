﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/LoopBlock")]
    class LoopBlock : Solid {

        private Vector2 start;
        private Vector2 speed;

        private Vector2 scale = Vector2.One;

        private bool waiting = true;
        private bool canRumble, canSquish;
        private bool returning, returningDash;
        private bool dashed;

        private float respawnTimer;
        private float targetSpeedX;
        private float dashedDirX;

        private int edgeThickness;

        public LoopBlock(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height, data.Int("edgeThickness", 10)) { }

        public LoopBlock(Vector2 position, int width, int height, int edgeThickness)
            : base(position, width, height, false) {
            Depth = Depths.Dust;
            SurfaceSoundIndex = SurfaceIndex.Snow;

            start = position;

            int minEdgeSize = Math.Min(width, height) / 8;
            this.edgeThickness = Calc.Clamp(edgeThickness, 1, (int)((minEdgeSize - 1) / 2f));

            OnDashCollide = OnDashed;

            SetupTiles();
        }

        private void SetupTiles() {
            int w = (int) (Width / 8f);
            int h = (int) (Height / 8f);

            VirtualMap<bool> tileMap = new VirtualMap<bool>(w, h);
            for (int i = 0; i < w; i++) {
                for (int j = 0; j < h; j++) {
                    tileMap[i, j] = (i < edgeThickness || i >= w - edgeThickness || j < edgeThickness || j >= h - edgeThickness);
                }
            }
        }

        private DashCollisionResults OnDashed(Player player, Vector2 dir) {
            if (dir.Y == 0 && !dashed) {
                int dashes = player.Dashes;
                float stamina = player.Stamina;
                player.ExplodeLaunch(new Vector2(Center.X, player.Center.Y), false, false);

                player.Dashes = dashes;
                player.Stamina = stamina;

                if (speed.Y < 0) {
                    player.Speed.Y += Math.Max(speed.Y, -80);
                }
                speed.X = dir.X * 180f;
                targetSpeedX = -dir.X * 90f;
                dashedDirX = dir.X;

                dashed = canSquish = true;
            }
            return DashCollisionResults.NormalCollision;
        }

        public override void Update() {
            base.Update();

            scale = Calc.Approach(scale, Vector2.One, 3f * Engine.DeltaTime);
            foreach (StaticMover staticMover in staticMovers) {
                if (staticMover.Entity is Spikes spikes) {
                    spikes.SetOrigins(Center);
                    foreach (Component component in spikes.Components) {
                        if (component is Image image)
                            image.Scale = scale;
                    }
                }
            }

            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    waiting = true;
                    base.Y = start.Y;
                    speed.Y = 0f;
                    Collidable = true;
                }
                return;
            }

            if (dashed) {
                if (returningDash) {
                    speed.X = Calc.Approach(speed.X, -targetSpeedX * 0.75f, 600f * Engine.DeltaTime);
                    MoveH(speed.X * Engine.DeltaTime);
                    if ((dashedDirX < 0 && X <= start.X) || (dashedDirX > 0 && X >= start.X)) {
                        returningDash = dashed = false;
                        MoveToX(start.X);
                        speed.X = 0f;
                    }
                } else {
                    speed.X = Calc.Approach(speed.X, targetSpeedX, 1200f * Engine.DeltaTime);
                    MoveH(speed.X * Engine.DeltaTime);
                    if (speed.X == targetSpeedX && ((dashedDirX < 0 && X > start.X) || (dashedDirX > 0 && X < start.X)))
                        returningDash = true;
                }
                if (canSquish) {
                    canSquish = false;
                    scale = new Vector2(1f - Math.Abs(dashedDirX) * 0.4f, 1f + Math.Abs(dashedDirX) * 0.4f);
                }
            }

            if (waiting) {
                Player playerRider = GetPlayerRider();
                if (playerRider != null && playerRider.Speed.Y >= 0f) {
                    canRumble = true;
                    speed.Y = 180f;
                    waiting = false;
                    Audio.Play("event:/game/04_cliffside/cloud_blue_boost", Center);
                }
                return;
            }

            if (returning) {
                speed.Y = Calc.Approach(speed.Y, 180f, 600f * Engine.DeltaTime);
                MoveTowardsY(start.Y, speed.Y * Engine.DeltaTime);
                if (base.ExactPosition.Y == start.Y) {
                    returning = false;
                    waiting = true;
                    speed.Y = 0f;
                }
                return;
            }

            if (speed.Y < 0f && canRumble) {
                canRumble = false;
                if (HasPlayerRider()) {
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }
            }

            if (speed.Y < 0f && base.Scene.OnInterval(0.02f)) {
                (base.Scene as Level).ParticlesBG.Emit(Cloud.P_Cloud, 1, Position + new Vector2(Width / 2, Height), new Vector2(base.Collider.Width / 2f, 1f), (float) Math.PI / 2f);
            }

            if (base.Y >= start.Y) {
                speed.Y -= 1200f * Engine.DeltaTime;
            } else {
                speed.Y += 1200f * Engine.DeltaTime;
                if (speed.Y >= -100f) {
                    Player playerRider2 = GetPlayerRider();
                    if (playerRider2 != null && playerRider2.Speed.Y >= 0f && !HasPlayerClimbing()) {
                        playerRider2.Speed.Y = -200f;
                    }
                    returning = true;
                }
            }
            float num = speed.Y;
            if (num < 0f) {
                num = -220f;
            }
            MoveV(speed.Y * Engine.DeltaTime, num);
        }

        public override void Render() {
            base.Render();

            float colorLerp = -Calc.Clamp(speed.Y, -80, 0) / 80;
            Color color = Color.Lerp(Color.MediumVioletRed, Color.Orange, colorLerp);
        }
    }
}
