using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits a flickering laser beam.
    /// The laser beam turns on and off in time with cassette blocks.
    /// </summary>
    /// <remarks>
    /// Emitter configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>cassetteIndices</term><description>
    /// Which of the cassette swap indices should enable the laser.
    /// This also affects the color of the beam, matching the cassette block colors from Celeste.
    /// Defaults to [0] (first cassette index only).
    /// Setting to [-1] will fire on every cassette swap.
    /// </description></item>
    /// <item><term>ticks</term><description>
    /// Which of the audible "ticks" after a cassette swap should enable the laser.
    /// Defined in entity data as a comma-separated list of integers.
    /// Defaults to [0] (fire once, immediately on cassette swap).
    /// Setting to [0,1] will fire on both ticks of a standard "2 tick per swap" rhythm.
    /// Setting to [-1] will fire on every tick regardless of how many ticks per swap.
    /// </description></item>
    /// <item><term>lengthInTicks</term><description>
    /// The number of audible ticks that the laser should be enabled for.
    /// Defaults to 2 (an entire cassette swap length).
    /// </description></item>
    /// </list>
    /// Laser configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>alpha</term><description>
    /// The base alpha value for the beam.
    /// Defaults to 0.4 (40%).
    /// </description></item>
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>color</term><description>
    /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>disableLasers</term><description>
    /// Whether or not colliding with this beam will disable all beams of the same color.
    /// Defaults to false.
    /// </description></item>
    /// <item><term>flicker</term><description>
    /// Whether or not the beam should flicker.
    /// Defaults to true, flickering 4 times per second.
    /// </description></item>
    /// <item><term>killPlayer</term><description>
    /// Whether or not colliding with the beam will kill the player.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>thickness</term><description>
    /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
    /// Defaults to 6 pixels.
    /// </description></item>
    /// <item><term>triggerZipMovers</term><description>
    /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
    /// Defaults to false.
    /// </description></item>
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
        
        public int[] CassetteIndices { get; }
        public int[] Ticks { get; }
        public int LengthInTicks { get; }
        
        #endregion

        #region Private Fields

        private CassetteListener cassetteListener;
        private int ticksRemaining;

        #endregion

        public CassetteTimedLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            string indices = data.Attr("cassetteIndices", "0");
            CassetteIndices = indices.Split(',').Select(s => int.TryParse(s, out int i) ? Calc.Clamp(i, 0, 3) : 0).ToArray();
            
            string ticks = data.Attr("ticks", "0");
            Ticks = ticks.Split(',').Select(s => int.TryParse(s, out int i) ? Math.Max(i, 0) : 0).ToArray();
            
            LengthInTicks = data.Int("lengthInTicks", 2);

            Add(cassetteListener = new CassetteListener {
                OnEntry = () => Collidable = false,
                OnTick = (index, tick) => {
                    if (--ticksRemaining == 0)
                        Collidable = false;

                    if ((CassetteIndices.FirstOrDefault() == -1 || CassetteIndices.Contains(index)) && (Ticks.FirstOrDefault() == -1 || Ticks.Contains(tick))) {
                        ticksRemaining = LengthInTicks;
                        Get<LaserBeamComponent>().Color = CassetteListener.ColorFromCassetteIndex(index);
                        Collidable = true;
                    }
                }
            });
        }
    }
}