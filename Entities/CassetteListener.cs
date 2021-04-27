using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Allows an entity to subscribe to certain events from a <see cref="CassetteBlockManager"/>.
    /// </summary>
    /// <remarks>
    /// Provided events are:
    /// <list type="bullet">
    /// <item><description><see cref="OnEntry"/> =&gt; Invoked when the entity is added to the scene.</description></item>
    /// <item><description><see cref="OnSwap"/> =&gt; Invoked when cassette blocks swap.</description></item>
    /// <item><description><see cref="OnTick"/> =&gt; Invoked when a "click" sound is heard.</description></item>
    /// <item><description><see cref="OnSixteenth"/> =&gt; Invoked the first frame of a sixteenth beat.</description></item>
    /// </list>
    /// </remarks>
    public class CassetteListener : Component {
        #region Events
        
        /// <summary>
        /// Invoked when the entity is added to the scene.
        /// </summary>
        public Action OnEntry;
        
        /// <summary>
        /// Invoked when a "click" sound is heard.
        /// </summary>
        /// <remarks>
        /// Arguments: (index, tick) where index is the current cassette block, and tick is a 0-based offset from the most recent swap.
        /// </remarks>
        public Action<int, int> OnTick;
        
        /// <summary>
        /// Invoked when cassette blocks swap.
        /// </summary>
        /// <remarks>
        /// Arguments: (index) where index is the current cassette block.
        /// </remarks>
        public Action<int> OnSwap;
        
        /// <summary>
        /// Invoked the first frame of a sixteenth beat.
        /// </summary>
        /// <remarks>
        /// Arguments: (index, sixteenth) where index is the current cassette block,
        /// and sixteenth is one less than <see cref="CassetteBlockManager.GetSixteenthNote"/> (0-based).
        /// </remarks>
        public Action<int, int> OnSixteenth;
        
        protected virtual void InvokeOnEntry() => OnEntry?.Invoke();
        protected virtual void InvokeOnTick(int index, int tick) => OnTick?.Invoke(index, tick);
        protected virtual void InvokeOnSwap(int index) => OnSwap?.Invoke(index);
        protected virtual void InvokeOnSixteenth(int index, int sixteenth) => OnSixteenth?.Invoke(index, sixteenth);
        
        #endregion

        #region Properties

        /// <summary>
        /// The active cassette index, ranges from 0 to 3.
        /// </summary>
        public int CurrentIndex => cassetteBlockManager == null ? 0 : (int) currentIndexFieldInfo.GetValue(cassetteBlockManager);
        
        /// <summary>
        /// One less than the result of <see cref="CassetteBlockManager.GetSixteenthNote"/> (0-based).
        /// </summary>
        public int CurrentSixteenth => cassetteBlockManager?.GetSixteenthNote() - 1 ?? 0;
        
        /// <summary>
        /// The number of ticks that have passed since the most recent swap.
        /// </summary>
        public int CurrentTick => (CurrentBeat / BeatsPerTick) % TicksPerSwap;
        
        /// <summary>
        /// The number of beats that have passed since the most recent cassette room cycle.
        /// </summary>
        public int CurrentBeat => cassetteBlockManager == null ? 0 : (int) beatIndexFieldInfo.GetValue(cassetteBlockManager);
        
        /// <summary>
        /// The number of beats that make up a single audible tick.
        /// </summary>
        public int BeatsPerTick { get; private set; } = 4;
        
        /// <summary>
        /// The number of audible ticks that make up a single cassette swap.
        /// </summary>
        public int TicksPerSwap { get; private set; } = 2;

        #endregion
        
        #region Private Fields

        private CassetteBlockManager cassetteBlockManager;
        
        private int lastSixteenth = -1;
        private int lastBlockIndex = -1;
        private int lastBeatIndex = -1;

        private static readonly FieldInfo currentIndexFieldInfo = typeof(CassetteBlockManager).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatIndexFieldInfo = typeof(CassetteBlockManager).GetField("beatIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatsPerTickFieldInfo = typeof(CassetteBlockManager).GetField("beatsPerTick", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ticksPerSwapFieldInfo = typeof(CassetteBlockManager).GetField("ticksPerSwap", BindingFlags.Instance | BindingFlags.NonPublic);
        
        #endregion
        
        public CassetteListener() : base(true, false)
        {
        }

        public override void EntityAwake() {
            base.EntityAwake();
            if (cassetteBlockManager == null) return;

            int beatsPerTick = (int) beatsPerTickFieldInfo.GetValue(cassetteBlockManager);
            int ticksPerSwap = (int) ticksPerSwapFieldInfo.GetValue(cassetteBlockManager);

            if (beatsPerTick > 0) BeatsPerTick = beatsPerTick;
            if (ticksPerSwap > 0) TicksPerSwap = ticksPerSwap;
            
            InvokeOnEntry();
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);
            
            if (!(scene is Level level)) return;
            level.HasCassetteBlocks = true;
            
            cassetteBlockManager = scene.Tracker.GetEntity<CassetteBlockManager>() ?? scene.Entities.ToAdd.OfType<CassetteBlockManager>().FirstOrDefault();
            if (cassetteBlockManager == null)
                scene.Add(cassetteBlockManager = new CassetteBlockManager());
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            cassetteBlockManager = null;
        }

        public override void Update() {
            base.Update();
            if (cassetteBlockManager == null) return;
            
            int sixteenth = CurrentSixteenth;
            int currentBlockIndex = CurrentIndex;
            int currentBeatIndex = CurrentBeat;
            int currentTick = CurrentTick;
            
            if (sixteenth != lastSixteenth) {
                lastSixteenth = sixteenth;
                InvokeOnSixteenth(currentBlockIndex, sixteenth);
            }

            if (currentBlockIndex != lastBlockIndex) {
                lastBlockIndex = currentBlockIndex;
                InvokeOnSwap(currentBlockIndex);
            }
            
            if (currentBeatIndex != lastBeatIndex) {
                lastBeatIndex = currentBeatIndex;
                if (currentBeatIndex % BeatsPerTick == 0)
                    InvokeOnTick(currentBlockIndex, currentTick);
            }
        }
        
        public static Color ColorFromCassetteIndex(int index) => index switch {
            0 => Calc.HexToColor("49aaf0"),
            1 => Calc.HexToColor("f049be"),
            2 => Calc.HexToColor("fcdc3a"),
            3 => Calc.HexToColor("38e04e"),
            _ => Color.White
        };
    }
}