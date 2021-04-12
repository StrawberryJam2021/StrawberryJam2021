using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteTimedLaserEmitterUp = LoadUp",
        "SJ2021/CassetteTimedLaserEmitterDown = LoadDown",
        "SJ2021/CassetteTimedLaserEmitterLeft = LoadLeft",
        "SJ2021/CassetteTimedLaserEmitterRight = LoadRight")]
    public class CassetteTimedLaserEmitter : LaserEmitter {
        #region Static Loader Methods
        
        public new static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedLaserEmitter(data, offset, Orientations.Up);
        
        public new static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedLaserEmitter(data, offset, Orientations.Down);
        
        public new static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedLaserEmitter(data, offset, Orientations.Left);
        
        public new static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedLaserEmitter(data, offset, Orientations.Right);
        
        #endregion
        
        #region Properties

        public override Color Color => CassetteListener.ColorFromCassetteIndex(CassetteIndex);

        public int CassetteIndex { get; private set; }
        
        #endregion

        public CassetteTimedLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
        }

        protected override void ReadEntityData(EntityData data) {
            base.ReadEntityData(data);
            CassetteIndex = data.Int("cassetteIndex");
        }
        
        protected override void AddComponents() {
            base.AddComponents();

            Add(new CassetteListener {
                OnSwap = index => Collidable = index == CassetteIndex
            });
        }
    }
}