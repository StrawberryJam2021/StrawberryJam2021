using System;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;
        
        public StrawberryJam2021Module() {
            Instance = this;
        }

        public override void Load() {
            SelfUpdater.Load();
            On.Celeste.Player.DashEnd += allowTeleport;
            On.Celeste.Player.BoostEnd += readyAT;
        }

        private void readyAT(On.Celeste.Player.orig_BoostEnd orig, Player self) {
            Entities.WormholeBooster.TDLock = true;
        }

        private void allowTeleport(On.Celeste.Player.orig_DashEnd orig, Player self) {
            if (Entities.WormholeBooster.TeleportingDNI && Entities.WormholeBooster.TDLock) {
                Entities.WormholeBooster.TeleportingDNI = false;
                Entities.WormholeBooster.TDLock = false;
            }
            orig(self);
        }

        public override void Unload() {
            SelfUpdater.Unload();
        }

    }
}
