using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/WormholeBooster")]
    class WormholeBooster : Booster {
        private PlayerCollider coll;
        private DynData<Booster> self;
        public static bool TeleDeath; // if true, the next boost from a Wormhole Booster will kill the player
        public static bool TeleportingDNI; // if true, the player can't interact with other boosters, used to avoid teleportation loops
        public static bool TDLock = false; // additional interaction-blocking variable, to differentiate between dashing and boosting
        public static ParticleType P_Teleporting;
        public static ParticleType P_WBurst;
        public static ParticleType P_WAppear;
        private static MethodInfo BoostPlayer = typeof(Booster).GetMethod("OnPlayer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
        private Color color;

        public IEnumerator TeleportCoroutine(Player player) {


            WormholeBooster nearest = FindNearestBooster();
            if (nearest == null) {
                RemoveSelf();
                yield break;
            }

            SceneAs<Level>().Add(new WBTrailManager(Position, nearest.Position));
            Celeste.Freeze(Engine.RawDeltaTime * 4);

            Audio.Play("event:/char/badeline/disappear", nearest.Position);
            self.Get<Sprite>("sprite").Visible = false;
            Collidable = false;
            player.Position = nearest.Position;
            typeof(Booster).GetMethod("OnPlayer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod).Invoke(nearest, new object[] { player });

            TeleportingDNI = true;

            yield return DashFix();

            RemoveSelf();
        }


        private static IEnumerator DashFix() {
            Input.Dash.ConsumePress();
            float timer = 0.25f;
            while (timer >= Engine.DeltaTime) {
                timer -= Engine.DeltaTime;
                yield return null;
                if (Input.Dash.Pressed) {
                    timer = 0;
                    Input.Dash.ConsumePress();
                    break;
                }
            }
        }

        public WormholeBooster(EntityData data, Vector2 offset) : this(data.Position + offset) {
        }

        public WormholeBooster(Vector2 position) : base(position, false) {
            TeleportingDNI = false;
            TDLock = false;
            TeleDeath = false;

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            PlayerCollider c;
            while ((c = Get<PlayerCollider>()) != null)
                Remove(c);
            Add(coll = new PlayerCollider(onWormholeActivate));

            Remove(self.Get<Sprite>("sprite"));
            Sprite sprite = StrawberryJam2021Module.SpriteBank.Create("wormholeBooster");
            self["sprite"] = sprite;
            Add(sprite);
            color = Calc.HexToColor("7800bd");

            self["particleType"] = P_WBurst;

        }
        public override void Added(Scene scene) {
            base.Added(scene);
            self = new DynData<Booster>(this);
            self.Get<Entity>("outline").RemoveSelf();
        }
        public static void Load() {
            On.Celeste.Player.NormalBegin += allowTeleport;
            On.Celeste.Player.DashBegin += killPlayer;
            On.Celeste.Player.BoostEnd += readyAT;
            On.Celeste.Player.BoostCoroutine += increaseDelay;
            On.Celeste.Booster.AppearParticles += wormholeAppearParticles;
        }

        private static void killPlayer(On.Celeste.Player.orig_DashBegin orig, Player self) {
            WormholeBooster booster = self.CollideFirst<WormholeBooster>();
            if (booster != null) {
                TeleportingDNI = true;
                TDLock = true;
            }
            if (TeleDeath && booster != null && self != null) {
                self.Visible = false;
                self.StateMachine.State = 11;
                self.DummyGravity = false;
                bool hitDash = false;

                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.ExpoIn, 0.75f, false);
                DynData<Booster> boost = new DynData<Booster>(booster);
                tween.OnUpdate = delegate {
                    boost.Get<Sprite>("sprite").Scale = new Vector2(1 - tween.Eased);
                    if (Input.Dash.Pressed && !hitDash) {

                        new DynData<Tween>(tween).Set("TimeLeft", 0f);
                        boost.Get<Sprite>("sprite").Scale = Vector2.Zero;
                        hitDash = true;
                        Input.Dash.ConsumePress();
                    }
                };
                tween.OnComplete = delegate {
                    TeleDeath = false;
                    TeleportingDNI = false;
                    TDLock = false;
                    Audio.Play("event:/char/badeline/disappear", self.Position);
                    self.StateMachine.State = 0;
                    self.Die(Vector2.Zero);

                    if (self != null) {

                        self.Visible = true;
                        new DynData<Booster>(self.CurrentBooster).Get<Entity>("outline").RemoveSelf();
                        self.CurrentBooster.RemoveSelf();
                    }
                };
                booster.Add(tween);
                booster.Collidable = false;
                tween.Start();
            } else
                orig(self);
        }

        public static void Unload() {
            On.Celeste.Player.NormalBegin -= allowTeleport;
            On.Celeste.Player.BoostEnd -= readyAT;
            On.Celeste.Player.DashBegin -= killPlayer;
            On.Celeste.Player.BoostCoroutine -= increaseDelay;
            On.Celeste.Booster.AppearParticles -= wormholeAppearParticles;
        }

        private static void wormholeAppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self) {
            if (self is WormholeBooster) {
                ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
                for (int i = 0; i < 360; i += 30) {
                    particlesBG.Emit(P_WAppear, 1, self.Center, Vector2.One * 2f, (float) i * ((float) Math.PI / 180f));
                }
            } else {
                orig(self);
            }
        }

        private static void readyAT(On.Celeste.Player.orig_BoostEnd orig, Player self) {
            TDLock = true;
            orig(self);
        }
        private static IEnumerator increaseDelay(On.Celeste.Player.orig_BoostCoroutine orig, Player self) {

            if (!TeleportingDNI) {
                IEnumerator original = orig(self);
                while (original.MoveNext())
                    yield return original.Current;
            } else {
                yield return 0.45f;
                self.StateMachine.State = 2;

            }
        }
        private static void allowTeleport(On.Celeste.Player.orig_NormalBegin orig, Player self) {
            TeleportingDNI = false;
            TDLock = false;
            orig(self);
        }

        public override void Update() {
            base.Update();
            if (self.Get<float>("respawnTimer") > 0.2f) {
                self.Set("respawnTimer", 0.1f);
            }
            self.Get<Sprite>("sprite").Color = color;
            if (SceneAs<Level>().Tracker.CountEntities<Entities.WormholeBooster>() == 1 && !TeleportingDNI && !TDLock) {
                TeleDeath = true;
            }
            if (TeleDeath) {
                color = Color.Lerp(color, Calc.HexToColor("61010c"), 3f * Engine.DeltaTime);
            }
        }
        private void onWormholeActivate(Player player) {
            if (TeleportingDNI)
                return;
            else {
                TeleportingDNI = true;
                if (TeleDeath) {
                    BoostPlayer.Invoke(this, new object[] { player });
                } else {

                    Add(new Coroutine(TeleportCoroutine(player)));

                }
            }
        }
        private WormholeBooster FindNearestBooster() {
            WormholeBooster leader = null;
            foreach (WormholeBooster booster in SceneAs<Level>().Tracker.GetEntities<WormholeBooster>()) {


                DynData<Booster> boost = new DynData<Booster>(booster);
                if (booster != this && booster.Collidable && !booster.BoostingPlayer && boost.Get<float>("respawnTimer") <= 0f) {
                    if (leader == null || (booster.Position - Position).Length() < (leader.Position - Position).Length()) {
                        leader = booster;

                    }

                }



            }
            return leader;
        }
        private class WBTrailManager : Entity {
            private Tween t;
            public WBTrailManager(Vector2 from, Vector2 to) : base(from) {
                Tag = Tags.FrozenUpdate;
                float duration = 0.2f;
                if ((to - from).LengthSquared() < 3000f)
                    duration = 0.2f;
                Add(t = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, duration, false));
                t.OnUpdate = delegate (Tween t) {
                    Vector2 where = Vector2.Lerp(Position, to, Calc.Max(t.Eased - 0.07f, 0));
                    SceneAs<Level>().Particles.Emit(P_Teleporting, 8, where, Vector2.Zero, (to - Position).Angle());
                };
                t.OnComplete = delegate { RemoveSelf(); };

            }
            public override void Awake(Scene scene) {
                t.Start();
            }
        }
    }
}