using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;
using Microsoft.Xna.Framework;
using System.Collections;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    public static class DarkMatterHooks {
        //Constants and constant-adjacents
        public const float DarkMatterAccel = 1.006f; //The constant acceleration per frame, in Δv/f, or v1-v0/f
        public const float DarkMatterMaxSpeedMultSq = 15f; //The maximum speed from entry speed, squared
        public const float DarkMatterMinSpeed = 200f; //The minimum speed that DarkMatter will travel at

        //StaticValues that actually need to be stored
        public static Sprite darkMatterSprite; //The Dark Matter Sprite, directly copied from TwigHelper
        public static int DarkMatterPlayerState; //Dark Matter Player State
        private static float initialSpeedSq; //The speed of the Player when entering DarkMatter squared; is reset on every exit.
        //The final speed will always be initialSpeedSq * DarkMatterMaxSpeedMultSq
        //Hooks
        public static void Load() {
            On.Celeste.Player.ctor += Player_ctor;
            IL.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
            On.Celeste.GameplayBuffers.Create += onGameplayBuffersCreate;
            On.Celeste.GameplayBuffers.Unload += onGameplayBuffersUnload;
        }

        public static void LoadContent(bool firstLoad) {
            darkMatterSprite = new Sprite(GFX.Game, "objects/StrawberryJam2021/darkMatterSprite/");
            darkMatterSprite.AddLoop("boost", "darkMatter", 0.06f);
            darkMatterSprite.Justify = new Vector2(0.5f, 0.75f); //This is the clean justification for the sprite rendering over top of the player
        }

        public static void Unload() {
            On.Celeste.Player.ctor -= Player_ctor;
            IL.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
            On.Celeste.GameplayBuffers.Create -= onGameplayBuffersCreate;
            On.Celeste.GameplayBuffers.Unload -= onGameplayBuffersUnload;
        }

        private static void onGameplayBuffersCreate(On.Celeste.GameplayBuffers.orig_Create orig) {
            orig();
            DarkMatterRenderer.DarkMatterLightning = VirtualContent.CreateRenderTarget("SJ2021-buffer-dark-matter", 160, 160);
            DarkMatterRenderer.BlurTempBuffer = VirtualContent.CreateRenderTarget("SJ2021-temp-blur-buffer", 320, 180);
        }

        private static void onGameplayBuffersUnload(On.Celeste.GameplayBuffers.orig_Unload orig) {
            orig();
            DarkMatterRenderer.DarkMatterLightning?.Dispose();
            DarkMatterRenderer.DarkMatterLightning = null;
            DarkMatterRenderer.BlurTempBuffer?.Dispose();
            DarkMatterRenderer.BlurTempBuffer = null;
        }

        //Because DarkMatterRenderer is a Global object it must be loaded on LoadingThread.
        //ILHook required for clean, no error throwing code. At large numbers of hooks, this can cause lock-time issues because of concurrent threads, or something like that. This seems to resolve that bug.
        private static void LevelLoader_LoadingThread(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchRet());
            if (cursor.TryGotoPrev(instr => instr.MatchLdarg(0))) { //Goes directly before `this.Loaded = true`
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<LevelLoader>>(LoadingThreadMod);
            }
        }

        private static void LoadingThreadMod(LevelLoader self) {
            List<LevelData> Levels = self.Level.Session?.MapData?.Levels ?? null;
            if (Levels != null) {
                if (Levels.Any(level => level.Entities?.Any(entity => entity.Name == "SJ2021/DarkMatter") ?? false)) {
                    self.Level.Add(new DarkMatterRenderer());
                }
            }
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig.Invoke(self, position, spriteMode);
            DarkMatterPlayerState = self.StateMachine.AddState(DarkMatterUpdate, DarkMatterCoroutine, DarkMatterBegin, DarkMatterEnd);
        }

        //DarkMatter Behavior. Copied from TwigHelper/JackalCollabHelper but cleaned up significantly.
        public static IEnumerator DarkMatterCoroutine(Player player) {
            yield return 0.1f; // wait 6 frames, this should be ample time for the player, and if the game is frozen this should not increase
            player.RefillStamina(); // refill all stamina
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            player.SceneAs<Level>().DirectionalShake(Vector2.Normalize(player.Speed));
        }

        public static void DarkMatterBegin(Player player) {
            player.Dashes = 0; //Disable Dashing
            player.Sprite.Visible = false;
            player.Hair.Visible = false;
            initialSpeedSq = player.Speed.LengthSquared();
            darkMatterSprite.Play("boost");
            player.Add(darkMatterSprite);
        }

        public static void DarkMatterEnd(Player player) {
            player.RefillDash();
            player.Sprite.Visible = true;
            player.Hair.Visible = true;
            player.Remove(darkMatterSprite);
            darkMatterSprite.Stop();
            initialSpeedSq = 0;
        }

        public static int DarkMatterUpdate(Player player) {
            if (player.Speed == Vector2.Zero) {
                player.Die(Vector2.Zero, true, true);
                return Player.StDummy;
            }
            //I should probably optimize this check but that's fine.
            if (!player.CollideCheck<DarkMatter>())
                return Player.StNormal;
            Vector2 normalSpeed = Vector2.Normalize(player.Speed); //Normalize externally because memory to code efficiency balance
            if (player.CollideCheck<Solid>()) {
                for(int i = 0; i < 3; i++)
                player.Die(-normalSpeed, true, true);
                return Player.StDummy;
            }
            if (player.Speed.LengthSquared() < 40000f) { // LengthSquared is faster than Length because Math.Sqrt is slow
                player.Speed = normalSpeed * 200f;
            }
            else if(player.Speed.LengthSquared() > initialSpeedSq * DarkMatterMaxSpeedMultSq) {
                player.Speed = normalSpeed * (initialSpeedSq * DarkMatterMaxSpeedMultSq);
            }
            Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
            return DarkMatterPlayerState;
        }
    }

    [Tracked]
    [CustomEntity("SJ2021/DarkMatter")]
    public class DarkMatter : Entity {

        //Particles are renderer from the StrawberryJamDarkMatterRenderer, operating on the List of rectangles.
        public const string Flag = "disable_lightning";

        private readonly float toggleOffset;

        public readonly int VisualWidth;

        public readonly int VisualHeight;

        public DarkMatterRenderer renderer;

        public EntityID id;

        public DarkMatter(Vector2 position, int width, int height, EntityID id)
            : base(position) {
            VisualWidth = width;
            VisualHeight = height;
            Collider = new Hitbox(width - 2, height - 2, 1f, 1f);
            Add(new PlayerCollider(OnPlayer));
            this.id = id;

            toggleOffset = Calc.Random.NextFloat();
        }

        public DarkMatter(EntityData data, Vector2 levelOffset, EntityID id)
            : this(data.Position + levelOffset, data.Width, data.Height, id) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Tracker.GetEntity<DarkMatterRenderer>().Track(this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Tracker.GetEntity<DarkMatterRenderer>().Untrack(this);
        }

        public override void Update() {
            if (Collidable && Scene.OnInterval(0.25f, toggleOffset)) {
                ToggleCheck();
            }
            if (!Collidable && Scene.OnInterval(0.05f, toggleOffset)) {
                ToggleCheck();
            }
            base.Update();
        }

        public void ToggleCheck() {
            Collidable = (Visible = InView());
        }

        private bool InView() {
            Camera camera = (Scene as Level).Camera;
            if (X + Width > camera.X - 16f && Y + Height > camera.Y - 16f && X < camera.X + 320f + 16f) {
                return Y < camera.Y + 180f + 16f;
            }
            return false;
        }

        private void OnPlayer(Player player) {
            if (renderer.mode == DarkMatterRenderer.Mode.Kill) {
                if (!SaveData.Instance.Assists.Invincible) {
                    int num = Math.Sign(player.X - X);
                    if (num == 0) {
                        num = -1;
                    }
                    player.Die(Vector2.UnitX * num);
                }
            } else {
                player.StateMachine.State = DarkMatterHooks.DarkMatterPlayerState;
            }
        }

        public static IEnumerator PulseRoutine(Level level) {
            for (float t2 = 0f; t2 < 1f; t2 += Engine.DeltaTime * 8f) {
                SetPulseValue(level, t2);
                yield return null;
            }
            for (float t2 = 1f; t2 > 0f; t2 -= Engine.DeltaTime * 8f) {
                SetPulseValue(level, t2);
                yield return null;
            }
            SetPulseValue(level, 0f);
        }

        private static void SetPulseValue(Level level, float t) {
            BloomRenderer bloom = level.Bloom;
            DarkMatterRenderer entity = level.Tracker.GetEntity<DarkMatterRenderer>();
            Glitch.Value = MathHelper.Lerp(0f, 0.075f, t);
            bloom.Strength = MathHelper.Lerp(1f, 0.8f, t);
            entity.Fade = t * 0.2f;
        }
    }
}
