using Celeste.Mod.StrawberryJam2021.Entities;
using Celeste.Mod.StrawberryJam2021.Triggers;

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
        public bool ZeroG = false;
        public DashSequenceDisplay DashSequenceDisplay;

        public RainDensityTrigger.Data RainDensityData = new RainDensityTrigger.Data { Density = 1f, StartDensity = 1f, EndDensity = 1f };
    }
}
