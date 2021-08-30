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
            triggerEndDensity = data.Float("density", 0f);
            triggerDuration = data.Float("duration", 0f);
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
        
        // replace RainFG.particles.Length with integer determined by Density
        private static void modRainRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdlen());
            cursor.Goto(cursor.Prev, MoveType.Before);
            cursor.Goto(cursor.Prev, MoveType.Before);
            
            cursor.RemoveRange(4);
            
            cursor.EmitDelegate<Func<int>>(determineVisibleParticleNumber);
        }
        
        private static void modRainUpdate(On.Celeste.RainFG.orig_Update orig, RainFG self, Scene scene) {
            orig(self, scene);
            
            if (Density != EndDensity) {
                float rate = Math.Abs(EndDensity - StartDensity) / Duration;
                Density = Calc.Approach(Density, EndDensity, rate * Engine.DeltaTime);
            }
        }
        
        private static int determineVisibleParticleNumber() {
            return (int)Math.Max(Math.Min(240 * Density, 240), 0);
        }
        
        private static void Reset(Level a, LevelExit b, LevelExit.Mode c, Session d, HiresSnow e) {
            Density = 1f;
            StartDensity = 1f;
            EndDensity = 1f;
        }
    }
}