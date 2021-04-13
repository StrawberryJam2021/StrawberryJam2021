using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class CassetteListener : Component {
        public Action OnEntry;
        public Action<int, int> OnTick;
        public Action<int> OnSwap;
        public Action<int, int> OnSixteenth;
        
        private CassetteBlockManager cassetteBlockManager;
        
        private int lastSixteenth = -1;
        private int lastBlockIndex = -1;
        private int lastBeatIndex = -1;

        private static readonly FieldInfo currentIndexFieldInfo = typeof(CassetteBlockManager).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatIndexFieldInfo = typeof(CassetteBlockManager).GetField("beatIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatsPerTickFieldInfo = typeof(CassetteBlockManager).GetField("beatsPerTick", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ticksPerSwapFieldInfo = typeof(CassetteBlockManager).GetField("ticksPerSwap", BindingFlags.Instance | BindingFlags.NonPublic);

        public int CurrentIndex => cassetteBlockManager == null ? 0 : (int) currentIndexFieldInfo.GetValue(cassetteBlockManager);
        public int CurrentSixteenth => cassetteBlockManager?.GetSixteenthNote() - 1 ?? 0;
        public int CurrentTick => (CurrentBeat / BeatsPerTick) % TicksPerSwap;
        public int CurrentBeat => cassetteBlockManager == null ? 0 : (int) beatIndexFieldInfo.GetValue(cassetteBlockManager);
        
        public int BeatsPerTick { get; private set; }
        public int TicksPerSwap { get; private set; }
        
        protected virtual void InvokeOnEntry() => OnEntry?.Invoke();
        protected virtual void InvokeOnTick(int index, int tick) => OnTick?.Invoke(index, tick);
        protected virtual void InvokeOnSwap(int index) => OnSwap?.Invoke(index);
        protected virtual void InvokeOnSixteenth(int index, int sixteenth) => OnSixteenth?.Invoke(index, sixteenth);
        
        public CassetteListener() : base(true, false)
        {
        }

        public override void EntityAwake() {
            base.EntityAwake();
            if (cassetteBlockManager == null) return;

            BeatsPerTick = cassetteBlockManager == null ? 4 : (int) beatsPerTickFieldInfo.GetValue(cassetteBlockManager);
            TicksPerSwap = cassetteBlockManager == null ? 2 : (int) ticksPerSwapFieldInfo.GetValue(cassetteBlockManager);
            
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