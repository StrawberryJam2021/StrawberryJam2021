﻿using Celeste.Mod.StrawberryJam2021.Entities;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;
        
        public StrawberryJam2021Module() {
            Instance = this;
        }

        public override void Load() {
            SelfUpdater.Load();
            AntiGravJelly.Load();
        }

        public override void Unload() {
            SelfUpdater.Unload();
            AntiGravJelly.Unload();
        }

    }
}
