using Celeste.Mod.StrawberryJam2021.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Module : EverestModule {

        public static StrawberryJam2021Module Instance;


        public StrawberryJam2021Module() {
            Instance = this;
        }

        public static SpriteBank SpriteBank => Instance._CustomEntitySpriteBank;
        private SpriteBank _CustomEntitySpriteBank;


        public override void Load() {
            SelfUpdater.Load();
            AntiGravJelly.Load();
            BubbleCollider.Load();
            ExplodingStrawberry.Load();
            CrystalBombBadelineBoss.Load();
            MaskedDecal.Load();
            WormholeBooster.Load();
        }




        public override void Unload() {
            SelfUpdater.Unload();
            AntiGravJelly.Unload();
            BubbleCollider.Unload();
            ExplodingStrawberry.Unload();
            CrystalBombBadelineBoss.Unload();
            MaskedDecal.Unload();
            WormholeBooster.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/StrawberryJam2021/CustomEntitySprites.xml");
            WormholeBooster.P_Teleporting = new ParticleType {
                Source = GFX.Game["particles/blob"],
                Color = Calc.HexToColor("8100C1") * 0.2f,
                Color2 = Calc.HexToColor("7800bd") * 0.2f,
                ColorMode = ParticleType.ColorModes.Choose,
                RotationMode = ParticleType.RotationModes.SameAsDirection,
                Size = 0.7f,
                SizeRange = 0.2f,
                DirectionRange = (float) Math.PI / 12f,
                FadeMode = ParticleType.FadeModes.Late,
                LifeMax = 0.2f,
                SpeedMin = 70f,
                SpeedMax = 100f,
                SpeedMultiplier = 1f,
                Acceleration = new Vector2(0f, 10f)
            };
            WormholeBooster.P_WBurst = new ParticleType(Booster.P_Burst);
            WormholeBooster.P_WBurst.Color = Calc.HexToColor("7800bd");
            WormholeBooster.P_WAppear = new ParticleType(Booster.P_Appear) {
                Color = Calc.HexToColor("8100C1")
            };
        }

    }
}
