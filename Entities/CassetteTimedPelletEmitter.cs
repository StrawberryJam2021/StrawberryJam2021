using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits pellets that will kill the player on contact.
    /// Pellets are automatically fired in time with cassette blocks.
    /// Has four available orientations, indicated by the <see cref="OrientableEntity.Orientations"/> enum.
    /// </summary>
    /// <remarks>
    /// Emitter configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>cassetteIndices</term><description>=&gt; <see cref="CassetteIndices"/><br/>
    /// Which of the cassette swap indices will fire pellets.
    /// This also affects the color of the pellets, matching the cassette block colors from Celeste.
    /// Setting to [-1] (default) will fire on every cassette swap.
    /// </description></item>
    /// <item><term>ticks</term><description>=&gt; <see cref="Ticks"/><br/>
    /// Which of the audible "ticks" after a cassette swap will fire a pellet.
    /// Defined in entity data as a comma-separated list of integers.
    /// Defaults to [0] (fire once, immediately on cassette swap).
    /// Setting to [0,1] will fire on both ticks of a standard "2 tick per swap" rhythm.
    /// Setting to [-1] will fire on every tick regardless of how many ticks per swap.
    /// </description></item>
    /// <item><term>pelletCount</term><description>=&gt; <see cref="PelletFiringComponent.Count"/><br/>
    /// The number of pellets that should be fired at once.
    /// Defaults to 1.
    /// </description></item>
    /// </list>
    /// Pellet configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>collideWithSolids</term><description>=&gt; <see cref="PelletFiringComponent.PelletComponent.PelletComponentSettings.CollideWithSolids"/><br/>
    /// Whether or not pellets will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>pelletColor</term><description>=&gt; <see cref="PelletFiringComponent.PelletComponent.PelletComponentSettings.Color"/><br/>
    /// The <see cref="Microsoft.Xna.Framework.Color"/> used to render the pellets.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>pelletSpeed</term><description>=&gt; <see cref="PelletFiringComponent.PelletComponent.PelletComponentSettings.Speed"/><br/>
    /// The number of units per second that the pellet should travel.
    /// Defaults to 100.
    /// </description></item>
    /// </list>
    /// </remarks>
    [CustomEntity("SJ2021/CassetteTimedPelletEmitterUp = LoadUp",
        "SJ2021/CassetteTimedPelletEmitterDown = LoadDown",
        "SJ2021/CassetteTimedPelletEmitterLeft = LoadLeft",
        "SJ2021/CassetteTimedPelletEmitterRight = LoadRight")]
    public class CassetteTimedPelletEmitter : PelletEmitter {
        #region Static Loader Methods
        
        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Up);
        
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Down);
        
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Left);
        
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new CassetteTimedPelletEmitter(data, offset, Orientations.Right);
        
        #endregion
        
        public int[] CassetteIndices { get; private set; }
        public int[] Ticks { get; private set; }
        
        protected CassetteTimedPelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation)
        {
            string indices = data.Attr("cassetteIndices");
            CassetteIndices = indices.Split(',').Select(s => int.TryParse(s, out int i) ? Calc.Clamp(i, 0, 3) : 0).ToArray();
            
            string ticks = data.Attr("ticks", "0");
            Ticks = ticks.Split(',').Select(s => int.TryParse(s, out int i) ? Math.Max(i, 0) : 0).ToArray();
            
            Add(new CassetteListener {
                OnTick = (index, tick) => {
                    if ((!CassetteIndices.Any() || CassetteIndices.Contains(index)) && (!Ticks.Any() || Ticks.Contains(tick)))
                        Get<PelletFiringComponent>().Fire(comp => comp.Settings.Color = CassetteListener.ColorFromCassetteIndex(index));
                }
            });
        }
    }
}