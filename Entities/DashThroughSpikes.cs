using System;
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
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, data.Attr("spikeType", "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Down;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, data.Attr("spikeType", "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Left;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, data.Attr("spikeType", "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) {
            Spikes.Directions dir = Spikes.Directions.Right;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, data.Attr("spikeType", "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }

        public Spikes.Directions Direction;
        private Vector2 directionVector;
        private Vector2 imageOffset;
        private int size;
        private string spikeType;

        public DashThroughSpikes(Vector2 position, int size, Spikes.Directions direction, string spikeType) :
            base(position) {

            Depth = Depths.Player - 1;
            Direction = direction;
            directionVector = SpikeDirToVector(direction);
            this.size = size;
            this.spikeType = spikeType;
            switch (direction) {
                case Spikes.Directions.Up:
                    Collider = new Hitbox(size, 3f, 0f, -3f);
                    Add(new LedgeBlocker(null));
                    break;
                case Spikes.Directions.Down:
                    Collider = new Hitbox(size, 3f, 0f, 0f);
                    break;
                case Spikes.Directions.Left:
                    Collider = new Hitbox(3f, size, -3f, 0f);
                    Add(new LedgeBlocker(null));
                    break;
                case Spikes.Directions.Right:
                    Collider = new Hitbox(3f, size, 0f, 0f);
                    Add(new LedgeBlocker(null));
                    break;
            }

            Add(new PlayerCollider(new Action<Player>(OnCollide)));

            Add(new StaticMover {
                OnShake = new Action<Vector2>(OnShake),
                SolidChecker = new Func<Solid, bool>(IsRiding),
                JumpThruChecker = new Func<JumpThru, bool>(IsRiding),
            });
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            string dir = Direction.ToString().ToLower();

            for (int i = 0; i < size / 8; i++) {
                Image image = new Image(GFX.Game[$"{spikeType}_{dir}00"]);

                switch (Direction) {
                    case Spikes.Directions.Up:
                        image.JustifyOrigin(0.5f, 1f);
                        image.Position = Vector2.UnitX * (i + 0.5f) * 8f + Vector2.UnitY;
                        break;
                    case Spikes.Directions.Down:
                        image.JustifyOrigin(0.5f, 0f);
                        image.Position = Vector2.UnitX * (i + 0.5f) * 8f - Vector2.UnitY;
                        break;
                    case Spikes.Directions.Left:
                        image.JustifyOrigin(1f, 0.5f);
                        image.Position = Vector2.UnitY * (i + 0.5f) * 8f + Vector2.UnitX;
                        break;
                    case Spikes.Directions.Right:
                        image.JustifyOrigin(0f, 0.5f);
                        image.Position = Vector2.UnitY * (i + 0.5f) * 8f - Vector2.UnitX;
                        break;
                }

                base.Add(image);
            }
        }

        private void OnShake(Vector2 amount) {
            imageOffset += amount;
        }

        public override void Render() {
            Vector2 position = Position;
            Position += imageOffset;
            base.Render();
            Position = position;
        }

        private void OnCollide(Player player) {
            if (DashingIntoSpikes(player)) {
                return;
            }

            switch (Direction) {
                case Spikes.Directions.Up:
                    if (player.Speed.Y >= 0f && player.Bottom <= Bottom) {
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

        //returns bool based on if the player is still dash type state, 
        //and their direction is toward the spikes in at least 1 direction
        private bool DashingIntoSpikes(Player player) {
            return (player.DashAttacking
                || player.StateMachine.State == Player.StDash 
                || player.StateMachine.State == Player.StRedDash
                || player.StateMachine.State == Player.StDreamDash)
                && (Math.Sign(player.DashDir.X) == -Math.Sign(directionVector.X) 
                || Math.Sign(player.DashDir.Y) == -Math.Sign(directionVector.Y));
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
                    return CollideCheckOutside(solid, Position + Vector2.UnitY);
                case Spikes.Directions.Down:
                    return CollideCheckOutside(solid, Position - Vector2.UnitY);
                case Spikes.Directions.Left:
                    return CollideCheckOutside(solid, Position + Vector2.UnitX);
                case Spikes.Directions.Right:
                    return CollideCheckOutside(solid, Position - Vector2.UnitX);
                default:
                    return false;
            }
        }

        private bool IsRiding(JumpThru jumpThru) {
            Spikes.Directions direction = Direction;
            return direction == Spikes.Directions.Up && CollideCheck(jumpThru, Position + Vector2.UnitY);
        }
    }
}