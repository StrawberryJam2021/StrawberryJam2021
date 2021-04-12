using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PelletEmitterUp = LoadUp",
        "SJ2021/PelletEmitterDown = LoadDown",
        "SJ2021/PelletEmitterLeft = LoadLeft",
        "SJ2021/PelletEmitterRight = LoadRight")]
    public class PelletEmitter : OrientableEntity {
        #region Static Loader Methods
        
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Up);
        
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Down);
        
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Left);
        
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Right);
        
        #endregion

        #region Properties

        public float PelletSpeed { get; private set; }
        
        public Color PelletColor { get; private set; }
        
        public bool CollideWithSolids { get; private set; }

        #endregion
        
        #region Private Fields

        private const float shotOriginOffset = 8f;
        private float timer = 2f;
        
        #endregion
        
        protected PelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
        }
        
        protected override void ReadEntityData(EntityData data) {
            PelletSpeed = data.Float("pelletSpeed", 100f);
            PelletColor = data.HexColor("pelletColor", Color.Red);
            CollideWithSolids = data.Bool("collideWithSolids", true);
        }

        protected override void AddComponents() {
            base.AddComponents();
            
            Add(new PelletFiringComponent {
                GetShotDirection = () => Orientation.Direction(),
                GetShotOrigin = () => Orientation.Direction() * shotOriginOffset,
                PelletSpeed = PelletSpeed,
                PelletColor = PelletColor,
                CollideWithSolids = CollideWithSolids
            });
            
            Sprite emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
            emitterSprite.Rotation = Orientation.Angle();
            
            Add(emitterSprite);
        }

        public override void Update() {
            base.Update();

            timer -= Engine.DeltaTime;
            if (timer < 0) {
                Get<PelletFiringComponent>().Fire();
                timer = 2f;
            }
        }
    }
}