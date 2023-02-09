using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    // Adapted from CelesteTAS with permission from DemoJameson: https://github.com/EverestAPI/CelesteTAS-EverestInterop
    // Currently only supports input playback with a subset of gameplay-relevant actions (see original repo for full reference)
    public static class TasHelper {
        private static readonly Regex InputPattern = new(@"^\d+((,[LRDUGJKXCZV])*|(,F,\d+(\.\d+)?))$", RegexOptions.IgnoreCase);
        private static readonly Buttons Left = Buttons.DPadLeft;
        private static readonly Buttons Right = Buttons.DPadRight;
        private static readonly Buttons Down = Buttons.DPadDown;
        private static readonly Buttons Up = Buttons.DPadUp;
        private static readonly Buttons Grab = Buttons.LeftStick;
        private static readonly Buttons Jump = Buttons.A;
        private static readonly Buttons Jump2 = Buttons.Y;
        private static readonly Buttons Dash = Buttons.B;
        private static readonly Buttons Dash2 = Buttons.X;
        private static readonly Buttons DemoDash = Buttons.RightShoulder;
        private static readonly Buttons DemoDash2 = Buttons.RightStick;

        private static readonly Dictionary<string, Binding> TasBinds = new() {
            { "Left", CreateBinding(Buttons.LeftThumbstickLeft, Buttons.DPadLeft) },
            { "Right", CreateBinding(Buttons.LeftThumbstickRight, Buttons.DPadRight) },
            { "Down", CreateBinding(Buttons.LeftThumbstickDown, Buttons.DPadDown) },
            { "Up", CreateBinding(Buttons.LeftThumbstickUp, Buttons.DPadUp) },
            { "Grab", CreateBinding(Grab) },
            { "Jump", CreateBinding(Jump, Jump2) },
            { "Dash", CreateBinding(Dash, Dash2) },
            { "DemoDash", CreateBinding(DemoDash, DemoDash2) },
            { "MenuLeft", new Binding() },
            { "MenuRight", new Binding() },
            { "MenuDown", new Binding() },
            { "MenuUp", new Binding() },
            { "Pause", new Binding() },
            { "Confirm", new Binding() },
            { "Cancel", new Binding() },
            { "Journal", new Binding() },
            { "QuickRestart", new Binding() },
            { "LeftDashOnly", new Binding() },
            { "RightDashOnly", new Binding() },
            { "UpDashOnly", new Binding() },
            { "DownDashOnly", new Binding() },
            { "LeftMoveOnly", new Binding() },
            { "RightMoveOnly", new Binding() },
            { "DownMoveOnly", new Binding() },
            { "UpMoveOnly", new Binding() },
        };

        private static readonly Dictionary<string, TasFile> tasFiles = new();
        private static TasFile activeTas;
        private static GamePadState lastState;
        private static int inputIndex;
        private static int frameIndex;
        private static int elapsedFrames;

        private static bool previousPauseCheck;
        private static bool currentPauseCheck;

        public static bool Active { get; private set; } = false;
        public static bool Paused { get; private set; } = false;
        public static void Clear() => tasFiles.Clear();
        public static float TotalTime => tasFiles.Values.Sum(f => f.TotalFrames) * Engine.DeltaTime;

        private static TasInput CurrentInput => activeTas.Inputs[inputIndex];

        public static bool Preload(string path) {
            List<TasInput> inputs = new();
            Dictionary<int, List<Action>> commands = new();
            if (Everest.Content.TryGet(path, out ModAsset tasFile)) {
                int frame = 0;
                using StreamReader reader = new(tasFile.Stream);
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine().Trim();
                    if (ParseInput(line, out TasInput input)) {
                        inputs.Add(input);
                        frame += input.Frames;
                    } else if (ParseCommand(line, out Action command)) {
                        if (!commands.ContainsKey(frame)) {
                            commands[frame] = new List<Action>();
                        }

                        commands[frame].Add(command);
                    }
                }

                if (inputs.Count > 0) {
                    tasFiles[path] = new TasFile(inputs, commands);
                    return true;
                }
            }

            return false;
        }

        public static void Play(string path) {
            if (!tasFiles.ContainsKey(path) && !Preload(path)) {
                return;
            }

            activeTas = tasFiles[path];
            lastState = default;
            inputIndex = 0;
            frameIndex = 0;
            elapsedFrames = 0;

            Enable();
        }

        // TODO (?): buffer time may not match up exactly due to engine freeze
        public static IEnumerator Wait(float buffer = 0f) {
            int bufferFrames = (int) (buffer / Engine.DeltaTime);
            while (elapsedFrames < activeTas.TotalFrames - bufferFrames) {
                yield return null;
            }
        }

        private static void Enable() {
            if (!Active) {
                Active = true;
                SwitchToTasInput();
            }
        }

        private static void Pause() {
            if (!Paused) {
                Paused = true;
                SwitchToPlayerInput();
                Input.Pause.ConsumePress();
                Input.ESC.ConsumePress();
                (Engine.Scene as Level).Pause(minimal: true);
            }
        }

        private static void Resume() {
            if (Paused) {
                Paused = false;
                SwitchToTasInput();
                SetInputs();
                AdvanceFrame();
            }
        }

        private static void Disable() {
            if (Active) {
                Active = false;
                SwitchToPlayerInput();
            }
        }

        private static void SetInputs() {
            MInput.GamePadData gamePad = MInput.GamePads[Input.Gamepad];
            gamePad.PreviousState = lastState;
            gamePad.CurrentState = CurrentInput.State;
            lastState = gamePad.CurrentState;
            MInput.UpdateVirtualInputs();
        }

        private static void SwitchToTasInput() {
            ApplyTasBindings();

            MInput.Disabled = false;
            MInput.ControllerHasFocus = true;
            MInput.GamePads[Input.Gamepad].Attached = true;
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState = default;
            MInput.Mouse.PreviousState = MInput.Mouse.CurrentState = default;
        }

        private static void SwitchToPlayerInput() {
            Input.Initialize();

            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState = Keyboard.GetState();
            MInput.Mouse.PreviousState = MInput.Mouse.CurrentState = Mouse.GetState();
            for (int i = 0; i < MInput.GamePads.Length; i++) {
                MInput.GamePads[i].PreviousState = MInput.GamePads[i].CurrentState = GamePad.GetState((PlayerIndex)i);
            }
        }

        private static bool PausePressed() {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = default;
            if (MInput.GamePads?.FirstOrDefault(data => data.Attached) is MInput.GamePadData gamePadData) {
                gamePadState = GamePad.GetState(gamePadData.PlayerIndex);
            }

            previousPauseCheck = currentPauseCheck;
            currentPauseCheck = Settings.Instance.Pause.Keyboard.Any(keyboardState.IsKeyDown);
            currentPauseCheck |= keyboardState.IsKeyDown(Keys.Escape);
            currentPauseCheck |= Settings.Instance.Pause.Controller.Any(gamePadState.IsButtonDown);
            return currentPauseCheck && !previousPauseCheck;
        }

        private static void AdvanceFrame() {
            if (elapsedFrames < activeTas.TotalFrames) {
                frameIndex++;
                elapsedFrames++;
                if (frameIndex >= CurrentInput.Frames) {
                    inputIndex++;
                    frameIndex = 0;
                }
            }
        }

        private static void ApplyTasBindings() {
            DynamicData settingsData = DynamicData.For(Settings.Instance);
            Dictionary<string, Binding> vanillaBinds = new();
            foreach (string name in TasBinds.Keys) {
                vanillaBinds[name] = settingsData.Get<Binding>(name);
                settingsData.Set(name, TasBinds[name]);
            }

            Input.Initialize();

            foreach (string name in vanillaBinds.Keys) {
                settingsData.Set(name, vanillaBinds[name]);
            }
        }

        private static Binding CreateBinding(params Buttons[] buttons) {
            Binding binding = new();
            binding.Add(buttons);
            return binding;
        }

        private static bool ParseInput(string line, out TasInput input) {
            input = default;
            if (!InputPattern.IsMatch(line)) {
                return false;
            }

            int idx = line.IndexOf(',');
            if (idx != -1) {
                int frames = int.Parse(line.Substring(0, idx));

                Buttons buttons = 0;
                Vector2 feather = Vector2.Zero;
                for (int i = idx + 1; i < line.Length; i += 2) {
                    char button = char.ToUpper(line[i]);

                    if (button == 'F') {
                        float angle = Calc.ToRad(float.Parse(line.Substring(i + 2)));
                        feather = new((float) Math.Sin(angle), (float) Math.Cos(angle));
                        break;
                    }

                    buttons |= button switch {
                        'L' => Left,
                        'R' => Right,
                        'D' => Down,
                        'U' => Up,
                        'G' => Grab,
                        'J' => Jump,
                        'K' => Jump2,
                        'X' => Dash,
                        'C' => Dash2,
                        'Z' => DemoDash,
                        'V' => DemoDash2,
                        _ => 0
                    };
                }

                GamePadState state = new(feather, Vector2.Zero, 0f, 0f, buttons);
                input = new TasInput(state, frames);
            } else {
                input = new TasInput(default, int.Parse(line));
            }

            return true;
        }

        private readonly static Action consoleLoad = () => {
            // "Real" TASes need extra waiting frames to account for spawn time (1f) and the respawn animation (36f)
            // Skipping 37f here lets us avoid having to manually edit TAS files before they can be used as playbacks
            for (int i = 0; i < 37; i++) {
                AdvanceFrame();
            }
        };

        private static bool ParseCommand(string line, out Action command) {
            command = null;

            if (line.StartsWith("console load")) {
                command = consoleLoad;
                return true;
            } else if (line.StartsWith("start animation")) {
                string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 3) {
                    command = () => {
                        if (Engine.Scene.Tracker.GetEntity<Player>() is Player player) {
                            player.StateMachine.State = Player.StDummy;
                            player.DummyAutoAnimate = false;
                            player.Sprite.Play(split[2]);
                        }
                    };

                    return true;
                }
            } else if (line == "stop animation") {
                command = () => {
                    if (Engine.Scene.Tracker.GetEntity<Player>() is Player player) {
                        player.StateMachine.State = Player.StNormal;
                    }
                };

                return true;
            }

            return false;
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Level.EndPauseEffects += Level_EndPauseEffects;
            On.Monocle.MInput.Update += MInput_Update;
            On.Monocle.MInput.GamePadData.Rumble += GamePadData_Rumble;
        }

        internal static void Unload() {
            On.Celeste.Level.EndPauseEffects -= Level_EndPauseEffects;
            On.Monocle.MInput.Update -= MInput_Update;
            On.Monocle.MInput.GamePadData.Rumble -= GamePadData_Rumble;
        }

        private static void Level_EndPauseEffects(On.Celeste.Level.orig_EndPauseEffects orig, Level self) {
            orig(self);
            if (Active) {
                Resume();
            }
        }

        private static void MInput_Update(On.Monocle.MInput.orig_Update orig) {
            if (Active) {
                if (Engine.Scene is Level level && elapsedFrames < activeTas.TotalFrames) {
                    if (!Paused) {
                        if (level.CanPause && PausePressed()) {
                            Pause();
                        } else {
                            if (activeTas.Commands.ContainsKey(elapsedFrames)) {
                                foreach (Action action in activeTas.Commands[elapsedFrames]) {
                                    action.Invoke();
                                }
                            }
                            SetInputs();
                            AdvanceFrame();
                            return;
                        }
                    }
                } else {
                    Disable();
                }
            }

            orig();
        }

        private static void GamePadData_Rumble(On.Monocle.MInput.GamePadData.orig_Rumble orig, MInput.GamePadData self, float strength, float time) {
            if (Active) {
                return;
            }

            orig(self, strength, time);
        }

        #endregion Hooks

        public class TasFile {
            public List<TasInput> Inputs;
            public Dictionary<int, List<Action>> Commands;
            public int TotalFrames;

            public TasFile(List<TasInput> inputs, Dictionary<int, List<Action>> commands) {
                Inputs = inputs;
                Commands = commands;
                TotalFrames = GetTotalFrames();
            }

            private int GetTotalFrames() {
                int totalFrames = Inputs.Sum(i => i.Frames);
                foreach (KeyValuePair<int, List<Action>> frameCommands in Commands) {
                    foreach (Action action in frameCommands.Value) {
                        if (action == consoleLoad) {
                            totalFrames -= 37;
                        }
                    }
                }

                return totalFrames;
            }
        }

        public struct TasInput {
            public GamePadState State;
            public int Frames;

            public TasInput(GamePadState state, int frames) {
                State = state;
                Frames = frames;
            }
        }
    }
}
