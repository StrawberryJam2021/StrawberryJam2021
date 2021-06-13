using Celeste.Mod.Entities;
using FMOD.Studio;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// WonkyCassetteBlockManager is similar to CassetteBlockManager in its
    /// operation, but with a few key differences that are very important.
    ///
    /// The way vanilla CassetteBlockManager operates is by keeping a list of
    /// which types of cassette blocks (e.g.: red, blue) are present in a given
    /// Level, and linearly transitioning through each type that is present in
    /// a predefined order.
    ///
    /// By contrast, WonkyCassetteBlockManager operates on a per-block basis,
    /// letting each block configure its own settings as to which beats it
    /// wants to move on. In doing this, it eschews the MaxBeat and
    /// CassetteBlockTempo globals and the various settings of the
    /// CassetteModifier, which are relics of the vanilla system. To determine
    /// the time signature, and the maximum number of beats, it reads the
    /// settings of the WonkyCassetteBlocks in the given Level (during Awake,
    /// to guarantee that they have all been loaded).
    [CustomEntity("SJ2021/WonkyCassetteBlockManager")]
    [Tracked]
    public class WonkyCassetteBlockManager : Entity {
        private int bpm;

        private int bars;

        // The top number in the time signature
        private int barLength;

        // The bottom number in the time signature
        private int beatLength;

        private float beatIncrement;

        private bool isLevelMusic;
        private EventInstance sfx;
        private EventInstance snapshot;
        private float beatTimer;
        private int maxBeats;
        private int beatIndex;

        public WonkyCassetteBlockManager() {
            Tag = Tags.Global;
            // Add(new TransitionListener {
            //     OnOutBegin = () => {
            //         SceneAs<Level>().Entities.UpdateLists();
            //         // TODO: this is broken
            //         if (SceneAs<Level>().Tracker.GetEntities<WonkyCassetteBlock>().Count == 0) {
            //             RemoveSelf();
            //         }
            //     }
            // });
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            var wonkyBlocks = scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>().ToList();

            bpm = wonkyBlocks[0].BPM;
            bars = wonkyBlocks[0].Bars;
            barLength = wonkyBlocks[0].BarLength;
            beatLength = wonkyBlocks[0].BeatLength;

            // We always want sixteenth notes here, regardless of time signature
            beatIncrement = (float) (60.0 / bpm / 4.0);
            maxBeats = 16 * bars * barLength / beatLength;

            if (wonkyBlocks.Skip(1).Any(wonkyBlock =>
                wonkyBlock.BPM != bpm || wonkyBlock.Bars != bars || wonkyBlock.BarLength != barLength || wonkyBlock.BeatLength != beatLength)) {
                throw new ArgumentException("Inconsistent parameters between multiple WonkyCassetteBlocks in the same Level.");
            }

            isLevelMusic = AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong == "-";

            if (!isLevelMusic)
                snapshot = Audio.CreateSnapshot("snapshot:/music_mains_mute");
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (!isLevelMusic) {
                Audio.Stop(snapshot);
                Audio.Stop(sfx);
            }
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            if (!isLevelMusic) {
                Audio.Stop(snapshot);
                Audio.Stop(sfx);
            }
        }

        public override void Update() {
            base.Update();
            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;
            if (!isLevelMusic && sfx == null) {
                sfx = Audio.CreateInstance(AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong);
                Audio.Play("event:/game/general/cassette_block_switch_2");
                sfx.start();
            } else {
                AdvanceMusic(Engine.DeltaTime);
            }
        }

        private void AdvanceMusic(float time) {
            beatTimer += time;

            if (beatTimer < beatIncrement)
                return;

            beatTimer -= beatIncrement;

            // beatIndex is always in sixteenth notes
            var wonkyBlocks = Scene.Tracker.GetEntities<WonkyCassetteBlock>().Cast<WonkyCassetteBlock>().ToList();
            foreach (var wonkyBlock in wonkyBlocks) {
                int beatInBar = beatIndex / (16 / beatLength) % barLength;

                wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);

                // if (wonkyBlock.MoveOn.Select(b => (b + barLength - 1) % barLength).Contains(beatInBar) || wonkyBlock.Activated)
                //     wonkyBlock.WillToggle();
            }

            sfx.setParameterValue("78_eighth_note", (beatIndex * beatLength / 16) + 1);

            // Doing this here because it would go to the next beat with a sixteenth note offset at start
            beatIndex = (beatIndex + 1) % maxBeats;
        }

        private void OnLevelStart() {
            foreach (var wonkyBlock in Scene.Tracker.GetEntities<WonkyCassetteBlock>()
                .Cast<WonkyCassetteBlock>()
                .Where(wonkyBlock => wonkyBlock.ID.Level == SceneAs<Level>().Session.Level)) {
                //wonkyBlock.SetActivatedSilently(false);
            }
        }

        private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            self.Tracker.GetEntity<WonkyCassetteBlockManager>()?.OnLevelStart();
        }

        public static void Load() {
            On.Celeste.Level.LoadLevel += OnLoadLevel;
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= OnLoadLevel;
        }
    }
}
