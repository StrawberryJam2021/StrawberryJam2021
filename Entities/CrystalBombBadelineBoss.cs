using Celeste.Mod.CavernHelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CrystalBombBadelineBoss")]
    [Tracked]
    public class CrystalBombBadelineBoss : FinalBoss {
        private DynamicData baseData;

        private static Hook crystalBombExplodeHook;
        private static MethodInfo crystalBombExplodeInfo = typeof(CrystalBomb).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo crystalBombExplodeHookInfo = typeof(CrystalBombBadelineBoss).GetMethod("On_CrystalBomb_Explode", BindingFlags.NonPublic | BindingFlags.Static);

        public CrystalBombBadelineBoss(EntityData data, Vector2 offset) : base(data, offset) {
            baseData = new DynamicData(typeof(FinalBoss), this);
            Get<PlayerCollider>().OnCollide = OnPlayer;
        }

        public static void Load() {
            crystalBombExplodeHook = new Hook(crystalBombExplodeInfo, crystalBombExplodeHookInfo);
            On.Celeste.Seeker.RegenerateCoroutine += On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode += On_Puffer_Explode;
        }

        public static void Unload() {
            crystalBombExplodeHook?.Dispose();
            On.Celeste.Seeker.RegenerateCoroutine -= On_Seeker_RegenerateCoroutine;
            On.Celeste.Puffer.Explode -= On_Puffer_Explode;
        }

        private static void On_CrystalBomb_Explode(Action<CrystalBomb> orig, CrystalBomb self) {
            DynamicData bombData = new DynamicData(self);
            if (bombData.Get<bool>("exploded"))
                return;
            self.Collider = bombData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = bombData.Get<Hitbox>("hitBox");
            orig(self);
        }

        private static IEnumerator On_Seeker_RegenerateCoroutine(On.Celeste.Seeker.orig_RegenerateCoroutine orig, Seeker self) {
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext()) {
                yield return origEnum.Current;
            }
            DynamicData seekerData = new DynamicData(self);
            self.Collider = seekerData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = seekerData.Get<Hitbox>("physicsHitbox");
        }

        private static void On_Puffer_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            orig(self);
            DynamicData pufferData = new DynamicData(self);
            Collider origCollider = self.Collider;
            self.Collider = pufferData.Get<Circle>("pushRadius");
            foreach (CrystalBombBadelineBoss boss in self.CollideAll<CrystalBombBadelineBoss>()) {
                boss.OnHit();
            }
            self.Collider = origCollider;
        }

        public override void Update() {
            base.Update();
            avoidPos = Vector2.Zero;
            if (Collidable) {
                foreach (CrystalBomb bomb in level.Entities.FindAll<CrystalBomb>()) {
                    if (CollideCheck(bomb))
                        new DynamicData(bomb).Invoke("Explode");
                }
            }
        }

        private new void OnPlayer(Player player) {
            player.Die((player.Center - Center).SafeNormalize());
        }

        // OnHit (originally OnPlayer) and MoveSequence are mostly copy-pasted and cleaned up from vanilla FinalBoss

        // private fields and methods that need to be accessed
        private SoundSource chargeSfx => baseData.Get<SoundSource>("chargeSfx");
        private SoundSource laserSfx => baseData.Get<SoundSource>("laserSfx");
        private Level level => baseData.Get<Level>("level");
        private Coroutine attackCoroutine => baseData.Get<Coroutine>("attackCoroutine");
        private Vector2[] nodes => baseData.Get<Vector2[]>("nodes");
        private bool dialog => baseData.Get<bool>("dialog");
        private FinalBossStarfield bossBg => baseData.Get<FinalBossStarfield>("bossBg");
        private SineWave floatSine => baseData.Get<SineWave>("floatSine");
        private int nodeIndex {
            get => baseData.Get<int>("nodeIndex");
            set => baseData.Set("nodeIndex", value);
        }
        private Vector2 avoidPos {
            get => baseData.Get<Vector2>("avoidPos");
            set => baseData.Set("avoidPos", value);
        }
        private int facing {
            get => baseData.Get<int>("facing");
            set => baseData.Set("facing", value);
        }
        private void TriggerFallingBlocks(float leftOfX) => baseData.Invoke("TriggerFallingBlocks", leftOfX);
        private void TriggerMovingBlocks(int nodeIndex) => baseData.Invoke("TriggerMovingBlocks", nodeIndex);
        private void CreateBossSprite() => baseData.Invoke("CreateBossSprite");
        private void StartAttacking() => baseData.Invoke("StartAttacking");
        private bool _CanChangeMusic(bool value, FinalBoss self) => baseData.Invoke<bool>("_CanChangeMusic", value, self);

        private void OnHit() {
            if (Sprite == null) {
                CreateBossSprite();
            }
            Sprite.Play("getHit");
            Audio.Play(SFX.char_bad_boss_hug, Position);
            chargeSfx.Stop();
            if (laserSfx.EventName == SFX.char_bad_boss_laser_charge && laserSfx.Playing) {
                laserSfx.Stop();
            }
            Collidable = false;
            nodeIndex++;
            if (dialog) {
                if (nodeIndex == 1) {
                    Scene.Add(new MiniTextbox("ch6_boss_tired_a"));
                } else if (nodeIndex == 2) {
                    Scene.Add(new MiniTextbox("ch6_boss_tired_b"));
                } else if (nodeIndex == 3) {
                    Scene.Add(new MiniTextbox("ch6_boss_tired_c"));
                }
            }
            foreach (FinalBossShot shot in level.Tracker.GetEntities<FinalBossShot>()) {
                shot.Destroy();
            }
            foreach (FinalBossBeam beam in level.Tracker.GetEntities<FinalBossBeam>()) {
                beam.Destroy();
            }
            TriggerFallingBlocks(X);
            TriggerMovingBlocks(nodeIndex);
            attackCoroutine.Active = false;
            Moving = true;
            bool lastHit = nodeIndex == nodes.Length - 1;
            if (_CanChangeMusic(level.Session.Area.Mode == AreaMode.Normal, this)) {
                AudioState levelAudio = level.Session.Audio;
                if (lastHit && level.Session.Level.Equals("boss-19")) {
                    Alarm.Set(this, 0.25f, () => {
                        Audio.Play(SFX.game_06_boss_spikes_burst);
                        foreach (CrystalStaticSpinner spinner in Scene.Tracker.GetEntities<CrystalStaticSpinner>()) {
                            spinner.Destroy(boss: true);
                        }
                    });
                    Audio.SetParameter(Audio.CurrentAmbienceEventInstance, "postboss", 1f);
                    Audio.SetMusic(null);
                } else if (levelAudio.Music.Event != SFX.music_reflection_fight && levelAudio.Music.Event != SFX.music_reflection_fight_glitch) {
                    levelAudio.Music.Event = SFX.music_reflection_fight;
                    levelAudio.Apply(forceSixteenthNoteHack: false);
                }
            }
            Add(new Coroutine(MoveSequence(lastHit)));
        }

        private IEnumerator MoveSequence(bool lastHit) {
            if (lastHit) {
                Audio.SetMusicParam("boss_pitch", 1f);
                // not sure what to call these tweens but I didn't want to keep the decompiler ones
                Tween glitchTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, start: true);
                glitchTween.OnUpdate = (Tween tween) => {
                    Glitch.Value = 0.6f * tween.Eased;
                };
                Add(glitchTween);
            } else {
                Tween glitchTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, start: true);
                glitchTween.OnUpdate = (Tween tween) => {
                    Glitch.Value = 0.5f * (1f - tween.Eased);
                };
                Add(glitchTween);
            }
            yield return 0.15f;
            foreach (ReflectionTentacles tentacles in Scene.Tracker.GetEntities<ReflectionTentacles>()) {
                tentacles.Retreat();
            }
            Celeste.Freeze(0.1f);
            if (lastHit) {
                Engine.TimeRate = 0.5f;
            } else {
                Engine.TimeRate = 0.75f;
            }
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            level.Shake();
            yield return 0.05f;
            for (float angle = 0f; angle < (float) Math.PI * 2f; angle += (float) Math.PI / 18f) {
                Vector2 position = Center + Sprite.Position + Calc.AngleToVector(angle + Calc.Random.Range(-(float) Math.PI / 90f, (float) Math.PI / 90f), Calc.Random.Range(16, 20));
                level.Particles.Emit(P_Burst, position, angle);
            }
            yield return 0.05f;
            Audio.SetMusicParam("boss_pitch", 0f);
            float startTimeRate = Engine.TimeRate;
            Tween effectsTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.35f / Engine.TimeRateB, start: true);
            effectsTween.UseRawDeltaTime = true;
            effectsTween.OnUpdate = (Tween tween) => {
                if (bossBg != null && bossBg.Alpha < tween.Eased) {
                    bossBg.Alpha = tween.Eased;
                }
                Engine.TimeRate = MathHelper.Lerp(startTimeRate, 1f, tween.Eased);
                if (lastHit) {
                    Glitch.Value = 0.6f * (1f - tween.Eased);
                }
            };
            Add(effectsTween);
            yield return 0.2f;
            Vector2 startPosition = Position;
            Vector2 endPosition = nodes[nodeIndex];
            float duration = Vector2.Distance(startPosition, endPosition) / 600f;
            float direction = (endPosition - startPosition).Angle();
            Tween moveTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineInOut, duration, start: true);
            moveTween.OnUpdate = (Tween tween) => {
                Position = Vector2.Lerp(startPosition, endPosition, tween.Eased);
                if (tween.Eased >= 0.1f && tween.Eased <= 0.9f && Scene.OnInterval(0.02f)) {
                    TrailManager.Add(this, Player.NormalHairColor, 0.5f, frozenUpdate: false, useRawDeltaTime: false);
                    level.Particles.Emit(Player.P_DashB, 2, Center, Vector2.One * 3f, direction);
                }
            };
            moveTween.OnComplete = (_) => {
                Sprite.Play("recoverHit");
                Moving = false;
                Collidable = true;
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (entity != null) {
                    facing = Math.Sign(entity.X - X);
                    if (facing == 0) {
                        facing = -1;
                    }
                }
                StartAttacking();
                floatSine.Reset();
            };
            Add(moveTween);
        }
    }
}
