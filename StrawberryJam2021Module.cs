using Celeste.Mod.StrawberryJam2021.Entities;
using Celeste.Mod.StrawberryJam2021.Triggers;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;

        public override Type SaveDataType => typeof(StrawberryJam2021SaveData);
        public static StrawberryJam2021SaveData SaveData => (StrawberryJam2021SaveData) Instance._SaveData;

        public override Type SessionType => typeof(StrawberryJam2021Session);
        public static StrawberryJam2021Session Session => (StrawberryJam2021Session) Instance._Session;

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
            HorizontalTempleGate.Load();
            ToggleSwapBlock.Load();
            WonkyCassetteBlock.Load();
            WonkyCassetteBlockController.Load();
            SpeedPreservePuffer.Load();
            ResizableDashSwitch.Load();
            SkateboardTrigger.Load();
            LaserEmitter.Load();
            OshiroAttackTimeTrigger.Load();
            CassetteBadelineBlock.Load();
            ConstantDelayFallingBlockController.Load();
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
            HorizontalTempleGate.Unload();
            ToggleSwapBlock.Unload();
            WonkyCassetteBlock.Unload();
            WonkyCassetteBlockController.Unload();
            SpeedPreservePuffer.Unload();
            ResizableDashSwitch.Unload();
            SkateboardTrigger.Unload();
            LaserEmitter.Unload();
            OshiroAttackTimeTrigger.Unload();
            CassetteBadelineBlock.Unload();
            ConstantDelayFallingBlockController.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/StrawberryJam2021/CustomEntitySprites.xml");

            WormholeBooster.LoadParticles();
            SwitchCrate.LoadTypes();
            SwitchCrateHolder.SetupParticles();
            LoopBlock.InitializeTextures();
            DashBoostField.LoadParticles();
            ResizableDashSwitch.LoadParticles();
            SkateboardTrigger.InitializeTextures();
            PocketUmbrella.LoadParticles();
            Paintbrush.LoadParticles();
        }
    }
}
