using Celeste.Mod.StrawberryJam2021.Entities;
using Celeste.Mod.StrawberryJam2021.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using System;

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
            SkyLantern.Load();
            BubbleCollider.Load();
            ExplodingStrawberry.Load();
            CrystalBombBadelineBoss.Load();
            MaskedDecal.Load();
            FloatingBubble.Load();
            HoldableDashTrigger.Load();
            WormholeBooster.Load();
            DashCountTrigger.Load();
            DashBoostField.Load();
            FlagDashSwitch.Load();
            PocketUmbrellaController.Load();
            BarrierDashSwitch.Load();
            TripleBoostFlower.Load();
            ResettingRefill.Load();
            SineDustSpinner.Load();
        }

        public override void Unload() {
            SelfUpdater.Unload();
            SkyLantern.Unload();
            BubbleCollider.Unload();
            ExplodingStrawberry.Unload();
            CrystalBombBadelineBoss.Unload();
            MaskedDecal.Unload();
            FloatingBubble.Unload();
            HoldableDashTrigger.Unload();
            WormholeBooster.Unload();
            DashCountTrigger.Unload();
            DashBoostField.Unload();
            FlagDashSwitch.Unload();
            PocketUmbrellaController.Unload();
            BarrierDashSwitch.Unload();
            TripleBoostFlower.Unload();
            ResettingRefill.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/StrawberryJam2021/CustomEntitySprites.xml");

            WormholeBooster.LoadParticles();
            LoopBlock.InitializeTextures();
            DashBoostField.LoadParticles();
        }
    }
}