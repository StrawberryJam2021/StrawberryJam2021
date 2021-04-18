using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that automatically emits pellets that will kill the player on contact.
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.
    /// </summary>
    /// <remarks>
    /// Emitter configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>frequency</term><description>
    /// How often pellets should be fired, in seconds. Defaults to 2 seconds.
    /// </description></item>
    /// <item><term>offset</term><description>
    /// The number of seconds to delay firing.
    /// Defaults to 0 (fires immediately).
    /// </description></item>
    /// <item><term>count</term><description>
    /// The number of pellets that should be fired at once.
    /// Defaults to 1.
    /// </description></item>
    /// </list>
    /// Pellet configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not pellets will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>pelletColor</term><description>
    /// The <see cref="Microsoft.Xna.Framework.Color"/> used to render the pellets.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>pelletSpeed</term><description>
    /// The number of units per second that the pellet should travel.
    /// Defaults to 100.
    /// </description></item>
    /// </list>
    /// </remarks>
    [CustomEntity("SJ2021/PelletEmitterUp = LoadUp",
        "SJ2021/PelletEmitterDown = LoadDown",
        "SJ2021/PelletEmitterLeft = LoadLeft",
        "SJ2021/PelletEmitterRight = LoadRight")]
    public class AutomaticPelletEmitter : PelletEmitter {
        #region Static Loader Methods
        
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new AutomaticPelletEmitter(data, offset, Orientations.Up);
        
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new AutomaticPelletEmitter(data, offset, Orientations.Down);
        
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new AutomaticPelletEmitter(data, offset, Orientations.Left);
        
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new AutomaticPelletEmitter(data, offset, Orientations.Right);
        
        #endregion
        
        public AutomaticPelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
        }
        
        protected override PelletFiringComponent CreateFiringComponent(EntityData data) =>
            new AutomaticPelletFiringComponent<PelletShot> {
                Frequency = data.Float("frequency", 2f),
                Offset = data.Float("offset"),
                Count = data.Int("count", 1)
            };
    }
}