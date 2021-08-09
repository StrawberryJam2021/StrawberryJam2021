using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
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

        public Action<CassetteState> OnBeat;

        protected virtual void InvokeOnTick(CassetteState state) => OnTick?.Invoke(state);
        protected virtual void InvokeOnSwap(CassetteState state) => OnSwap?.Invoke(state);
        protected virtual void InvokeOnSixteenth(CassetteState state) => OnSixteenth?.Invoke(state);

        protected virtual void InvokeOnBeat(CassetteState state) => OnBeat?.Invoke(state);

        #endregion

        #region Properties

        public CassetteState CurrentState { get; private set; }

        private int beatsPerTick => cassetteBlockManagerData?.Get<int>(nameof(beatsPerTick)) ?? 4;
        private int ticksPerSwap => cassetteBlockManagerData?.Get<int>(nameof(ticksPerSwap)) ?? 2;
        private int maxBeat => cassetteBlockManagerData?.Get<int>(nameof(maxBeat)) ?? 16;
        private int beatIndex => cassetteBlockManagerData?.Get<int>(nameof(beatIndex)) ?? 0;
        private int beatIndexMax => cassetteBlockManagerData?.Get<int>(nameof(beatIndexMax)) ?? 0;
        private int currentIndex => cassetteBlockManagerData?.Get<int>(nameof(currentIndex)) ?? 0;
        private float tempoMult => cassetteBlockManagerData?.Get<float>(nameof(tempoMult)) ?? 1f;

        #endregion

        #region Private Fields

        private CassetteBlockManager cassetteBlockManager;
        private DynData<CassetteBlockManager> cassetteBlockManagerData;

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
            cassetteBlockManagerData = new DynData<CassetteBlockManager>(cassetteBlockManager);
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            cassetteBlockManager = null;
            cassetteBlockManagerData = null;
        }

        public override void Update() {
            base.Update();

            if (cassetteBlockManager != null) {
                var lastState = CurrentState;
                UpdateState();

                if (CurrentState.Sixteenth != lastState.Sixteenth) {
                    InvokeOnSixteenth(CurrentState);
                }

                if (CurrentState.CurrentTick.Index != lastState.CurrentTick.Index) {
                    InvokeOnSwap(CurrentState);
                }

                if (CurrentState.Beat != lastState.Beat) {
                    InvokeOnBeat(CurrentState);
                }

                if (CurrentState.CurrentTick.Index != lastState.CurrentTick.Index ||
                    CurrentState.CurrentTick.Offset != lastState.CurrentTick.Offset) {
                    InvokeOnTick(CurrentState);
                }
            }
        }

        private void UpdateState() {
            float beatLength = tempoMult * (10f / 60f); // apparently one beat is 10 frames
            int index = currentIndex;

            var currentTick = new CassetteTick {
                Index = index,
                Offset = (beatIndex / beatsPerTick) % ticksPerSwap,
            };

            CurrentState = new CassetteState {
                BeatsPerTick = beatsPerTick,
                TicksPerSwap = ticksPerSwap,
                BeatCount = beatIndexMax,
                BlockCount = maxBeat / (beatsPerTick * ticksPerSwap),
                Sixteenth = cassetteBlockManager.GetSixteenthNote(),
                TempoMultiplier = tempoMult,
                Beat = beatIndex,
                CurrentTick = currentTick,
                NextTick = nextTick(currentTick),
                PreviousTick = previousTick(currentTick),
                BeatLength = beatLength,
            };
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
            public float BeatLength;
            public float TempoMultiplier;
            public int Sixteenth;
            public int Beat;

            public float TickLength => BeatLength * BeatsPerTick;
            public float SwapLength => BeatLength * BeatsPerTick * TicksPerSwap;

            public CassetteTick CurrentTick;
            public CassetteTick NextTick;
            public CassetteTick PreviousTick;
        }
    }
}