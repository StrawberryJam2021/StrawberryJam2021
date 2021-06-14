using Celeste.Mod.StrawberryJam2021.Entities;
using FMOD.Studio;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Session : EverestModuleSession {
        public float WonkyBeatIncrement;
        public float WonkyBeatTimer;
        public int WonkyMaxBeats;
        public int WonkyBeatIndex;

        // Music stuff
        public int WonkyBpm;
        public int WonkyBars;
        public int WonkyBarLength = 4; // The top number in the time signature
        public int WonkyBeatLength = 4; // The bottom number in the time signature
        public string WonkyParam;

        public void AdvanceMusic(float time, Scene scene, EventInstance sfx) {
            WonkyBeatTimer += time;

            if (WonkyBeatTimer < WonkyBeatIncrement)
                return;

            WonkyBeatTimer -= WonkyBeatIncrement;

            // beatIndex is always in sixteenth notes
            var wonkyBlocks = scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>().ToList();
            int nextBeatIndex = (WonkyBeatIndex + 1) % WonkyMaxBeats;
            int beatInBar = WonkyBeatIndex / (16 / WonkyBeatLength) % WonkyBarLength; // current beat

            int nextBeatInBar = nextBeatIndex / (16 / WonkyBeatLength) % WonkyBarLength; // next beat
            bool beatIncrementsNext = (nextBeatIndex / (float) (16 / WonkyBeatLength)) % 1 == 0; // will the next beatIndex be the start of a new beat

            foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks) {
                wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);

                if (wonkyBlock.OnAtBeats.Contains(nextBeatInBar) != wonkyBlock.Activated && beatIncrementsNext) {
                    wonkyBlock.WillToggle();
                }
            }

            sfx.setParameterValue(WonkyParam, (WonkyBeatIndex * WonkyBeatLength / 16) + 1);

            // Doing this here because it would go to the next beat with a sixteenth note offset at start
            WonkyBeatIndex = (WonkyBeatIndex + 1) % WonkyMaxBeats;
        }
    }
}
