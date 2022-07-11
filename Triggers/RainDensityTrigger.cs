using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/RainDensityTrigger")]
    public class RainDensityTrigger : Trigger {
        public class Data {
            public float Density;
            public float StartDensity;
            public float EndDensity;
            public float Duration;
        }

        private static Data SessionData => StrawberryJam2021Module.Session.RainDensityData;

        private float triggerEndDensity;
        private float triggerDuration;

        public RainDensityTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            triggerEndDensity = Calc.Clamp(data.Float("density", 0f), 0f, 1f);
            triggerDuration = Math.Max(data.Float("duration", 0f), 0.01f);
        }

        public static void Load() {
            On.Celeste.RainFG.Update += modRainUpdate;
            IL.Celeste.RainFG.Render += modRainRender;
        }

        public static void Unload() {
            On.Celeste.RainFG.Update -= modRainUpdate;
            IL.Celeste.RainFG.Render -= modRainRender;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            SessionData.StartDensity = SessionData.Density;
            SessionData.EndDensity = triggerEndDensity;
            SessionData.Duration = triggerDuration;
        }

        // multiply RainFG.particles.Length in for loop by Density in order to adjust number of particles rendered
        private static void modRainRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchConvI4())) {
                cursor.EmitDelegate<Func<int, int>>(len => (int) (len * SessionData.Density));
            }
        }

        private static void modRainUpdate(On.Celeste.RainFG.orig_Update orig, RainFG self, Scene scene) {
            orig(self, scene);

            if (SessionData.Density != SessionData.EndDensity) {
                float rate = Math.Abs(SessionData.EndDensity - SessionData.StartDensity) / SessionData.Duration;
                SessionData.Density = Calc.Approach(SessionData.Density, SessionData.EndDensity, rate * Engine.DeltaTime);
            }
        }
    }
}
