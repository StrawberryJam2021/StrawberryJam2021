using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    // Adapted from CelesteTAS with permission from DemoJameson: https://github.com/EverestAPI/CelesteTAS-EverestInterop
    // Currently only supports input playback with a subset of gameplay-relevant actions (see original repo for full reference)
    public static class TasHelper {
        private static readonly Regex InputPattern = new(@"^\d+((,[LRDUGJKXCZV])*|(,F,\d+(\.\d+)?))$", RegexOptions.IgnoreCase);
        private static readonly FastReflectionDelegate MInput_UpdateVirtualInputs = typeof(MInput).GetMethod("UpdateVirtualInputs", BindingFlags.Static | BindingFlags.NonPublic).CreateFastDelegate();
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
            { "DemoDash", CreateBinding(DemoDash, DemoDash2) }
        };

        private static List<TasInput> inputs;
        private static GamePadState lastState;
        private static int inputIndex;
        private static int frameIndex;
        private static int elapsedFrames;
        private static int totalFrames;

        private static bool previousPauseCheck;
        private static bool currentPauseCheck;

        public static bool Active { get; private set; } = false;
        public static bool Paused { get; private set; } = false;

        public static void Play(List<TasInput> tasInputs) {
            if (tasInputs != null && tasInputs.Count > 0) {
                inputs = tasInputs;
                lastState = default;
                inputIndex = 0;
                frameIndex = 0;
                elapsedFrames = 0;
                totalFrames = inputs.Sum(i => i.Frames);

                // "Real" TASes need extra waiting frames to account for spawn time (1f) and the respawn animation (36f)
                // Skipping 37f here lets us avoid having to manually edit TAS files before they can be used as playbacks
                // TODO (?): would probably be better to only do this if we find a Console Load command
                while (elapsedFrames < 37 && inputIndex <= inputs.Count - 1) {
                    AdvanceFrame();
                }

                Enable();
            }
        }

        // TODO (?): buffer time may not match up exactly due to engine freeze
        public static IEnumerator Wait(float buffer = 0f) {
            int bufferFrames = (int) (buffer / Engine.DeltaTime);
            while (elapsedFrames < totalFrames - bufferFrames) {
                yield return null;
            }
        }

        public static bool TryParse(string path, out List<TasInput> inputs) {
            inputs = new List<TasInput>();
            if (Everest.Content.TryGet(path, out ModAsset tasFile)) {
                using StreamReader reader = new(tasFile.Stream);
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine().Trim();
                    if (InputPattern.IsMatch(line)) {
                        inputs.Add(ParseInput(line));
                    }
                }

                return true;
            }

            return false;
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
            gamePad.CurrentState = lastState = inputs[inputIndex].State;
            MInput_UpdateVirtualInputs.Invoke(null, null);
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
            frameIndex++;
            elapsedFrames++;
            if (frameIndex >= inputs[inputIndex].Frames) {
                inputIndex++;
                frameIndex = 0;
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

        private static TasInput ParseInput(string line) {
            int idx = line.IndexOf(',');
            if (idx != -1) {
                int frames = int.Parse(line.Substring(0, idx));

                Buttons buttons = 0;
                Vector2 feather = Vector2.Zero;
                for (int i = idx + 1; i < line.Length; i += 2) {
                    char input = char.ToUpper(line[i]);

                    if (input == 'F') {
                        float angle = Calc.ToRad(float.Parse(line.Substring(i + 2)));
                        feather = new((float) Math.Sin(angle), (float) Math.Cos(angle));
                        break;
                    }

                    buttons |= input switch {
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
                return new TasInput(state, frames);
            } else {
                return new TasInput(default, int.Parse(line));
            }
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Level.EndPauseEffects += Level_EndPauseEffects;
            On.Monocle.MInput.Update += MInput_Update;
        }

        internal static void Unload() {
            On.Celeste.Level.EndPauseEffects -= Level_EndPauseEffects;
            On.Monocle.MInput.Update -= MInput_Update;
        }

        private static void Level_EndPauseEffects(On.Celeste.Level.orig_EndPauseEffects orig, Level self) {
            if (Active) {
                Resume();
            }
        }

        private static void MInput_Update(On.Monocle.MInput.orig_Update orig) {
            if (Active) {
                if (Engine.Scene is Level level && inputIndex <= inputs.Count - 1) {
                    if (!Paused) {
                        if (level.CanPause && PausePressed()) {
                            Pause();
                        } else {
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
    }

    #endregion Hooks

    public struct TasInput {
        public GamePadState State;
        public int Frames;

        public TasInput(GamePadState state, int frames) {
            State = state;
            Frames = frames;
        }
    }
}
