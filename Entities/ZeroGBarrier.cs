using System.Collections.Generic;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using Celeste.Mod.StrawberryJam2021.Triggers;

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
            On.Celeste.Player.ClimbHop -= Player_ClimbHop;
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

        internal static Vector2[] directionSet = new Vector2[4] { new Vector2(-1, 0), Vector2.UnitY, Vector2.UnitX, new Vector2(0, -1) };

        public EntityData data;
        public Vector2 offset;
        public FourWayDirection direction;
        private List<Vector2> zeroGParticles;
        private Vector2 region;

        public ZeroGBarrier(EntityData data, Vector2 offset) : base(data, offset) {
            this.data = data;
            this.offset = offset;
            direction = data.Enum<FourWayDirection>("direction", FourWayDirection.Right);
            particles.Clear();
            zeroGParticles = new List<Vector2>();
            region = new Vector2(base.Width - 1f, base.Height - 1f);
            for (int i = 0; (float) i < base.Width * base.Height / 16f; i++) {
                zeroGParticles.Add(new Vector2(Calc.Random.NextFloat(base.Width - 1f), Calc.Random.NextFloat(base.Height - 1f)));
            }

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            scene.Add(new ZeroGTrigger(data, offset, direction));
        }
        

        public override void Update() {
            base.Update();
            int count = zeroGParticles.Count;
            for (int i = 0; i < count; i++) {
                Vector2 value = zeroGParticles[i] + directionSet[(int)direction] * speeds[i % 3] * Engine.DeltaTime; //speeds.Length is constant here so it's fine to leave this.
                value = modVec2(value, region);
                zeroGParticles[i] = value;
            }
        }

        public override void Render() {
            Color color = Color.White * 0.5f;
            foreach (Vector2 particle in zeroGParticles) {
                Draw.Pixel.Draw(Position + particle, Vector2.Zero, color);
            }
            if (Flashing) {
                Draw.Rect(base.Collider, Color.White * Flash * 0.5f);
            }
        }

        internal Vector2 modVec2(Vector2 v, Vector2 m) {
            return new Vector2(mod(v.X, m.X), mod(v.Y, m.Y));
        }

        internal float mod(float x, float m) => (x % m + m) % m;
    }
}
