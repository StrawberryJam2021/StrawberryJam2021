using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity(
        "SJ2021/DashThroughSpikesUp = LoadUp",
        "SJ2021/DashThroughSpikesDown = LoadDown",
        "SJ2021/DashThroughSpikesLeft = LoadLeft",
        "SJ2021/DashThroughSpikesRight = LoadRight"
        )]
    public class DashThroughSpikes : Entity {

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Up;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir);
        }
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Down;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir);
        }
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Left;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir);
        }
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Right;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir);
        }

        public Spikes.Directions Direction;
        private Vector2 DirectionVector;
        private Vector2 imageOffset;
        private int size;
        private float lastDashTime;
        private Vector2 dashDir;

        public DashThroughSpikes(Vector2 position, int size, Spikes.Directions direction):
            base(position) {

            Depth = -1;
            Direction = direction;
            DirectionVector = SpikeDirToVector(direction);
            this.size = size;
            switch (direction) {
                case Spikes.Directions.Up:
                    base.Collider = new Hitbox((float) size, 3f, 0f, -3f);
                    base.Add(new LedgeBlocker(null));
                    break;
                case Spikes.Directions.Down:
                    base.Collider = new Hitbox((float) size, 3f, 0f, 0f);
                    break;
                case Spikes.Directions.Left:
                    base.Collider = new Hitbox(3f, (float) size, -3f, 0f);
                    base.Add(new LedgeBlocker(null));
                    break;
                case Spikes.Directions.Right:
                    base.Collider = new Hitbox(3f, (float) size, 0f, 0f);
                    base.Add(new LedgeBlocker(null));
                    break;
            }

            Add(new DashListener(new Action<Vector2>(OnDash)));

            Add(new PlayerCollider(new Action<Player>(OnCollide)));

            Add(new StaticMover {
                OnShake = new Action<Vector2>(OnShake),
                SolidChecker = new Func<Solid, bool>(IsRiding),
                JumpThruChecker = new Func<JumpThru, bool>(IsRiding),
            });
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            AreaData areaData = AreaData.Get(scene);

            string dir = Direction.ToString().ToLower();

            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("danger/spikes/default_" + dir);
            for (int i = 0; i < size / 8; i++) {
                Image image = new Image(Calc.Random.Choose(atlasSubtextures));
            image.SetColor(Color.Black);
                switch (Direction) {
                    case Spikes.Directions.Up:
                        image.JustifyOrigin(0.5f, 1f);
                        image.Position = Vector2.UnitX * ((float) i + 0.5f) * 8f + Vector2.UnitY;
                        break;
                    case Spikes.Directions.Down:
                        image.JustifyOrigin(0.5f, 0f);
                        image.Position = Vector2.UnitX * ((float) i + 0.5f) * 8f - Vector2.UnitY;
                        break;
                    case Spikes.Directions.Left:
                        image.JustifyOrigin(1f, 0.5f);
                        image.Position = Vector2.UnitY * ((float) i + 0.5f) * 8f + Vector2.UnitX;
                        break;
                    case Spikes.Directions.Right:
                        image.JustifyOrigin(0f, 0.5f);
                        image.Position = Vector2.UnitY * ((float) i + 0.5f) * 8f - Vector2.UnitX;
                        break;
                }
                base.Add(image);
            }
       
        }

        private void OnShake(Vector2 amount) {
            imageOffset += amount;
        }


        public override void Update() {
            base.Update();
        }

        public override void Render() {
            Vector2 position = Position;
            Position += imageOffset;
            base.Render();
            Position = position;
        }

        private void OnCollide(Player player) {
            if (DashingIntoSpikes()) {
                return;
            }

            switch (Direction) {
                case Spikes.Directions.Up:
                    if (player.Speed.Y >= 0f && player.Bottom <= base.Bottom) {
                        player.Die(new Vector2(0f, -1f), false, true);
                        return;
                    }
                    break;
                case Spikes.Directions.Down:
                    if (player.Speed.Y <= 0f) {
                        player.Die(new Vector2(0f, 1f), false, true);
                        return;
                    }
                    break;
                case Spikes.Directions.Left:
                    if (player.Speed.X >= 0f) {
                        player.Die(new Vector2(-1f, 0f), false, true);
                        return;
                    }
                    break;
                case Spikes.Directions.Right:
                    if (player.Speed.X <= 0f) {
                        player.Die(new Vector2(1f, 0f), false, true);
                    }
                    break;
                default:
                    return;
            }
        }

        private Vector2 SpikeDirToVector(Spikes.Directions dir) {

            return dir switch {
                Spikes.Directions.Up => -Vector2.UnitY,
                Spikes.Directions.Down => Vector2.UnitY,
                Spikes.Directions.Right => Vector2.UnitX,
                _ => -Vector2.UnitX, //left or default case

            };
        }

        const float dashTimeThreshold = 0.3f; //length of time until player is considered not dashing
        //returns bool based on if the player is still dashing, and their direction is toward the spikes (diagonals count)
        private bool DashingIntoSpikes() {
            return base.Scene.TimeActive - lastDashTime < dashTimeThreshold
                && (Math.Sign(dashDir.X) == -Math.Sign(DirectionVector.X) 
                || Math.Sign(dashDir.Y) == -Math.Sign(DirectionVector.Y));
        }

        //Updates timestamp each time player dashes
        private void OnDash(Vector2 dir) {
            lastDashTime = base.Scene.TimeActive;
            dashDir = dir;
        }

        private static int GetSize(EntityData data, Spikes.Directions dir) {
            if (dir > Spikes.Directions.Down) {
                return data.Height;
            }
            return data.Width;
        }


        private bool IsRiding(Solid solid) {
            switch (Direction) {
                case Spikes.Directions.Up:
                    return base.CollideCheckOutside(solid, Position + Vector2.UnitY);
                case Spikes.Directions.Down:
                    return base.CollideCheckOutside(solid, Position - Vector2.UnitY);
                case Spikes.Directions.Left:
                    return base.CollideCheckOutside(solid, Position + Vector2.UnitX);
                case Spikes.Directions.Right:
                    return base.CollideCheckOutside(solid, Position - Vector2.UnitX);
                default:
                    return false;
            }
        }


        private bool IsRiding(JumpThru jumpThru) {
            Spikes.Directions direction = Direction;
            return direction == Spikes.Directions.Up && base.CollideCheck(jumpThru, Position + Vector2.UnitY);
        }
    }
}