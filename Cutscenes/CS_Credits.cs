using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    public class CS_Credits : CutsceneEntity {
        public const float FadeTime = 0.5f;
        public const string CreditsSong = "event:/sj21_credits";

        public static readonly Dictionary<string, string> HeartsidesToLobbies = new() {
            { "StrawberryJam2021/1-Beginner/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/1-Beginner" },
            { "StrawberryJam2021/2-Intermediate/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/2-Intermediate" },
            { "StrawberryJam2021/3-Advanced/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/3-Advanced" },
            { "StrawberryJam2021/4-Expert/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/4-Expert" },
            { "StrawberryJam2021/5-Grandmaster/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/5-Grandmaster" }
        };

        private static ILHook areaCompleteHook;
        private readonly SortedDictionary<string, CreditsPlayback> playbacks = new(StringComparer.OrdinalIgnoreCase);
        private readonly MTexture gradient;
        private readonly bool fromHeartside;
        private AudioState previousAudio;
        private Credits credits;
        private float fade;

        public CS_Credits(bool fromHeartside = true)
            : base(true, false) {
            this.fromHeartside = fromHeartside;
            gradient = GFX.Gui["creditsgradient"].GetSubtexture(0, 1, 1920, 1);
            Tag = Tags.Global | Tags.HUD;
        }

        public override void OnBegin(Level level) {
            Audio.BusMuted(Buses.GAMEPLAY, mute: true);
            MInput.UpdateNull();
            MInput.Disabled = true;

            Level.InCredits = true;
            Level.TimerHidden = true;
            Level.Entities.FindFirst<TotalStrawberriesDisplay>()?.RemoveSelf();
            Level.Entities.FindFirst<GameplayStats>()?.RemoveSelf();

            if (fromHeartside) {
                Add(new Coroutine(LobbyRoutine()));
            } else {
                Add(new Coroutine(MovieRoutine()));
            }
        }

        public override void Update() {
            MInput.Disabled = false;
            if (Level.CanPause && (Input.Pause.Pressed || Input.ESC.Pressed)) {
                Input.Pause.ConsumeBuffer();
                Input.ESC.ConsumeBuffer();
                Level.Pause(minimal: true);
            }

            credits?.Update();
            MInput.Disabled = true;

            base.Update();
        }

        public override void Render() {
            bool mirror = SaveData.Instance.Assists.MirrorMode;
            float creditsX = !fromHeartside ? Celeste.TargetCenter.X : mirror ? 50f : 1570f;

            if (fromHeartside && !Level.Paused) {
                if (mirror) {
                    gradient.Draw(new Vector2(1720f, -10f), Vector2.Zero, Color.White * 0.6f, new Vector2(-1f, 1100f));
                } else {
                    gradient.Draw(new Vector2(200f, -10f), Vector2.Zero, Color.White * 0.6f, new Vector2(1f, 1100f));
                }
            }

            if (fade > 0f) {
                Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * Ease.CubeInOut(fade));
            }

            if (!Level.Paused) {
                credits?.Render(new Vector2(creditsX, 0f));
            }

            base.Render();
        }

        public override void OnEnd(Level level) {
            Audio.BusMuted(Buses.GAMEPLAY, mute: false);
            MInput.Disabled = false;

            if (fromHeartside) {
                Level.CompleteArea(skipScreenWipe: true, skipCompleteScreen: true);
            } else {
                Level.InCredits = false;
                Level.TimerHidden = false;
                Level.Add(new TotalStrawberriesDisplay());
                Level.Add(new GameplayStats());
                Level.Session.Audio = previousAudio;
                Level.Session.Audio.Apply();
            }
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            Audio.BusMuted(Buses.GAMEPLAY, mute: false);
            MInput.Disabled = false;
        }

        private IEnumerator MovieRoutine() {
            previousAudio = Level.Session.Audio.Clone();
            Audio.SetMusic(null);
            yield return FadeTo(1f);

            Level.Session.Audio.Music.Event = CreditsSong;
            Level.Session.Audio.Apply();

            yield return 0.5f;

            credits = new Credits();

            while (credits.BottomTimer < 2f) {
                yield return null;
            }

            yield return new FadeWipe(Level, wipeIn: false).Wait();

            Audio.SetMusic(null);
            credits = null;

            yield return FadeTo(0f);

            EndCutscene(Level);
        }

        private IEnumerator LobbyRoutine() {
            yield return null;

            foreach (CustomBirdTutorial tutorial in Level.Tracker.GetEntities<CustomBirdTutorial>()) {
                tutorial.TriggerHideTutorial();
            }

            foreach (Entity entity in Level.Entities) {
                if (entity.Get<TalkComponent>() is TalkComponent talker && talker.UI != null) {
                    talker.UI.Visible = false;
                }
            }

            Level.Wipe.Cancel();

            fade = 1f;

            Level.Session.Audio.Music.Event = CreditsSong;
            Level.Session.Audio.Apply();

            yield return 0.5f;

            credits = new Credits(scale: 0.6f);

            yield return 1f;
            if (playbacks.Count > 0) {
                //float playbackDuration = playbacks.Values.Sum(p => p.Duration);
                //float transitionTime = (credits.Duration - playbackDuration - 1f - (playbacks.Count * FadeTime * 2)) / playbacks.Count;
                foreach (CreditsPlayback playback in playbacks.Values) {
                    if (Level.Tracker.GetEntity<Player>() is Player player) {
                        player.Position = playback.Position;
                        Level.Camera.Position = Level.GetFullCameraTargetAt(player, playback.Position);
                        yield return 1f;
                    }
                    Level.Add(playback);
                    yield return FadeTo(0f);
                    yield return playback.Wait();
                    yield return FadeTo(1f);
                    //yield return transitionTime;
                }
            } else {
                yield return FadeTo(0f);
            }

            while (credits.BottomTimer < 2f) {
                yield return null;
            }

            yield return new FadeWipe(Level, wipeIn: false).Wait();

            Audio.SetMusic(null);

            EndCutscene(Level);
        }

        private IEnumerator FadeTo(float value) {
            while (fade != value) {
                fade = Calc.Approach(fade, value, Engine.DeltaTime * FadeTime);
                yield return null;
            }
        }

        internal static void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            areaCompleteHook = new ILHook(typeof(AreaComplete).GetMethod("orig_Update"), AreaCompleteUpdateHook);
        }

        internal static void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            areaCompleteHook?.Dispose();
            areaCompleteHook = null;
        }

        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            if (entityData.Name == "playbackTutorial" && HeartsidesToLobbies.Values.Contains(level.Session.Area.SID)) {
                if (level.Entities.ToAdd.OfType<CS_Credits>().FirstOrDefault() is CS_Credits credits) {
                    credits.playbacks[entityData.Attr("tutorial")] = new CreditsPlayback(entityData, offset);
                }                
                
                return true;
            }

            return false;
        }

        private static void AreaCompleteUpdateHook(ILContext il) {
            ILCursor cursor = new(il);
            ILLabel breakLabel = null;

            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchBrfalse(out breakLabel),
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdcI4(0),
                instr => instr.MatchStfld<AreaComplete>("canConfirm"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<AreaComplete, bool>>(SwitchToCredits);
                cursor.Emit(OpCodes.Brtrue, breakLabel);
            }
        }

        private static bool SwitchToCredits(AreaComplete areaComplete) {
            if (HeartsidesToLobbies.TryGetValue(areaComplete.Session.Area.SID, out string lobbySID)) {
                AreaKey targetArea = AreaData.Get(lobbySID)?.ToKey() ?? AreaKey.Default;
                Session session = new(targetArea);
                new FadeWipe(areaComplete, false, delegate () {
                    Audio.SetMusic(null);
                    Engine.Scene = new LevelLoader(new Session(targetArea)) {
                        PlayerIntroTypeOverride = new Player.IntroTypes?(Player.IntroTypes.None),
                        Level = {
                            new CS_Credits()
                        }
                    };
                });
                return true;
            }

            return false;
        }

        // DEBUG
        [Command("sj_credits", "[StrawberryJam2021] triggers the SJ credits (default warps to prologue credits, use 1-5 to play a specific lobby)")]
        private static void PlayCredits(int lobby) {
            string lobbySID = lobby switch {
                1 => "StrawberryJam2021/0-Lobbies/1-Beginner",
                2 => "StrawberryJam2021/0-Lobbies/2-Intermediate",
                3 => "StrawberryJam2021/0-Lobbies/3-Advanced",
                4 => "StrawberryJam2021/0-Lobbies/4-Expert",
                5 => "StrawberryJam2021/0-Lobbies/5-Grandmaster",
                _ => "StrawberryJam2021/0-Lobbies/0-Prologue"
            };

            SaveData.InitializeDebugMode(true);
            AreaKey targetArea = AreaData.Get(lobbySID)?.ToKey() ?? default;
            Session session = new(targetArea);
            if (HeartsidesToLobbies.Values.Contains(targetArea.SID)) {
                // Loads into the appropriate lobby credits as if we came from the HS (mostly)
                new FadeWipe(Engine.Scene, false, delegate () {
                    Audio.SetMusic(null);
                    Engine.Scene = new LevelLoader(session) {
                        PlayerIntroTypeOverride = new Player.IntroTypes?(Player.IntroTypes.None),
                        Level = {
                            new CS_Credits()
                        }
                    };
                });
            } else if (targetArea.SID == "StrawberryJam2021/0-Lobbies/0-Prologue") {
                // Spawns in front of credits talker with the appropriate music progress
                session.RespawnPoint = new Vector2(-210f, -408f);
                session.Audio.Music.Progress = 2;
                Engine.Scene = new LevelLoader(session);
            } else {
                Engine.Commands.Log($"Could not find {lobbySID}");
            }
        }
    }
}
