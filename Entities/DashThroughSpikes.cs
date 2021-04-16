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
    [TrackedAs(typeof(Spikes))]
    class DashThroughSpikes: Spikes {
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new DashThroughSpikes(offset, entityData, Directions.Up, entityData.Attr("type", "default"));
        }
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new DashThroughSpikes(offset, entityData, Directions.Down, entityData.Attr("type", "default"));
        }
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new DashThroughSpikes(offset, entityData, Directions.Left, entityData.Attr("type", "default"));
        }
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new DashThroughSpikes(offset, entityData, Directions.Right, entityData.Attr("type", "default"));
        }

        public DashThroughSpikes(Vector2 position, EntityData data, Spikes.Directions dir, string type) : base(position, GetSize(data, dir), dir, type) {
            base.Add(new PlayerCollider(new Action<Player>(this.OnCollide), null, null));
        }
       // public DashThroughSpikes(EntityData data, Vector2 offset, Spikes.Directions dir) : this(data.Position + offset, GetSize(data, dir), dir) {}


        private void OnCollide(Player player) {
            if (DashingIntoSpikes(player.DashDir)) {
                return;
            }

            switch (this.Direction) {
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

        private Vector2 DashDirToVector(Spikes.Directions dir) {

            return dir switch {
                Spikes.Directions.Up => Vector2.UnitY,
                Spikes.Directions.Down => -Vector2.UnitY,
                Spikes.Directions.Right => Vector2.UnitX,
                _ => -Vector2.UnitX, //left or default case
               
            };
        }

        private bool DashingIntoSpikes(Vector2 DashDir) {
            return DashDirToVector(Direction) == -DashDir;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        private static int GetSize(EntityData data, Spikes.Directions dir) {
            if (dir > Spikes.Directions.Down) {
                int num = dir - Spikes.Directions.Left;
                return data.Height;
            }
            return data.Width;
        }

    }
}
