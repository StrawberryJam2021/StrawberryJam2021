using Celeste.Mod.StrawberryJam2021.Entities;
using FMOD.Studio;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Session : EverestModuleSession {
        public int MusicWonkyBeatIndex;
        public int CassetteWonkyBeatIndex;
        public float MusicBeatTimer;
        public float CassetteBeatTimer;
    }
}
