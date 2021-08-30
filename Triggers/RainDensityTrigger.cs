using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/RainDensityTrigger")]
    class RainDensityTrigger : Trigger {
        private static float Density = 1f;
        private static float StartDensity = 1f;
        private static float EndDensity = 1f;
        private static float Duration;
        
        private float triggerEndDensity;
        private float triggerDuration;
        
        
        public RainDensityTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            triggerEndDensity = Math.Max(Math.Min(data.Float("density", 0f), 1f), 0f);
            triggerDuration = Math.Max(data.Float("duration", 0f), 0f);
        }
        
        public static void Load() {
            On.Celeste.RainFG.Update += modRainUpdate;
            IL.Celeste.RainFG.Render += modRainRender;
            Everest.Events.Level.OnExit += Reset;
        }
        
        public static void Unload() {
            On.Celeste.RainFG.Update -= modRainUpdate;
            IL.Celeste.RainFG.Render -= modRainRender;
            Everest.Events.Level.OnExit -= Reset;
        } 
        
        public override void OnEnter(Player player) {
            base.OnEnter(player);
            
            StartDensity = Density;
            EndDensity = triggerEndDensity;
            Duration = triggerDuration;
        }
        
        // multiply RainFG.particles.Length in for loop by Density in order to adjust number of particles rendered
        private static void modRainRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchConvI4())) {
                Logger.Log("SJ2021/RainDensityTrigger", $"Adding IL hook at {cursor.Index} to be able to customize rain density");
                cursor.EmitDelegate<Func<int, int>>(len => (int)(len * Density));
            }
        }
        
        private static void modRainUpdate(On.Celeste.RainFG.orig_Update orig, RainFG self, Scene scene) {
            orig(self, scene);
            
            if (Density != EndDensity) {
                float rate = Math.Abs(EndDensity - StartDensity) / Duration;
                Density = Calc.Approach(Density, EndDensity, rate * Engine.DeltaTime);
            }
        }
        
        private static void Reset(Level a, LevelExit b, LevelExit.Mode c, Session d, HiresSnow e) {
            Density = 1f;
            StartDensity = 1f;
            EndDensity = 1f;
        }
    }
}