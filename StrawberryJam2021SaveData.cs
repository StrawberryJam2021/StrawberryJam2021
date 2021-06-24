using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021SaveData : EverestModuleSaveData {
        public HashSet<string> FilledJamJarSIDs { get; set; } = new HashSet<string>();
    }
}
