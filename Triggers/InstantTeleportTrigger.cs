using Celeste;
using Celeste.Mod.Entities;
using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [TrackedAs(typeof(Trigger))]
    [CustomEntity(
        "VivHelperTEMP/BasicInstantTeleportTrigger = Basic",
        "VivHelperTEMP/MainInstantTeleportTrigger = Main",
        "VivHelperTEMP/CustomInstantTeleportTrigger = Custom")]
    public class InstantTeleportTrigger : Trigger
    {
        public enum TransitionType
        {
            None,
            Lightning,
            GlitchEffect,
            ColorFlash
        }
        public Level level;
        public string[] flags;
        public string currentRoom, newRoom;
        public Vector2 currentPos, newPos;
        public float[] vel, cameraOffset;
        public float rot, vel2;
        public TransitionType transition;
        public bool velMod, rotMod;
        public bool dreaming;
        public bool addTriggerOffset;

        //new stuff
        public bool onExit;
        public bool differentSide;

        private bool triggered = false;
        private float timeSlowDown;
        private Color flashColor;
        private float delay;
        private int legacyCamera;
        private bool resetDashes;

        //new stuff
        private int direction = -1;

        private bool doFlash;
        private float flash;
        public static Entity Basic(Level level, LevelData levelData, Vector2 offset, EntityData entityData) => new InstantTeleportTrigger(entityData, offset);
        public static Entity Main(Level level, LevelData levelData, Vector2 offset, EntityData entityData) => new InstantTeleportTrigger(entityData, offset);
        public static Entity Custom(Level level, LevelData levelData, Vector2 offset, EntityData entityData) => new InstantTeleportTrigger(entityData, offset);
        public InstantTeleportTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            newRoom = data.Attr("WarpRoom", currentRoom).Trim();
            newPos = new Vector2(data.Float("newPosX", -1f), data.Float("newPosY", -1f));
            cameraOffset = new float[] { data.Float("CameraOffsetX", 0f), data.Float("CameraOffsetY", 0f) };
            velMod = data.Bool("VelocityModifier", false);
            vel = new float[] { data.Float("ExitVelocityX", velMod ? 1 : 0), data.Float("ExitVelocityY", velMod ? 1 : 0) };
            vel2 = data.Float("ExitVelocityS", -1);
            rotMod = data.Bool("RotationType", false);
            rot = 0 - ((float)Math.PI * data.Float("RotationActor", 0) / 180);
            dreaming = data.Bool("Dreaming", true);
            transition = data.Enum("TransitionType", TransitionType.None);
            flashColor = ColorFix(data.Attr("Color", "White"));
            flags = data.Attr("ZFlagsData", "").Split(',');
            addTriggerOffset = data.Bool("AddTriggerOffset", true);
            timeSlowDown = data.Float("TimeSlowDown", 0f);
            delay = data.Float("TimeBeforeTeleport", 0f);
            legacyCamera = data.Int("CameraType", -1);
            resetDashes = data.Bool("ResetDashes", true);
            onExit = data.Bool("OnExit", false);
            differentSide = data.Bool("DifferentSide", false);
            if (legacyCamera == -1) legacyCamera = 1;
        }

        public static Color ColorFix(string s, float alpha = 1f) {
            PropertyInfo[] prop = typeof(Color).GetProperties();
            if (s == "Transparent")
                return Color.Transparent;
            foreach (PropertyInfo p in prop) { if (s == p.Name) { return ColorCopy((Color) p.GetValue(default(Color)), alpha); } }
            return ColorCopy(Calc.HexToColor(s), alpha);
        }

        public static Color ColorCopy(Color color, float alpha) {
            return new Color(color.R, color.G, color.B, (byte) alpha * 255);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
            currentRoom = level.Session.Level;
            List<LevelData> Levels = level.Session.MapData.Levels;
            bool b = false;
            foreach (LevelData l in Levels) { b |= l.Name == newRoom; }
            if (!b)
            {
                newRoom = currentRoom; newPos = new Vector2(-1, -1); vel = new float[] { velMod ? 1 : 0, velMod ? 1 : 0 }; vel2 = -1; dreaming = true;
                cameraOffset = new float[] { 0, 0 }; transition = TransitionType.None; flashColor = Color.White; rotMod = false; rot = 0f;
            }
        }

        public override void Update()
        {
            base.Update();
            if (doFlash)
            {
                flash = Calc.Approach(flash, 1f, Engine.DeltaTime * 10f);
                if (flash >= 1f)
                {
                    doFlash = false;
                }
            }
            else if (flash > 0f)
            {
                flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 3f);
            }
        }

        private void Transition(TransitionType transition, Player player)
        {
            if (transition == TransitionType.Lightning)
            {
                level.Flash(Color.White);
                level.Shake();
                level.Add(new LightningStrike(new Vector2(player.X + 60f, player.Y - 180), 10, 200f));
                level.Add(new LightningStrike(new Vector2(player.X + 220f, player.Y - 180), 40, 200f, 0.25f));
                Audio.Play("event:/new_content/game/10_farewell/lightning_strike");
            }
            
        }
        

        public override void OnEnter(Player player)
        {
            if (onExit)
            {
                // Calculate entrance direction
                // [2 horizontal directions], [2 vertical directions]
                int[] sides = new int[4];
                // Set priority
                sides[0] = player.Speed.X > 0 ? 1 : -1; //Left
                sides[1] = -sides[0]; //Right
                sides[2] = player.Speed.Y > 0 ? 1 : -1; //Top
                sides[3] = -sides[2]; //Bottom

                Vector2 prevTopRight = player.TopRight - player.Speed; //TopLeft, TopRight, BottomLeft, BottomRight
                Vector2 prevBottomLeft = player.BottomLeft - player.Speed;
                // Check if the segment prevLoc -> curLoc crosses any of the boundaries
                sides[0] *= (this.Left > prevTopRight.X && this.Left <= player.TopRight.X) ? 1 : 0;
                sides[1] *= (this.Right < prevBottomLeft.X && this.Right >= player.BottomLeft.X) ? 1 : 0;
                sides[2] *= (this.Top > prevBottomLeft.Y && this.Top <= player.BottomLeft.Y) ? 1 : 0;
                sides[3] *= (this.Bottom < prevTopRight.Y && this.Bottom >= player.TopRight.Y) ? 1 : 0;
                int maxValue = sides.Max();
                this.direction = Array.IndexOf(sides, maxValue);
            }
            else
            {
                if (delay > 0) Add(new Coroutine(OnEnterSequence(player)));
                else if (!triggered && StrawberryJam2021Module.VivHelperGetFlags(level, flags, "and") && player != null && !player.Dead) Teleport(player);
            }
            
        }

        public override void OnLeave(Player player)
        {
            if (onExit)
            {
                bool trigger = true;
                if (differentSide)
                {
                    // Calculate exit direction
                    // [2 horizontal directions], [2 vertical directions]
                    int[] sides = new int[4];
                    // Set priority
                    sides[0] = player.Speed.X > 0 ? -1 : 1; //Left
                    sides[1] = -sides[0]; //Right
                    sides[2] = player.Speed.Y > 0 ? -1 : 1; //Top
                    sides[3] = -sides[2]; //Bottom

                    Vector2 prevTopRight = player.TopRight - player.Speed; //TopLeft, TopRight, BottomLeft, BottomRight
                    Vector2 prevBottomLeft = player.BottomLeft - player.Speed;
                    // Check if the segment prevLoc -> curLoc crosses any of the boundaries
                    sides[0] *= (this.Left < prevTopRight.X && this.Left >= player.TopRight.X) ? 1 : 0;
                    sides[1] *= (this.Right > prevBottomLeft.X && this.Right <= player.BottomLeft.X) ? 1 : 0;
                    sides[2] *= (this.Top < prevBottomLeft.Y && this.Top >= player.BottomLeft.Y) ? 1 : 0;
                    sides[3] *= (this.Bottom > prevTopRight.Y && this.Bottom <= player.TopRight.Y) ? 1 : 0;
                    int maxValue = sides.Max();
                    int exitDirection = Array.IndexOf(sides, maxValue);
                    trigger = exitDirection != this.direction;
                }
                if (trigger)
                {
                    if (delay > 0) Add(new Coroutine(OnEnterSequence(player)));
                    else if (!triggered && StrawberryJam2021Module.VivHelperGetFlags(level, flags, "and") && player != null && !player.Dead) Teleport(player);
                } 
            }
            direction = -1;
        }

        public IEnumerator OnEnterSequence(Player player)
        {
            float timer = delay;
            while(timer > 0)
            {
                if (player.Dead || player == null) { yield break; }
                timer -= Engine.DeltaTime;
                yield return null;
            }
            if (!triggered && StrawberryJam2021Module.VivHelperGetFlags(level, flags, "and") && player != null && !player.Dead)
            {
                Teleport(player);
            }
        }

        public static bool CompareEntityIDs(EntityID a, EntityID b) {
            return a.Level == b.Level && a.ID == b.ID;
        }

        public static bool MatchDashState(int state) {
            return new int[] { 2, 5}.Contains(state);
        }

        private void Teleport(Player player)
        {
            triggered = true;
            float temp4 = transition == TransitionType.GlitchEffect ? 0.5f : 0f;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, null, Engine.DeltaTime, start: true);
            tween.OnComplete = delegate
            {

                level.OnEndOfFrame += delegate
                {
                    Tween tween1 = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, start: true);
                    tween1.OnUpdate = delegate (Tween t)
                    {
                        Glitch.Value = temp4 * (t.Eased);
                    };
                    player.Add(tween1);
                    //Instantiates all the variables of the previous player object into something we can use later.
                    Facings facing = player.Facing;
                    Vector2 levelOffset = level.LevelOffset;
                    Vector2[] vals = new Vector2[5];
                    vals[0] = player.Position - levelOffset; vals[1] = level.Camera.Position - levelOffset;
                    vals[2] = player.Position.X < Position.X ? player.Position - Position + new Vector2(8, 0) : player.Position - Position;
                    vals[3] = level.CameraOffset;
                    Tuple<Vector2, Vector2, bool, bool> c = new Tuple<Vector2, Vector2, bool, bool>(player.CameraAnchor, player.CameraAnchorLerp, player.CameraAnchorIgnoreX, player.CameraAnchorIgnoreY);
                    Dictionary<Entity, Vector2> followerPoints = new Dictionary<Entity, Vector2>();
                    Leader leader = player.Leader;
                    foreach (Follower follower in leader.Followers)
                    {
                        if (follower.Entity != null)
                        {
                            follower.Entity.Position -= player.Position;
                            follower.Entity.AddTag(Tags.Global);
                            if(!CompareEntityIDs(follower.ParentEntityID, EntityID.None)) level.Session.DoNotLoad.Add(follower.ParentEntityID);
                        }
                    }
                    for (int i = 0; i < leader.PastPoints.Count; i++)
                    {
                        leader.PastPoints[i] -= player.Position;
                    }
                    int state = player.StateMachine.State;
                    player.CleanUpTriggers();
                    player.CameraAnchorIgnoreX = true;
                    player.CameraAnchorIgnoreY = true;
                    int pDashes = 0; //This should be player.Dashes if resetDashes is true, if it's false it's unused.
                    Vector2 pDashDir = Vector2.Zero;
                    if (!resetDashes) { pDashes = player.Dashes; }
                    if (MatchDashState(player.StateMachine.State)) { pDashDir = player.DashDir; }
                    //Removes the current level we are in without breaking everything
                    level.Remove(player);
                    level.UnloadLevel();
                    level.Entities.Remove(level.Entities.FindAll<FlingBird>()); //Apparently Awake is called in a FlingBird to save it to the EntityList and we don't want that.
                    
                    //Sets the future values of the player. This code is modified from the Teleport Trigger to accommodate more things.
                    level.Session.Level = newRoom;
                    level.Session.Dreaming = dreaming;
                    level.Session.FirstLevel = false;
                    level.Add(player);
                    level.LoadLevel(Player.IntroTypes.Transition);
                    if (!resetDashes) { player.Dashes = pDashes; }
                    if (newPos.X >= 0 && newPos.X <= level.Bounds.X + level.Bounds.Width - level.LevelOffset.X &&
                        newPos.Y >= 0 && newPos.Y <= level.Bounds.Y + level.Bounds.Height - level.LevelOffset.Y)
                    {
                        player.Position = level.LevelOffset + newPos;
                        if (addTriggerOffset) { player.Position += vals[2]; }
                        ModifySpeed(player);
                        if (legacyCamera == 2) NewCamera(level, player, vals, c);
                        else CameraShit(level, player, level.LevelOffset + vals[1]);
                    }
                    else if (vals[0].X >= 0 && vals[0].X <= level.Bounds.X + level.Bounds.Width - level.LevelOffset.X &&
                        vals[0].Y >= 0 && vals[0].Y <= level.Bounds.Y + level.Bounds.Height - level.LevelOffset.Y)
                    {
                        player.Position = level.LevelOffset + vals[0];
                        ModifySpeed(player);
                        if (legacyCamera == 2) NewCamera(level, player, vals, c);
                        else CameraShit(level, player, level.LevelOffset + vals[1]);
                    }
                    else
                    {
                        player.Position = new Vector2(1, 1);
                        ModifySpeed(player);
                        if (legacyCamera > 2) NewCamera(level, player, vals, c);
                        else CameraShit(level, player, level.LevelOffset + vals[1]);
                    }
                    try
                    {
                        level.Session.RespawnPoint = level.GetSpawnPoint(player.Position);
                    }
                    catch
                    {
                        level.Session.RespawnPoint = player.Position;
                        Console.WriteLine("No Respawn Point found, made one. Please be aware."); //Test
                    }
                    if (MatchDashState(player.StateMachine.State)) { player.DashDir = pDashDir.RotateTowards(player.Speed.Angle(), 6.3f); }
                    if (state == 10) { player.SummitLaunch(player.Position.X); }

                    foreach (Follower follower in leader.Followers)
                    {
                        if (follower.Entity != null)
                        {
                            follower.Entity.Position += player.Position;
                            follower.Entity.RemoveTag(Tags.Global);
                            level.Session.DoNotLoad.Remove(follower.ParentEntityID);
                        }
                    }
                    for (int i = 0; i < leader.PastPoints.Count; i++)
                    {
                        leader.PastPoints[i] += player.Position;
                    }
                    leader.TransferFollowers();

                    player.Facing = facing;
                    player.Hair.MoveHairBy(level.LevelOffset - levelOffset);

                    /*if (level.Wipe != null)
                    {
                        level.Wipe.Cancel();
                    }*/
                    Tween tween2 = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, start: true);
                    tween2.OnUpdate = delegate (Tween t)
                    {
                        Glitch.Value = temp4 * (1f - t.Eased);
                    };
                    player.Add(tween2);
                    if (transition == TransitionType.ColorFlash) { level.Flash(flashColor, false); }
                    Transition(transition, player);
                    if (timeSlowDown != 0f)
                    {
                        Add(new Coroutine(TimeSlowDown(timeSlowDown, .1f)));
                    }
                };
            };
            player.Add(tween);
        }

        public static IEnumerator TimeSlowDown(float duration = .25f, float speedInit = 0.5f) {
            for (float i = 0; i < duration; i += .05f) {
                Engine.TimeRate = speedInit + (1 - speedInit) * i;
                yield return .05f;
            }
            Engine.TimeRate = 1f;
        }

        private void ModifySpeed(Player player) {
            if (vel2 == -1f && !(vel[0] == 0f && vel[1] == 0f))
            {
                player.Speed = velMod ? new Vector2(player.Speed.X * vel[0], player.Speed.Y * vel[1]) : player.Speed + new Vector2(vel[0], vel[1]);
            }
            if (vel2 != -1f && vel[0] == 0f && vel[1] == 0f)
            {
                player.Speed = velMod ? player.Speed * vel2 : player.Speed + new Vector2(vel2 * (float)Math.Cos(Calc.Angle(player.Speed)), vel2 * (float)Math.Sin(Calc.Angle(player.Speed)));
            }
            player.Speed = rotMod ? Calc.RotateTowards(player.Speed, rot, 6.3f) : Calc.Rotate(player.Speed, rot);
            
        }

        private void CameraShit(Level level, Player player, Vector2 v)
        {
            if (legacyCamera == 0)
            {
                level.Camera.Position = v;
            }
            else
            {
                Vector2 camPos = player.Position;
                Vector2 vector2 = new Vector2(player.X - 160f, player.Y - 90f);
                camPos.X = MathHelper.Clamp(vector2.X, (float)level.Bounds.Left, (float)(level.Bounds.Right - 320));
                camPos.Y = MathHelper.Clamp(vector2.Y, (float)level.Bounds.Top, (float)(level.Bounds.Bottom - 180));
                level.Camera.Position = camPos;

            }
        }

        private void NewCamera(Level level, Player player, Vector2[] vals, Tuple<Vector2, Vector2, bool, bool> c)
        {
            level.CameraOffset = vals[3];
            Vector2 camPos = player.Position;
            Vector2 vector2 = new Vector2(player.X - 160f, player.Y - 90f) + vals[3];
            camPos.X = MathHelper.Clamp(vector2.X, (float)level.Bounds.Left, (float)(level.Bounds.Right - 320));
            camPos.Y = MathHelper.Clamp(vector2.Y, (float)level.Bounds.Top, (float)(level.Bounds.Bottom - 180));
            level.Camera.Position = camPos;


            switch (legacyCamera)
            {
                case 3:
                    player.CameraAnchor = c.Item1;
                    player.CameraAnchorLerp = Vector2.Zero;
                    player.CameraAnchorIgnoreX = c.Item3;
                    player.CameraAnchorIgnoreY = c.Item4;
                    break;
                default:
                    player.CameraAnchor = c.Item1;
                    player.CameraAnchorLerp = c.Item2;
                    player.CameraAnchorIgnoreX = c.Item3;
                    player.CameraAnchorIgnoreY = c.Item4;
                    break;
            }
            

        }
      
    }
}
