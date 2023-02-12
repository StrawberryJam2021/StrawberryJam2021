using Celeste.Mod.StrawberryJam2021.Entities;
using Celeste.Mod.StrawberryJam2021.Triggers;
using Celeste.Mod.StrawberryJam2021.StylegroundMasks;
using Monocle;
using System;
using Celeste.Mod.StrawberryJam2021.Effects;
using Celeste.Mod.Helpers;
using Celeste.Mod.StrawberryJam2021.Cutscenes;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;

        public override Type SettingsType => typeof(StrawberryJam2021Settings);
        public static StrawberryJam2021Settings Settings => (StrawberryJam2021Settings) Instance._Settings;

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
            WonkyCassetteBlock.Load();
            WonkyCassetteBlockController.Load();
            SpeedPreservePuffer.Load();
            ResizableDashSwitch.Load();
            SkateboardTrigger.Load();
            LaserEmitter.Load();
            OshiroAttackTimeTrigger.Load();
            ConstantDelayFallingBlockController.Load();
            DirectionalBooster.Load();
            HintController.Load();
            RainDensityTrigger.Load();
            ZeroGBarrier.Load();
            DarkMatterHooks.Load();
            MaskHooks.Load();
            MaskedOutline.Load();
            DashSequenceDisplay.Load();
            GroupedParallaxDecal.Load();
            ExpiringDashRefill.Load();
            ToggleSwapBlock.Load();
            ShowHitboxTrigger.Load();
            WindTunnelNoParticles.Load();
            LightSourceLimitController.Load();
            TogglePlaybackHandler.Load();
            CassetteListener.Load();
            CrystallineHelperTimeFreezeMusicController.Load();
            SolarElevator.Load();
            SpriteSwapTrigger.Load();
            CS_Credits.Load();
            TasHelper.Load();
            GlowController.Load();

            Everest.Events.Level.OnLoadBackdrop += onLoadBackdrop;
        }

        public override void Unload() {
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
            WonkyCassetteBlock.Unload();
            WonkyCassetteBlockController.Unload();
            SpeedPreservePuffer.Unload();
            ResizableDashSwitch.Unload();
            SkateboardTrigger.Unload();
            LaserEmitter.Unload();
            OshiroAttackTimeTrigger.Unload();
            ConstantDelayFallingBlockController.Unload();
            DirectionalBooster.Unload();
            HintController.Unload();
            RainDensityTrigger.Unload();
            ZeroGBarrier.Unload();
            DarkMatterHooks.Unload();
            MaskHooks.Unload();
            DashSequenceDisplay.Unload();
            GroupedParallaxDecal.Unload();
            ExpiringDashRefill.Unload();
            ToggleSwapBlock.Unload();
            ShowHitboxTrigger.Unload();
            WindTunnelNoParticles.Unload();
            LightSourceLimitController.Unload();
            TogglePlaybackHandler.Unload();
            CassetteListener.Unload();
            CrystallineHelperTimeFreezeMusicController.Unload();
            SolarElevator.Unload();
            SpriteSwapTrigger.Unload();
            CS_Credits.Unload();
            TasHelper.Unload();
            GlowController.Unload();

            Everest.Events.Level.OnLoadBackdrop -= onLoadBackdrop;
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
            PelletEmitter.PelletShot.LoadParticles();
            NodedCloud.LoadParticles();
            LaserEmitter.LoadParticles();
            DarkMatterHooks.LoadContent(firstLoad);
            Utilities.LoadContent();
            MaskedOutline.LoadTexture();
            BeeFireball.LoadContent();
        }

        private Backdrop onLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
            if (child.Name.Equals("SJ2021/HexagonalGodray", StringComparison.OrdinalIgnoreCase)) {
                return new HexagonalGodray(child.Attr("color"), child.Attr("fadeColor"), child.AttrInt("numberOfRays"), child.AttrFloat("speedX"), child.AttrFloat("speedY"), child.AttrFloat("rotation"), child.AttrFloat("rotationRandomness"));
            }
            if (child.Name.Equals("SJ2021/HeartComet", StringComparison.OrdinalIgnoreCase)) {
                return new HeartComet(child.AttrInt("cometX"), child.AttrInt("cometY"));
            }
            if (child.Name.Equals("SJ2021/MeteorShower", StringComparison.OrdinalIgnoreCase)) {
                return new MeteorShower(child.AttrInt("numberOfMeteors"));
            }
            return null;
        }

        //This occurs after all mods get initialized.
        public override void Initialize() {
            base.Initialize();
            // Hacky, but works since these mods are dependencies of SJ
            CrystallineHelperTimeFreezeMusicController.crystallineHelper_TimeCrystal_stopStage =
                FakeAssembly.GetFakeEntryAssembly().GetType("vitmod.TimeCrystal")?.GetField("stopStage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            CS_Credits.lobbyMapControllerType = FakeAssembly.GetFakeEntryAssembly().GetType("Celeste.Mod.XaphanHelper.Controllers.LobbyMapController");
        }

        // Temporary code from vivhelper
        public static bool VivHelperGetFlags(Level l, string[] flags, string and_or) {
            if (l == null)
                return false;
            bool b = and_or == "and";
            if (flags.Length == 1 && flags[0] == "") { return true; }
            foreach (string flag in flags) {
                if (and_or == "or") { b |= flag[0] != '!' ? l.Session.GetFlag(flag) : !l.Session.GetFlag(flag.TrimStart('!')); } else { b &= flag[0] != '!' ? l.Session.GetFlag(flag) : !l.Session.GetFlag(flag.TrimStart('!')); }
            }
            return b;
        }
    }
}
