using Monocle;
using Celeste.Mod.StrawberryJam2021.Entities;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;

        public static SpriteBank SpriteBank => Instance._CustomEntitySpriteBank;
        private SpriteBank _CustomEntitySpriteBank;
        
        public StrawberryJam2021Module() {
            Instance = this;
        }

        public override void Load() {
            SelfUpdater.Load();
            AntiGravJelly.Load();
            BubbleCollider.Load();
            ExplodingStrawberry.Load();
            MaskedDecal.Load();
        }

        public override void Unload() {
            SelfUpdater.Unload();
            AntiGravJelly.Unload();
            BubbleCollider.Unload();
            ExplodingStrawberry.Unload();
            MaskedDecal.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/StrawberryJam2021/CustomEntitySprites.xml");
        }

    }
}
