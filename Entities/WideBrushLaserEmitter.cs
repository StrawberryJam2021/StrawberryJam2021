using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WideBrushLaserEmitterUp = LoadUp",
        "SJ2021/WideBrushLaserEmitterDown = LoadDown",
        "SJ2021/WideBrushLaserEmitterLeft = LoadLeft",
        "SJ2021/WideBrushLaserEmitterRight = LoadRight")]
    public class WideBrushLaserEmitter : BrushLaserEmitter {
        public new static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new WideBrushLaserEmitter(data, offset, Orientations.Up);

        public new static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new WideBrushLaserEmitter(data, offset, Orientations.Down);

        public new static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new WideBrushLaserEmitter(data, offset, Orientations.Left);

        public new static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new WideBrushLaserEmitter(data, offset, Orientations.Right);

        protected int Size => Orientation.Vertical() ? DataWidth : DataHeight;

        public WideBrushLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation)
        {
        }

        protected override IEnumerable<LaserColliderComponent> CreateLaserColliders() {
            const int tileSize = 8;
            int count = Size / tileSize;
            var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);
            var start = offset / 2;
            return Enumerable.Range(0, count).Select(i => new LaserColliderComponent {
                CollideWithSolids = CollideWithSolids, Offset = start + offset * i, Thickness = tileSize,
            });
        }

        protected override IEnumerable<Sprite> CreateEmitterSprites() {
            const int tileSize = 8;
            int count = Size / (tileSize * 2);
            var offset = Orientation.Vertical() ? new Vector2(tileSize * 2, 0) : new Vector2(0, tileSize * 2);
            var start = offset / 2;
            return Enumerable.Range(0, count).Select(i => ConfigureEmitterSprite(StrawberryJam2021Module.SpriteBank.Create("brushLaserEmitter"), start + offset * i));
        }
    }
}