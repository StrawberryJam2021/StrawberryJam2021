using Monocle;
using Celeste.Mod.StrawberryJam2021.Entities;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;

        // SpriteBanks
        public static SpriteBank GrabTempleGateSpriteBank => Instance._GrabTempleGateSpriteBank;
        public SpriteBank _GrabTempleGateSpriteBank;
        
        public StrawberryJam2021Module() {
            Instance = this;
        }

        public override void Load() {
            SelfUpdater.Load();
            ClassicZoneController.Load();
        }

        public override void Unload() {
            SelfUpdater.Unload();
            ClassicZoneController.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            _GrabTempleGateSpriteBank = new SpriteBank(GFX.Game, "Graphics/StrawberryJam2021/GrabTempleGateSprites.xml");

        }

    }
}
