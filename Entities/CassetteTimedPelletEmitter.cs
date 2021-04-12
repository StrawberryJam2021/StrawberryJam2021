using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteTimedPelletEmitterUp = LoadUp",
        "SJ2021/CassetteTimedPelletEmitterDown = LoadDown",
        "SJ2021/CassetteTimedPelletEmitterLeft = LoadLeft",
        "SJ2021/CassetteTimedPelletEmitterRight = LoadRight")]
    public class CassetteTimedPelletEmitter : PelletEmitter {
        #region Static Loader Methods
        
        public new static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Up);
        
        public new static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Down);
        
        public new static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Left);
        
        public new static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Right);
        
        #endregion
        
        public override Color PelletColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);

        public override float Frequency => 0;

        public int CassetteIndex { get; private set; }
        
        protected CassetteTimedPelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation)
        {
        }

        protected override void ReadEntityData(EntityData data) {
            base.ReadEntityData(data);
            CassetteIndex = data.Int("cassetteIndex");
        }

        protected override void AddComponents() {
            base.AddComponents();
            Add(new CassetteListener {
                OnSwap = index => {
                    if (index == CassetteIndex)
                        Get<PelletFiringComponent>().Fire();
                }
            });
        }
    }
}