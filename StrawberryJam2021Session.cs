using Celeste.Mod.StrawberryJam2021.Entities;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Session : EverestModuleSession {
        public int MusicWonkyBeatIndex;
        public int CassetteWonkyBeatIndex;
        public float MusicBeatTimer;
        public float CassetteBeatTimer;
        public bool CassetteBlocksDisabled = true;
        public string CassetteBlocksLastParameter = "";

        public bool OshiroBSideMode = false;

        public bool SkateboardEnabled = false;

        public DashSequenceDisplay DashSequenceDisplay;

    }
}
