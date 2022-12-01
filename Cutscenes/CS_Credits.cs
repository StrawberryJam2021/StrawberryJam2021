using Celeste.Mod.CollabUtils2.Entities;
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
        private const float FadeTime = 2f;
        private const string CreditsSong = "event:/sj21_credits";

        public static readonly Dictionary<string, string> HeartsidesToLobbies = new() {
            { "StrawberryJam2021/1-Beginner/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/1-Beginner" },
            { "StrawberryJam2021/2-Intermediate/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/2-Intermediate" },
            { "StrawberryJam2021/3-Advanced/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/3-Advanced" },
            { "StrawberryJam2021/4-Expert/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/4-Expert" },
            { "StrawberryJam2021/5-Grandmaster/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/5-Grandmaster" }
        };

        internal static Type lobbyMapControllerType;
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
            Tag = TagsExt.SubHUD;
        }

        public override void OnBegin(Level level) {
            Audio.BusMuted(Buses.GAMEPLAY, mute: true);
            MInput.UpdateNull();
            MInput.Disabled = true;
            Level.InCredits = true;
            Level.TimerHidden = true;

            if (fromHeartside) {
                Add(new Coroutine(LobbyRoutine()));
            } else {
                Add(new Coroutine(MovieRoutine()));
            }
        }

        public override void Update() {
            base.Update();
            Audio.MusicUnderwater = false;

            if (!TasHelper.Active) {
                MInput.Disabled = false;
                if (Level.CanPause && (Input.Pause.Pressed || Input.ESC.Pressed)) {
                    Input.Pause.ConsumeBuffer();
                    Input.ESC.ConsumeBuffer();
                    Level.Pause(minimal: true);
                }
                MInput.Disabled = true;
            }
        }

        public override void Render() {
            base.Render();
            if (fromHeartside) {
                if (SaveData.Instance.Assists.MirrorMode) {
                    gradient.Draw(new Vector2(1720f, -10f), Vector2.Zero, Color.White * 0.6f, new Vector2(-1f, 1100f));
                } else {
                    gradient.Draw(new Vector2(200f, -10f), Vector2.Zero, Color.White * 0.6f, new Vector2(1f, 1100f));
                }
            }

            if (fade > 0f) {
                Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * Ease.CubeInOut(fade));
            }
        }

        public override void OnEnd(Level level) {
            Audio.BusMuted(Buses.GAMEPLAY, mute: false);
            MInput.Disabled = false;
            credits?.RemoveSelf();

            if (fromHeartside) {
                Level.CompleteArea(skipScreenWipe: true, skipCompleteScreen: true);
            } else {
                Level.InCredits = false;
                Level.TimerHidden = false;
                Level.Add(new TotalStrawberriesDisplay());
                Level.Add(new GameplayStats());
                Level.Session.Audio = previousAudio;
                Level.Session.Audio.Apply();
                if (Level.Tracker.GetEntity<CreditsTalker>()?.Get<TalkComponent>() is TalkComponent talker) {
                    talker.Enabled = true;
                }
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

            Level.Entities.FindFirst<TotalStrawberriesDisplay>()?.RemoveSelf();
            Level.Entities.FindFirst<GameplayStats>()?.RemoveSelf();
            if (Level.Tracker.GetEntity<CreditsTalker>()?.Get<TalkComponent>() is TalkComponent talker) {
                talker.Enabled = false;
            }

            yield return FadeTo(1f);

            Level.Session.Audio.Music.Event = CreditsSong;
            Level.Session.Audio.Apply();

            yield return 0.5f;

            Level.Add(credits = new Credits(Celeste.TargetCenter));

            while (credits.BottomTimer < 2f) {
                yield return null;
            }

            yield return new FadeWipe(Level, wipeIn: false) {
                Duration = 3f,
                OnUpdate = (percent) => Audio.SetMusicParam("fade", 1 - percent)
            }.Wait();

            credits.RemoveSelf();

            yield return FadeTo(0f);

            EndCutscene(Level);
        }

        private IEnumerator LobbyRoutine() {
            yield return null;

            Level.Entities.FindFirst<TotalStrawberriesDisplay>()?.RemoveSelf();
            Level.Entities.FindFirst<GameplayStats>()?.RemoveSelf();
            Level.Entities.OfType<RainbowBerry>().FirstOrDefault()?.RemoveSelf();
            if (lobbyMapControllerType != null) {
                Level.Tracker.Entities[lobbyMapControllerType].FirstOrDefault()?.RemoveSelf();
            }

            foreach (CustomBirdTutorial tutorial in Level.Tracker.GetEntities<CustomBirdTutorial>()) {
                tutorial.TriggerHideTutorial();
            }

            foreach (Entity entity in Level.Entities) {
                entity.Get<TalkComponent>()?.RemoveSelf();
            }

            Level.Wipe.Cancel();

            fade = 1f;

            Level.Session.Audio.Music.Event = CreditsSong;
            Level.Session.Audio.Apply();

            yield return 0.5f;

            float creditsX = SaveData.Instance.Assists.MirrorMode ? 50f : 1870f;
            Level.Add(credits = new Credits(new Vector2(creditsX, 0f), alignment: 1f, scale: 0.6f, doubleColumns: false));

            yield return 1f;

            if (playbacks.Count > 0) {
                foreach (CreditsPlayback playback in playbacks.Values) {
                    if (Level.Tracker.GetEntity<Player>() is Player player) {
                        player.Position = playback.Position;
                        Level.Camera.Position = player.CameraTarget;
                        yield return 1f;
                    }

                    TasHelper.Play(playback.Inputs);
                    yield return FadeTo(0f);
                    yield return TasHelper.Wait(buffer: FadeTime);
                    yield return FadeTo(1f);
                }
            } else {
                // DEBUG (show lobby so we can verify things work)
                yield return FadeTo(0f);
                yield return 3f;
                yield return FadeTo(1f);
            }

            while (credits.BottomTimer < 2f) {
                yield return null;
            }

            yield return new FadeWipe(Level, wipeIn: false) {
                Duration = 3f,
                OnUpdate = (percent) => Audio.SetMusicParam("fade", 1 - percent)
            }.Wait();

            Audio.SetMusic(null);

            EndCutscene(Level);
        }

        private IEnumerator FadeTo(float value) {
            while (fade != value) {
                fade = Calc.Approach(fade, value, Engine.DeltaTime / FadeTime);
                yield return null;
            }
        }

        public struct CreditsPlayback {
            public Vector2 Position;
            public List<TasInput> Inputs;

            public CreditsPlayback(Vector2 position, List<TasInput> inputs) {
                Position = position;
                Inputs = inputs;
            }
        }

        #region Hooks

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
                    string name = entityData.Attr("tutorial");
                    string path = $"Tutorials/{name}.tas";
                    if (TasHelper.TryParse(path, out List<TasInput> inputs)) {
                        if (inputs.Count > 0) {
                            credits.playbacks[name] = new CreditsPlayback(entityData.Position + offset, inputs);
                        } else {
                            LevelEnter.ErrorMessage = "[SJ] Could not find a TAS file at {#ff1144}" + path + "{#}";
                        }                        
                    } else {
                        LevelEnter.ErrorMessage = "[SJ] Could not parse any inputs in {#ff1144}" + path + "{#}";
                    }
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
        #endregion
    }
}
