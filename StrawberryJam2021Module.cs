using Microsoft.Xna.Framework;
using System.Collections;

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
            On.Celeste.Player.BoostCoroutine += increaseDelay;
        }

        private IEnumerator increaseDelay(On.Celeste.Player.orig_BoostCoroutine orig, Player self) {
            if (!Entities.WormholeBooster.TeleportingDNI) {
                IEnumerator original = orig(self);
                while (original.MoveNext())
                    yield return original.Current;
            } else {
                yield return 0.65f;
                if (Entities.WormholeBooster.TeleDeath) {
                    self.Die(Vector2.Zero);
                    Entities.WormholeBooster.TeleDeath = false;
                    Entities.WormholeBooster.TeleportingDNI = false;
                } else {
                    self.StateMachine.State = 2;
                }
            }
        }

        private void readyAT(On.Celeste.Player.orig_BoostEnd orig, Player self) {
            Entities.WormholeBooster.TDLock = true;
            if (Entities.WormholeBooster.TeleDeath) {
                self.Die(Vector2.Zero);
                Entities.WormholeBooster.TeleDeath = false;
                Entities.WormholeBooster.TeleportingDNI = false;
            }
        }

        private void allowTeleport(On.Celeste.Player.orig_DashEnd orig, Player self) {
            if (Entities.WormholeBooster.TeleportingDNI && Entities.WormholeBooster.TDLock) {
                Entities.WormholeBooster.TeleportingDNI = false;
                Entities.WormholeBooster.TDLock = false;

            }
            if (self.SceneAs<Level>().Tracker.CountEntities<Entities.WormholeBooster>() < 2) {
                Entities.WormholeBooster.TeleDeath = true;
            }
            orig(self);
        }

        public override void Unload() {
            SelfUpdater.Unload();
            On.Celeste.Player.DashEnd -= allowTeleport;
            On.Celeste.Player.BoostEnd -= readyAT;
        }

    }
}
