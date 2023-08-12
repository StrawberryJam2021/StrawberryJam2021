using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Triggers
{
    [CustomEntity("StrawberryJam2021/liftBoostTrigger")]
    [Tracked(true)]
    public class LiftBoostTrigger : Trigger
    {
        private static IDetour hook_Actor_set_LiftSpeed;

        public LiftBoostTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
        {
            

        }
        public static void Load()
        {
            hook_Actor_set_LiftSpeed = new Hook(
            typeof(Actor).GetProperty("LiftSpeed", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
            typeof(LiftBoostTrigger).GetMethod("set_LiftSpeed", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public static void Unload() {
            hook_Actor_set_LiftSpeed?.Dispose();
        }
        private delegate void orig_set_LiftSpeed(Actor self, Vector2 value);
        private static void set_LiftSpeed(orig_set_LiftSpeed orig,Actor self,Vector2 value)
            {
            Vector2 currentLiftSpeed;
            var test = self.LiftSpeed;
            LiftBoostTrigger boostTrigger = self.CollideFirst<LiftBoostTrigger>();
            DynData<Actor> actorData = new DynData<Actor>(self);
            if (boostTrigger != null)
            {
                
                Vector2 lastLiftSpeed = actorData.Get<Vector2>("lastLiftSpeed");
                currentLiftSpeed = value;
                if (value != Vector2.Zero && self.LiftSpeedGraceTime > 0f)
                {
                    if (value.X == 0f && lastLiftSpeed.X != 0)
                    {
                        currentLiftSpeed.X = lastLiftSpeed.X;
                    }
                    if (value.Y == 0f && lastLiftSpeed.Y != 0)
                    {
                        currentLiftSpeed.Y = lastLiftSpeed.Y;
                    }
                    actorData.Set("lastLiftSpeed", currentLiftSpeed);
                    actorData.Set("currentLiftSpeed", currentLiftSpeed);
                    actorData.Set("liftSpeedTimer", self.LiftSpeedGraceTime);
                }
                
                

            }
            else
            {
                orig(self,value);
            }
        }
    }
}