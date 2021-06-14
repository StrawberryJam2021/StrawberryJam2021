using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/WonkyCassetteBlockController")]
    [Tracked]
    public class WonkyCassetteBlockController : Entity {

        // Music stuff
        private readonly int bpm;
        private readonly int bars;  
        private readonly int barLength; // The top number in the time signature
        private readonly int beatLength; // The bottom number in the time signature
        private readonly string param;

        private bool isLevelMusic;
        private EventInstance sfx;
        private EventInstance snapshot;

        public WonkyCassetteBlockController(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int("bpm"), data.Int("bars"), data.Attr("timeSignature"), data.Attr("sixteenthNoteParam", "sixteenth_note")) { }

        public WonkyCassetteBlockController(Vector2 position, int bpm, int bars, string timeSignature, string param)
            : base(position) {
            this.bpm = bpm;
            this.bars = bars;
            this.param = param;

            GroupCollection timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;
            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            barLength = int.Parse(timeSignatureParsed[1].Value);
            beatLength = int.Parse(timeSignatureParsed[2].Value);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (Scene.Tracker.GetEntity<CassetteBlockManager>() is not null)
                throw new Exception("WonkyCassetteBlockController detected in same room as ManualCassetteController!");

            isLevelMusic = AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong == "-";

            if (!isLevelMusic)
                snapshot = Audio.CreateSnapshot("snapshot:/music_mains_mute");

            StrawberryJam2021Session session = StrawberryJam2021Module.Session;

            // We always want sixteenth notes here, regardless of time signature
            session.WonkyBeatIncrement = (float) (60.0 / bpm * beatLength / 16.0);
            session.WonkyMaxBeats = 16 * bars * barLength / beatLength;
            session.WonkyBeatIndex = session.WonkyBeatIndex % session.WonkyMaxBeats;
            session.WonkyBpm = bpm;
            session.WonkyBars = bars;
            session.WonkyBarLength = barLength;
            session.WonkyBeatLength = beatLength;
            session.WonkyParam = param;
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

            if (isLevelMusic)
                sfx = Audio.CurrentMusicEventInstance;
            if (!isLevelMusic && sfx == null) {
                sfx = Audio.CreateInstance(AreaData.Areas[SceneAs<Level>().Session.Area.ID].CassetteSong);
                Audio.Play("event:/game/general/cassette_block_switch_2");
                sfx.start();
            } else {
                StrawberryJam2021Module.Session.AdvanceMusic(Engine.DeltaTime, Scene, sfx);
            }
        }

        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            StrawberryJam2021Session session = StrawberryJam2021Module.Session;
            foreach (WonkyCassetteBlock wonkyBlock in self.Tracker.GetEntities<WonkyCassetteBlock>()) {
                wonkyBlock.SetActivatedSilently(wonkyBlock.OnAtBeats.Contains(session.WonkyBeatIndex / (16 / session.WonkyBeatLength) % session.WonkyBarLength));
            }
        }
    }
}
