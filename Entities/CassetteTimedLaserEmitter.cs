using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits a flickering laser beam.
    /// The laser beam turns on and off in time with cassette blocks.
    /// </summary>
    /// <remarks>
    /// Will kill the player by default, but its functionality can be changed.<br/>
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.<br/>
    /// Configurable values from Ahorn:<br/>
    /// <list type="bullet">
    /// <item><description>"alpha" =&gt; <see cref="LaserEmitter.Alpha"/></description></item>
    /// <item><description>"collideWithSolids" =&gt; <see cref="LaserEmitter.CollideWithSolids"/></description></item>
    /// <item><description>"cassetteIndex" =&gt; <see cref="CassetteIndex"/></description></item>
    /// <item><description>"disableLasers" =&gt; <see cref="LaserEmitter.DisableLasers"/></description></item>
    /// <item><description>"flicker" =&gt; <see cref="LaserEmitter.Flicker"/></description></item>
    /// <item><description>"killPlayer" =&gt; <see cref="LaserEmitter.KillPlayer"/></description></item>
    /// <item><description>"lengthInTicks" =&gt; <see cref="LengthInTicks"/></description></item>
    /// <item><description>"thickness" =&gt; <see cref="LaserEmitter.Thickness"/></description></item>
    /// <item><description>"tickOffset" =&gt; <see cref="TickOffset"/></description></item>
    /// <item><description>"triggerZipMovers" =&gt; <see cref="LaserEmitter.TriggerZipMovers"/></description></item>
    /// </list>
    /// </remarks>
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
        
        /// <summary>
        /// The cassette index swap on which to activate the beam.
        /// </summary>
        /// <remarks>
        /// This also affects the color of the beam, matching the cassette block colors from Celeste.
        /// </remarks>
        public int CassetteIndex { get; private set; }
        
        /// <summary>
        /// Ignored for <see cref="CassetteTimedLaserEmitter"/>.
        /// </summary>
        public override Color Color => CassetteListener.ColorFromCassetteIndex(CassetteIndex);
        
        /// <summary>
        /// The number of audible ticks that the laser will remain active.
        /// </summary>
        /// <remarks>
        /// Defaults to 2, which is (by default) the length a cassette is active before it swaps.
        /// </remarks>
        public int LengthInTicks { get; private set; }
        
        /// <summary>
        /// The number of audible ticks after a swap before the laser activates.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 (no delay).
        /// </remarks>
        public int TickOffset { get; private set; }
        
        #endregion

        #region Private Fields

        private CassetteListener cassetteListener;
        private int ticksRemaining;

        #endregion

        public CassetteTimedLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
        }

        protected override void ReadEntityData(EntityData data) {
            base.ReadEntityData(data);
            CassetteIndex = data.Int("cassetteIndex");
            TickOffset = data.Int("tickOffset");
            LengthInTicks = data.Int("lengthInTicks", 2);
        }
        
        protected override void AddComponents() {
            base.AddComponents();

            Add(cassetteListener = new CassetteListener {
                OnEntry = () => Collidable = false,
                OnTick = (index, tick) => {
                    if (--ticksRemaining == 0)
                        Collidable = false;

                    int currentTick = index * cassetteListener.TicksPerSwap + tick;
                    int targetTick = CassetteIndex * cassetteListener.TicksPerSwap + TickOffset;
                    
                    if (currentTick == targetTick) {
                        ticksRemaining = LengthInTicks;
                        Collidable = true;
                    }
                }
            });
        }
    }
}