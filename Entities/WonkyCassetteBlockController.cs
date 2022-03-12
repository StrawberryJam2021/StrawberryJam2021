using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WonkyCassetteBlockController")]
    [Tracked]
    public class WonkyCassetteBlockController : Entity {

        // Music stuff
        public readonly int bpm;
        public readonly int bars;
        public readonly int barLength; // The top number in the time signature
        public readonly int beatLength; // The bottom number in the time signature
        private readonly float cassetteOffset;
        private readonly string param;

        public readonly int ExtraBoostFrames;

        private readonly string DisableFlag;

        private float beatIncrement;
        private int maxBeats;

        private bool isLevelMusic;
        private EventInstance sfx;
        private EventInstance snapshot;

        private bool transitioningIn = false;

        public WonkyCassetteBlockController(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int("bpm"), data.Int("bars"), data.Attr("timeSignature"), data.Attr("sixteenthNoteParam", "sixteenth_note"), data.Float("cassetteOffset"), data.Int("boostFrames", 1), data.Attr("disableFlag")) { }

        public WonkyCassetteBlockController(Vector2 position, int bpm, int bars, string timeSignature, string param, float cassetteOffset, int boostFrames, string disableFlag)
            : base(position) {
            Tag = Tags.FrozenUpdate | Tags.TransitionUpdate;

            Add(new TransitionListener() {
                OnInBegin = () => transitioningIn = true,
                OnInEnd = () => transitioningIn = false,
            });

            this.bpm = bpm;
            this.bars = bars;
            this.param = param;
            this.cassetteOffset = cassetteOffset;

            GroupCollection timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;
            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            barLength = int.Parse(timeSignatureParsed[1].Value);
            beatLength = int.Parse(timeSignatureParsed[2].Value);

            if (boostFrames < 1)
                throw new ArgumentException($"Boost Frames must be 1 or greater, but is set to {boostFrames}.");

            ExtraBoostFrames = boostFrames - 1;

            this.DisableFlag = disableFlag;
        }

        public void DisableAndReset(Scene scene, StrawberryJam2021Session session) {
            session.MusicBeatTimer = 0;
            session.MusicWonkyBeatIndex = 0;

            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;
            session.CassetteWonkyBeatIndex = 0;
            
            var wonkyBlocks = scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>();

            foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks) {
                wonkyBlock.Activated = false;
            }

            var minorControllers = scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>();

            foreach (WonkyMinorCassetteBlockController minorController in minorControllers) {
                minorController.Reset(scene, session);
            }

            session.CassetteBlocksDisabled = true;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (Scene.Tracker.GetEntity<CassetteBlockManager>() is not null)
                throw new Exception("WonkyCassetteBlockController detected in same room as ManualCassetteController!");

            isLevelMusic = AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong == "-";

            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;
            else
                snapshot = Audio.CreateSnapshot("snapshot:/music_mains_mute");

            StrawberryJam2021Session session = StrawberryJam2021Module.Session;

            // We always want sixteenth notes here, regardless of time signature
            beatIncrement = (float) (60.0 / bpm * beatLength / 16.0);
            maxBeats = 16 * bars * barLength / beatLength;

            session.MusicWonkyBeatIndex = session.MusicWonkyBeatIndex % maxBeats;

            // Synchronize the beat indices.
            // This may leave cassette blocks activated or deactivated for up to
            // the duration of an offset longer than normal at the start, but
            // that will fix itself within one beatIncrement duration
            session.CassetteWonkyBeatIndex = session.MusicWonkyBeatIndex;

            // Re-synchronize the beat timers
            // Positive offsets will make the cassette blocks lag behind the music progress
            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;

            // Reset timers on song change
            if (session.CassetteBlocksLastParameter != param) {
                DisableAndReset(scene, session);
                session.CassetteBlocksLastParameter = param;
            }

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in Scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>()) {
                minorController.MinorAwake(scene, session, this);
            }
        }

        public void CheckDisableAndReset() {
            StrawberryJam2021Session session = StrawberryJam2021Module.Session;

            if (DisableFlag.Length == 0) {
                if (session.CassetteBlocksDisabled)
                    session.CassetteBlocksDisabled = false;
                return;
            }

            Level level = SceneAs<Level>();
            bool shouldDisable = level.Session.GetFlag(DisableFlag);

            if (!session.CassetteBlocksDisabled && shouldDisable) {
                DisableAndReset(level, session);

            } else if (session.CassetteBlocksDisabled && !shouldDisable) {
                session.CassetteBlocksDisabled = false;
            }
        }

        private void AdvanceMusic(float time, Scene scene, StrawberryJam2021Session session) {
            CheckDisableAndReset();

            if (session.CassetteBlocksDisabled)
                return;

            session.CassetteBeatTimer += time;

            if (session.CassetteBeatTimer >= beatIncrement) {

                session.CassetteBeatTimer -= beatIncrement;

                // beatIndex is always in sixteenth notes
                var wonkyBlocks = scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>();
                int nextBeatIndex = (session.CassetteWonkyBeatIndex + 1) % maxBeats;
                int beatInBar = session.CassetteWonkyBeatIndex / (16 / beatLength) % barLength; // current beat

                int nextBeatInBar = nextBeatIndex / (16 / beatLength) % barLength; // next beat
                bool beatIncrementsNext = (nextBeatIndex / (float) (16 / beatLength)) % 1 == 0; // will the next beatIndex be the start of a new beat

                foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks) {
                    if (wonkyBlock.ControllerIndex != 0)
                        continue;

                    wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);

                    if (wonkyBlock.OnAtBeats.Contains(nextBeatInBar) != wonkyBlock.Activated && beatIncrementsNext) {
                        wonkyBlock.WillToggle();
                    }
                }
                
                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.CassetteWonkyBeatIndex = (session.CassetteWonkyBeatIndex + 1) % maxBeats;
            }

            session.MusicBeatTimer += time;

            if (session.MusicBeatTimer >= beatIncrement) {

                session.MusicBeatTimer -= beatIncrement;

                sfx?.setParameterValue(param, (session.MusicWonkyBeatIndex * beatLength / 16) + 1);

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.MusicWonkyBeatIndex = (session.MusicWonkyBeatIndex + 1) % maxBeats;
            }

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in Scene.Tracker.GetEntities<WonkyMinorCassetteBlockController>()) {
                minorController.AdvanceMusic(time, scene);
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (!isLevelMusic) {
                Audio.Stop(snapshot);
                Audio.Stop(sfx);
            }
        }

        public override void Update() {
            base.Update();

            if (transitioningIn)
                return;

            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;

            if (!isLevelMusic && sfx == null) {
                sfx = Audio.CreateInstance(AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong);
                Audio.Play("event:/game/general/cassette_block_switch_2");
                sfx.start();
            } else {
                AdvanceMusic(Engine.DeltaTime, Scene, StrawberryJam2021Module.Session);
            }
        }

        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Monocle.Engine.Update += Engine_Update;
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Monocle.Engine.Update -= Engine_Update;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            StrawberryJam2021Session session = StrawberryJam2021Module.Session;
            WonkyCassetteBlockController mainController = self.Tracker.GetEntity<WonkyCassetteBlockController>();
            var minorControllers = self.Tracker.GetEntities<WonkyMinorCassetteBlockController>();

            foreach (WonkyCassetteBlock wonkyBlock in self.Tracker.GetEntities<WonkyCassetteBlock>()) {
                if (wonkyBlock.ControllerIndex == 0) {
                    wonkyBlock.SetActivatedSilently(mainController != null && !session.CassetteBlocksDisabled && wonkyBlock.OnAtBeats.Contains(session.CassetteWonkyBeatIndex / (16 / mainController.beatLength) % mainController.barLength));
                } else {
                    foreach (WonkyMinorCassetteBlockController minorController in minorControllers) {
                        if (wonkyBlock.ControllerIndex == minorController.ControllerIndex) {
                            wonkyBlock.SetActivatedSilently(minorController != null && !session.CassetteBlocksDisabled && wonkyBlock.OnAtBeats.Contains(minorController.CassetteWonkyBeatIndex / (16 / minorController.beatLength) % minorController.barLength));
                        }
                    }
                }
            }
        }

        private static void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gametime) {
            float oldFreezeTimer = Engine.FreezeTimer;

            orig(self, gametime);

            if (!Engine.DashAssistFreeze && oldFreezeTimer > 0f) {
                Engine.Scene.Tracker.GetEntity<WonkyCassetteBlockController>()?.AdvanceMusic(Engine.DeltaTime, Engine.Scene, StrawberryJam2021Module.Session);
                Engine.Scene.Tracker.GetEntities<WonkyCassetteBlock>().ForEach(block => block.Update());
            }
        }
    }
}
