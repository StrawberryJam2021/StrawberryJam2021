using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WonkyMinorCassetteBlockController")]
    [Tracked]
    public class WonkyMinorCassetteBlockController : Entity {

        // Music stuff
        public readonly int barLength; // The top number in the time signature
        public readonly int beatLength; // The bottom number in the time signature

        public readonly int ControllerIndex;

        public int CassetteWonkyBeatIndex;
        public float CassetteBeatTimer;

        private float beatIncrement;
        private float beatDelta;
        private int maxBeats;

        public WonkyMinorCassetteBlockController(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Attr("timeSignature"), data.Int("controllerIndex", 1)) { }

        public WonkyMinorCassetteBlockController(Vector2 position, string timeSignature, int controllerIndex)
            : base(position) {

            GroupCollection timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;
            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            barLength = int.Parse(timeSignatureParsed[1].Value);
            beatLength = int.Parse(timeSignatureParsed[2].Value);

            if (controllerIndex < 1)
                throw new ArgumentException($"Controller Index must be 1 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;
        }

        // Reset cassette position to start of a bar
        public void Reset(StrawberryJam2021Session session) {
            this.CassetteWonkyBeatIndex = 0;
            // Timer has to be offset by the beat increment delta to account for different start of the next bar
            // This is because the index is the index of the next played note, not the current one
            this.CassetteBeatTimer = beatDelta + session.CassetteBeatTimer;
        }

        // Synchronize cassette position to start of a bar
        // Next tick will activate the first beat
        public void Synchronize(float time, StrawberryJam2021Session session) {
            this.CassetteWonkyBeatIndex = 0;
            this.CassetteBeatTimer = beatDelta + (session.CassetteBeatTimer - time);
        }

        // Called by main controller
        public void MinorAwake(Scene scene, StrawberryJam2021Session session, WonkyCassetteBlockController mainController) {
            if (beatLength != mainController.beatLength)
                throw new ArgumentException($"Minor and main controller time signature denominator don't match. Main is {mainController.beatLength}, minor is {beatLength}");

            // Ensure that session.CassetteBeatTimer is always smaller than this.beatIncrement
            if (barLength > mainController.barLength)
                throw new ArgumentException($"Minor controller has a larger time signature numerator than main controller. Main controller must have the largest time signature numerator.  Main is {mainController.barLength}, minor is {barLength}");

            // Get minor bpm from main controller
            // This has to divide into integers, otherwise I am not responsible for desyncs
            int bpm = mainController.bpm * this.barLength / mainController.barLength;

            // We always want sixteenth notes here, regardless of time signature
            beatIncrement = (float) (60.0 / bpm * beatLength / 16.0);
            // The max index is only a single bar in minor controllers
            maxBeats = 16 * barLength / beatLength;

            // Synchronize the beat indices.
            // Progress towards the next beat
            float timerProgress = session.MusicBeatTimer / mainController.beatIncrement;
            // Progress in the current bar
            float barProgress = ((session.CassetteWonkyBeatIndex + timerProgress) / (mainController.barLength * 16 / (float) mainController.beatLength)) % 1;
            float accurateBeatIndex = barProgress * this.maxBeats;

            this.CassetteWonkyBeatIndex = (int) accurateBeatIndex;

            // Timer has to be offset by the beat increment delta to account for different start of the next bar 
            // This is because the index is the index of the next played note, not the current one
            beatDelta = this.beatIncrement - mainController.beatIncrement;

            this.CassetteBeatTimer = (accurateBeatIndex - this.CassetteWonkyBeatIndex) * this.beatIncrement + beatDelta - mainController.cassetteOffset;
        }

        // Called by main controller
        public void AdvanceMusic(float time, Scene scene) {
            this.CassetteBeatTimer += time;

            if (this.CassetteBeatTimer >= beatIncrement) {

                this.CassetteBeatTimer -= beatIncrement;

                // beatIndex is always in sixteenth notes
                var wonkyBlocks = scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>();
                int nextBeatIndex = (this.CassetteWonkyBeatIndex + 1) % maxBeats;
                int beatInBar = this.CassetteWonkyBeatIndex / (16 / beatLength) % barLength; // current beat

                int nextBeatInBar = nextBeatIndex / (16 / beatLength) % barLength; // next beat
                bool beatIncrementsNext = (nextBeatIndex / (float) (16 / beatLength)) % 1 == 0; // will the next beatIndex be the start of a new beat

                foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks) {
                    if (wonkyBlock.ControllerIndex != this.ControllerIndex)
                        continue;

                    wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);

                    if (wonkyBlock.OnAtBeats.Contains(nextBeatInBar) != wonkyBlock.Activated && beatIncrementsNext) {
                        wonkyBlock.WillToggle();
                    }
                }

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                this.CassetteWonkyBeatIndex = (this.CassetteWonkyBeatIndex + 1) % maxBeats;
            }
        }
    }
}
