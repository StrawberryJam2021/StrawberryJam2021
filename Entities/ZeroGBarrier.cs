using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using Celeste.Mod.StrawberryJam2021.Triggers;
using System.Collections;
using Mono.Cecil.Cil;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// This implements the Extended Variants Gravity 0, Air Friction -1, and validates a Session variable that works with hooks to say:
    /// if the player is on a ceiling with 0g and 
    /// is ducking, or, doesn't have a dash or a wall to the left or right of them, it will teleport them back to the previous Spawnpoint.
    /// </summary>
    [CustomEntity("SJ2021/ZeroGBarrier")]
    [Tracked]
    public class ZeroGBarrier : SeekerBarrier {
        #region Hooks
        private static int softlockFrames = 0;

        public static void Load() {
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.ClimbHop += Player_ClimbHop;
        }
        public static void Unload() {
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.ClimbHop += Player_ClimbHop;
        }

        private static void Player_ClimbHop(On.Celeste.Player.orig_ClimbHop orig, Player self) {
            if (StrawberryJam2021Module.Session.ZeroG) {
                int i = 0;
                while(self.CollideFirst<Solid>(self.Position + Vector2.UnitX * (float)self.Facing - Vector2.UnitY * i) != null) {
                    i++;
                }
                self.Position -= Vector2.UnitY * i;
            }
            orig(self);
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            if (self.JustRespawned)
                softlockFrames = 0;
            orig(self);
            if (!self.JustRespawned && StrawberryJam2021Module.Session.ZeroG) {
                if(self.Position == self.PreviousPosition && self.Dashes == 0 && (self.Stamina < 20 || !self.CanUnDuck || (!self.ClimbCheck(-1) && !self.ClimbCheck(1)))) {
                    softlockFrames++;
                } else {
                    softlockFrames = 0;
                }
                //I'll eventually figure out a way to render the fade to black better from 360->480 softlockFrames
                //for some reason this runs every other frame????
                if(softlockFrames > 240) {
                    softlockFrames = 0;
                    self.Die(Vector2.Zero, true, false);
                }
            }
        }
        #endregion

        /// <summary>
        /// Equivalent to Vector2 angle pi/2*(value)
        /// </summary>
        public enum FourWayDirection { Right = 0, Up = 1, Left = 2, Down = 3 }

        public EntityData data;
        public Vector2 offset;
        public FourWayDirection direction;

        public ZeroGBarrier(EntityData data, Vector2 offset) : base(data, offset) {
            this.data = data;
            this.offset = offset;
            direction = data.Enum<FourWayDirection>("direction", FourWayDirection.Right);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            scene.Add(new ZeroGTrigger(data, offset, direction));
        }
    }
}
