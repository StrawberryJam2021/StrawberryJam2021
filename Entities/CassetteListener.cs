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
    /// <item><description><see cref="OnSwap"/> =&gt; Invoked when cassette blocks swap.</description></item>
    /// <item><description><see cref="OnTick"/> =&gt; Invoked when a "click" sound is heard.</description></item>
    /// <item><description><see cref="OnSixteenth"/> =&gt; Invoked the first frame of a sixteenth beat.</description></item>
    /// </list>
    /// </remarks>
    public class CassetteListener : Component {
        #region Events

        /// <summary>
        /// Invoked when a "click" sound is heard.
        /// </summary>
        public Action<CassetteState> OnTick;

        /// <summary>
        /// Invoked when cassette blocks swap.
        /// </summary>
        public Action<CassetteState> OnSwap;

        /// <summary>
        /// Invoked the first frame of a sixteenth beat.
        /// </summary>
        public Action<CassetteState> OnSixteenth;

        protected virtual void InvokeOnTick(CassetteState state) => OnTick?.Invoke(state);
        protected virtual void InvokeOnSwap(CassetteState state) => OnSwap?.Invoke(state);
        protected virtual void InvokeOnSixteenth(CassetteState state) => OnSixteenth?.Invoke(state);

        #endregion

        #region Properties

        public CassetteState CurrentState { get; private set; }

        private int beatsPerTick => cassetteBlockManager == null ? 4 : (int) beatsPerTickFieldInfo.GetValue(cassetteBlockManager);
        private int ticksPerSwap => cassetteBlockManager == null ? 2 : (int) ticksPerSwapFieldInfo.GetValue(cassetteBlockManager);
        private int maxBeat => cassetteBlockManager == null ? 16 : (int) maxBeatFieldInfo.GetValue(cassetteBlockManager);
        private int beatIndex => cassetteBlockManager == null ? 0 : (int) beatIndexFieldInfo.GetValue(cassetteBlockManager);
        private int beatIndexMax => cassetteBlockManager == null ? 0 : (int) beatIndexMaxFieldInfo.GetValue(cassetteBlockManager);
        private int currentIndex => cassetteBlockManager == null ? 0 : (int) currentIndexFieldInfo.GetValue(cassetteBlockManager);
        private float tempoMult => cassetteBlockManager == null ? 1f : (float) tempoMultFieldInfo.GetValue(cassetteBlockManager);

        #endregion

        #region Private Fields

        private CassetteBlockManager cassetteBlockManager;

        private static readonly FieldInfo currentIndexFieldInfo = typeof(CassetteBlockManager).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatIndexFieldInfo = typeof(CassetteBlockManager).GetField("beatIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatIndexMaxFieldInfo = typeof(CassetteBlockManager).GetField("beatIndexMax", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatsPerTickFieldInfo = typeof(CassetteBlockManager).GetField("beatsPerTick", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ticksPerSwapFieldInfo = typeof(CassetteBlockManager).GetField("ticksPerSwap", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo maxBeatFieldInfo = typeof(CassetteBlockManager).GetField("maxBeat", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo tempoMultFieldInfo = typeof(CassetteBlockManager).GetField("tempoMult", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

        private CassetteTick nextTick(CassetteTick tick) {
            if (++tick.Offset >= ticksPerSwap) {
                tick.Offset = 0;
                if (++tick.Index >= maxBeat)
                    tick.Index = 0;
            }
            return tick;
        }

        private CassetteTick previousTick(CassetteTick tick) {
            if (--tick.Offset < 0) {
                tick.Offset = ticksPerSwap - 1;
                if (--tick.Index < 0)
                    tick.Index = maxBeat - 1;
            }
            return tick;
        }

        public CassetteListener() : base(true, false)
        {
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

            var currentTick = new CassetteTick {
                Index = currentIndex,
                Offset = (beatIndex / beatsPerTick) % ticksPerSwap
            };

            var lastState = CurrentState;

            CurrentState = new CassetteState {
                BeatsPerTick = beatsPerTick,
                TicksPerSwap = ticksPerSwap,
                BeatCount = beatIndexMax,
                BlockCount = maxBeat / (beatsPerTick * ticksPerSwap),
                Sixteenth = cassetteBlockManager.GetSixteenthNote(),
                Beat = beatIndex,
                CurrentTick = currentTick,
                NextTick = nextTick(currentTick),
                PreviousTick = previousTick(currentTick),
                TickLength = tempoMult * beatsPerTick * (10f / 60f), // apparently one beat is 10 frames
            };

            if (CurrentState.Sixteenth != lastState.Sixteenth) {
                InvokeOnSixteenth(CurrentState);
            }

            if (CurrentState.CurrentTick.Index != lastState.CurrentTick.Index) {
                InvokeOnSwap(CurrentState);
            }

            if (CurrentState.CurrentTick.Index != lastState.CurrentTick.Index || CurrentState.CurrentTick.Offset != lastState.CurrentTick.Offset) {
                InvokeOnTick(CurrentState);
            }
        }

        public static Color ColorFromCassetteIndex(int index) => index switch {
            0 => Calc.HexToColor("49aaf0"),
            1 => Calc.HexToColor("f049be"),
            2 => Calc.HexToColor("fcdc3a"),
            3 => Calc.HexToColor("38e04e"),
            _ => Color.White
        };

        public struct CassetteTick {
            public int Index;
            public int Offset;
        }

        public struct CassetteState {
            public int BeatsPerTick;
            public int TicksPerSwap;
            public int BeatCount;
            public int BlockCount;
            public float TickLength;

            public int Sixteenth;
            public int Beat;

            public CassetteTick CurrentTick;
            public CassetteTick NextTick;
            public CassetteTick PreviousTick;
        }
    }
}