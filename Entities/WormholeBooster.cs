using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/WormholeBooster")]
    class WormholeBooster : Booster {
        private DynData<Booster> data;
        public Sprite sprite;
        public static bool TeleDeath;
        public static bool CanTeleport;
        public static ParticleType P_Teleporting;
        public static ParticleType P_WBurst;
        public static ParticleType P_WAppear;
        private Color color;
        private static readonly Color displaceColor = Calc.HexToColor("827E00");
        public DisplacementRenderHook displaceHook;
        private MTexture displace;
        private Sprite displacementMask;
        private float displaceEase = 1;
        private const float DelayTime = 0.3f;
        private float delayTimer;

        public WormholeBooster(EntityData data, Vector2 offset) : this(data.Position + offset) {
        }

        public WormholeBooster(Vector2 position) : base(position, false) {
            color = Calc.HexToColor("7800bd");

            displace = GFX.Game["util/StrawberryJam2021/wormhole_disp"];
            Add(displaceHook = new DisplacementRenderHook(BlackHoleDisplacement));

            Add(displacementMask = StrawberryJam2021Module.SpriteBank.Create("wormholeMask"));
            displacementMask.Stop();
            displacementMask.Visible = false;

            data = new DynData<Booster>(this);
            Remove(data.Get<Sprite>("sprite"));
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("WormholeBooster"));
            sprite.Color = color;
            data["sprite"] = sprite;
            data["particleType"] = P_WBurst;

            TeleDeath = false;
            CanTeleport = true;
        }

        public static void Load() {
            On.Celeste.Booster.AppearParticles += wormholeAppearParticles;
            On.Celeste.Booster.PlayerReleased += BoosterPlayerReleasedHook;
            On.Celeste.Player.BoostCoroutine += PlayerBoostCoroutineHook;
            On.Celeste.Player.BoostUpdate += PlayerBoostUpdateHook;
        }

        public static void Unload() {
            On.Celeste.Booster.AppearParticles -= wormholeAppearParticles;
            On.Celeste.Booster.PlayerReleased -= BoosterPlayerReleasedHook;
            On.Celeste.Player.BoostCoroutine -= PlayerBoostCoroutineHook;
            On.Celeste.Player.BoostUpdate -= PlayerBoostUpdateHook;
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

        private static void BoosterPlayerReleasedHook(On.Celeste.Booster.orig_PlayerReleased orig, Booster self) {
            orig(self);
            CanTeleport = true;
            if (self is WormholeBooster wb) {
                wb.delayTimer = DelayTime;
            }
        }

        private static IEnumerator PlayerBoostCoroutineHook(On.Celeste.Player.orig_BoostCoroutine orig, Player self) {
            if (self.CurrentBooster is WormholeBooster booster) {
                yield return 0.45f;
                self.StateMachine.State = Player.StDash;
            } else {
                IEnumerator original = orig(self);
                while (original.MoveNext()) {
                    yield return original.Current;
                }
            }
        }

        private static int PlayerBoostUpdateHook(On.Celeste.Player.orig_BoostUpdate orig, Player self) {
            int result = orig(self);
            if (self.CurrentBooster is WormholeBooster booster) {
                if (CanTeleport && booster.delayTimer <= 0f) {
                    if (TeleDeath) {
                        booster.Add(new Coroutine(booster.KillCoroutine(self)));
                        result = Player.StDummy;
                    } else {
                        booster.Add(new Coroutine(booster.TeleportCoroutine(self)));
                        result = Player.StNormal;
                    }
                }
            }
            return result;
        }


        public static void LoadParticles() {
            P_Teleporting = new ParticleType {
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
            P_WBurst = new ParticleType(P_Burst) {
                Color = Calc.HexToColor("7800bd")
            };
            P_WAppear = new ParticleType(P_Appear) {
                Color = Calc.HexToColor("8100C1")
            };
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            data.Get<Entity>("outline").RemoveSelf();
        }

        public override void Update() {
            base.Update();

            sprite.Color = color;

            if (data.Get<float>("respawnTimer") > 0.2f) {
                data.Set("respawnTimer", 0.1f);
            }
            if (delayTimer > 0f) {
                delayTimer -= Engine.DeltaTime;
            }
            if (Scene.Tracker.CountEntities<WormholeBooster>() == 1) {
                TeleDeath = true;
            }
            if (TeleDeath) {
                color = Color.Lerp(color, Calc.HexToColor("61010c"), 3f * Engine.DeltaTime);
            }
        }

        public IEnumerator TeleportCoroutine(Player player) {
            WormholeBooster nearest = FindNearestBooster();
            if (nearest == null) {
                yield break;
            }
            CanTeleport = false;

            Level level = SceneAs<Level>();
            level.Add(new WBTrailManager(Position, nearest.Position));
            Audio.Play("event:/char/badeline/disappear", nearest.Position);
            sprite.Visible = false;
            Collidable = false;
            player.Position = nearest.Center;

            Tag |= Tags.FrozenUpdate;
            level.Frozen = true;
            Vector2 target = level.GetFullCameraTargetAt(player, player.Position);
            while (Vector2.Distance(level.Camera.Position, target) > Engine.ViewWidth / 8) {
                Vector2 current = level.Camera.Position;
                level.Camera.Position = current + (target - current) * (1f - (float) Math.Pow((double) (0.01f / 1f), (double) Engine.DeltaTime));
                yield return null;
            }
            level.Frozen = false;
            RemoveSelf();
        }

        public IEnumerator KillCoroutine(Player player) {
            TeleDeath = false;
            player.Visible = false;
            player.DummyGravity = false;

            for (float p = 0; p < 1f; p += Engine.DeltaTime / 0.75f) {
                sprite.Scale = Vector2.Lerp(Vector2.One, Vector2.Zero, Ease.CubeIn(p));
                if (Input.DashPressed) {
                    sprite.Scale = Vector2.Zero;
                    break;
                } 
                yield return null;
            }

            Audio.Play("event:/char/badeline/disappear", player.Position);

            player.StateMachine.State = Player.StNormal;
            player.Die(Vector2.Zero);
            player.Visible = true;
            RemoveSelf();
        }

        private void BlackHoleDisplacement() {
            if (!sprite.Visible) {
                displaceEase -= 8 * Engine.RawDeltaTime;
            }
            displaceEase = Calc.Clamp(displaceEase, 0, 1);
            displace.Draw(Position, displace.Center, Color.White * displaceEase, 0.2f);
            displacementMask.GetFrame(sprite.CurrentAnimationID, sprite.CurrentAnimationFrame).Draw(Position - new Vector2(16), Vector2.Zero, displaceColor * displaceEase);
        }

        private WormholeBooster FindNearestBooster() {
            WormholeBooster closest = null;
            float shortestDistance = float.MaxValue;
            foreach (WormholeBooster booster in Scene.Tracker.GetEntities<WormholeBooster>()) {
                if (this == booster) {
                    continue;
                }
                float currDistance = (booster.Position - Position).LengthSquared();
                if (currDistance < shortestDistance) {
                    closest = booster;
                    shortestDistance = currDistance;
                }
            }
            return closest;
        }

        private class WBTrailManager : Entity {
            private Tween t;

            public WBTrailManager(Vector2 from, Vector2 to) : base(from) {
                Tag = Tags.FrozenUpdate;
                Add(t = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, 0.2f, false));
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