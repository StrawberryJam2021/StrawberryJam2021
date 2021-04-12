using Monocle;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class CassetteListener : Component {
        public Action OnEntry;
        public Action<int> OnTick;
        public Action<int> OnSwap;
        public Action<int> OnSixteenth;
        
        private CassetteBlockManager cassetteBlockManager;
        
        private int lastSixteenth = -1;
        private int lastBlockIndex = -1;
        private int lastBeatIndex = -1;

        private int beatsPerTick;

        private static readonly FieldInfo currentIndexFieldInfo = typeof(CassetteBlockManager).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatIndexFieldInfo = typeof(CassetteBlockManager).GetField("beatIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo beatsPerTickFieldInfo = typeof(CassetteBlockManager).GetField("beatsPerTick", BindingFlags.Instance | BindingFlags.NonPublic);

        protected virtual void InvokeOnEntry() => OnEntry?.Invoke();
        protected virtual void InvokeOnTick(int tick) => OnTick?.Invoke(tick);
        protected virtual void InvokeOnSwap(int index) => OnSwap?.Invoke(index);
        protected virtual void InvokeOnSixteenth(int sixteenth) => OnSixteenth?.Invoke(sixteenth);
        
        public CassetteListener() : base(true, false)
        {
        }

        public override void EntityAwake() {
            base.EntityAwake();
            if (cassetteBlockManager == null) return;

            beatsPerTick = (int) beatsPerTickFieldInfo.GetValue(cassetteBlockManager);
            
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

            int sixteenth = cassetteBlockManager.GetSixteenthNote() - 1;
            if (sixteenth != lastSixteenth) {
                lastSixteenth = sixteenth;
                InvokeOnSixteenth(sixteenth);
            }

            int currentBlockIndex = (int)currentIndexFieldInfo.GetValue(cassetteBlockManager);
            if (currentBlockIndex != lastBlockIndex) {
                lastBlockIndex = currentBlockIndex;
                InvokeOnSwap(currentBlockIndex);
            }

            int currentBeatIndex = (int) beatIndexFieldInfo.GetValue(cassetteBlockManager);
            if (currentBeatIndex != lastBeatIndex) {
                lastBeatIndex = currentBeatIndex;
                if (currentBeatIndex % beatsPerTick == 0)
                    InvokeOnTick(currentBeatIndex / beatsPerTick);
            }
        }
    }
}