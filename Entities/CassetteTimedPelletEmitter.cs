using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits pellets that will kill the player on contact.
    /// Pellets are automatically fired in time with cassette blocks.
    /// </summary>
    /// <remarks>
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.<br/>
    /// Configurable values from Ahorn:<br/>
    /// <list type="bullet">
    /// <item><description>"collideWithSolids" =&gt; <see cref="PelletEmitter.CollideWithSolids"/></description></item>
    /// <item><description>"cassetteIndex" =&gt; <see cref="CassetteIndex"/></description></item>
    /// <item><description>"pelletCount" =&gt; <see cref="PelletEmitter.PelletCount"/></description></item>
    /// <item><description>"pelletSpeed" =&gt; <see cref="PelletEmitter.PelletSpeed"/></description></item>
    /// <item><description>"tickOffset" =&gt; <see cref="TickOffset"/></description></item>
    /// </list>
    /// </remarks>
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
        
        /// <summary>
        /// The cassette index swap on which to fire pellets.
        /// </summary>
        /// <remarks>
        /// This also affects the color of the pellets, matching the cassette block colors from Celeste.
        /// </remarks>
        public int CassetteIndex { get; private set; }
        
        /// <summary>
        /// Ignored for <see cref="CassetteTimedPelletEmitter"/>.
        /// </summary>
        public override float Frequency => 0;

        /// <summary>
        /// Ignored for <see cref="CassetteTimedPelletEmitter"/>.
        /// </summary>
        public override Color PelletColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);
        
        /// <summary>
        /// The number of audible "ticks" that should be heard after a cassette swap before pellets are fired.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 (fire immediately on cassette swap).
        /// </remarks>
        public int TickOffset { get; private set; }
        
        protected CassetteTimedPelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation)
        {
        }

        protected override void ReadEntityData(EntityData data) {
            base.ReadEntityData(data);
            CassetteIndex = data.Int("cassetteIndex");
            TickOffset = data.Int("tickOffset");
        }

        protected override void AddComponents() {
            base.AddComponents();

            Add(new CassetteListener {
                OnTick = (index, tick) => {
                    if (index == CassetteIndex && tick == TickOffset)
                        Get<PelletFiringComponent>().Fire();
                }
            });
        }
    }
}