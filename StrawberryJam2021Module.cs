using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;


        public StrawberryJam2021Module() {
            Instance = this;
        }
        public static SpriteBank SpriteBank;
        public override void LoadContent(bool firstLoad) {
            SpriteBank = new SpriteBank(GFX.Game, "Graphics/SJ2021/CustomSprites.xml");
        }
        public override void Load() {
            SelfUpdater.Load();
            Entities.WormholeBooster.Load();
        }

        


        public override void Unload() {
            SelfUpdater.Unload();
            Entities.WormholeBooster.Unload();
        }

    }
}
