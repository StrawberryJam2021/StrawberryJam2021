using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/ArbiterOfCheeseCrimes")]
    [Tracked]
    public class ArbiterOfCheeseCrimes : Trigger {

        public ArbiterOfCheeseCrimes(EntityData data, Vector2 offset) : base(data, offset) { }

        public static void Load() {
            ModSupport.Instance.Load();

            Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
        }

        public static void Unload() {
            ModSupport.Instance.Unload();
            
            Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
        }

        private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal) {
            TextMenu.Item item = menu.GetItems().Find(i =>
                i.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button) i).Label == Dialog.Clean("menu_pause_resume"));

            if (item == null) {
                return;
            }

            var controllers = level.Tracker.GetEntities<ArbiterOfCheeseCrimes>().Cast<ArbiterOfCheeseCrimes>();

            if (controllers.Any(e => e.PlayerIsInside)) {
                menu.Remove(item);

                menu.OnESC = (menu.OnCancel = (menu.OnPause = delegate
                {
                    /* The Arbiter says:
                     Pausing the game is fine. It's just a game, life happens.
                     At any point, your dog may deposit their latest object of pride
                     onto your carpet right outside of your room, and there is nothing
                     for you to do other than to pause the game to deal with it.
                     There are entirely valid reasons to pause the game in the middle of a room,
                     but this is an unfair world, filled with malice and evil.
                     There will be those who try to abuse this mechanic,
                     intended to provide brief respite from the intensity of gameplay,
                     allowing the player to deal with other parts of their life for a moment.
                     The pause menu shall not be taken away. However, this sacred place must be provided
                     protection from those with evil in their soul. And those with weak hearts
                     shall be provided another bulwark against their own weakness,
                     which they shall have to overcome should they decide to give in and give up.
                     */

                    // No-Op :)
                }));
            }
        }

        public class ModSupport {
            internal static ModSupport Instance = new();

            private static List<IDetour> hooks = new();

            private void TryHookMethod(string modName, string typename, string methodName, BindingFlags flags, string targetMethod, BindingFlags targetMethodFlags, HookConfig cfg) {
                try {
                    var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == modName);

                    if (mod == null) {
                        return;
                    }

                    var type = mod?.GetType().Assembly.GetType(typename);

                    var hookedMethod = type?.GetMethod(methodName, flags);

                    var target = GetType().GetMethod(targetMethod, targetMethodFlags);
                    hooks.Add(new Hook(hookedMethod, target, cfg));

                } catch (Exception) {
                    Logger.Log(LogLevel.Error, nameof(StrawberryJam2021Module), $"Exception loading mod support for {modName}, method {methodName}.");
                }
            }

            private void TryHookPropertyGet(string modName, string typename, string propName, BindingFlags flags, string targetMethod, BindingFlags targetMethodFlags, bool nonPublicGet, HookConfig cfg) {
                try {
                    var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == modName);

                    if (mod == null) {
                        return;
                    }

                    var type = mod?.GetType().Assembly.GetType(typename);

                    var hookedMethod = type?.GetProperty(propName, flags)?.GetGetMethod(nonPublicGet);

                    var target = GetType().GetMethod(targetMethod, targetMethodFlags);
                    hooks.Add(new Hook(hookedMethod, target, cfg));

                } catch (Exception) {
                    Logger.Log(LogLevel.Error, nameof(StrawberryJam2021Module), $"Exception loading mod support for {modName}, property {propName}.");
                }
            }

            internal void Load() {
                TryHookMethod("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.StateManager", 
                    "LoadState", BindingFlags.Instance | BindingFlags.NonPublic,
                    nameof(SpeedrunTool_LoadState), BindingFlags.Static | BindingFlags.NonPublic,
                    new HookConfig {After = new[] {"*"}});
                TryHookMethod("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.StateManager", 
                    "SaveState", BindingFlags.Instance | BindingFlags.NonPublic,
                    nameof(SpeedrunTool_SaveState), BindingFlags.Static | BindingFlags.NonPublic,
                    new HookConfig {After = new[] {"*"}});
                TryHookPropertyGet("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SpeedrunToolSettings", 
                    "FreezeAfterLoadState", BindingFlags.Instance | BindingFlags.Public,
                    nameof(SpeedrunTool_FreezeAfterLoadState_Get), BindingFlags.Static | BindingFlags.NonPublic,
                    false, new HookConfig {After = new[] {"*"}});

                try {
                    var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "SpeedrunTool");

                    if (mod != null) {
                        var type = mod?.GetType().Assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager");

                        SpeedrunTool_ClearState =
                            type?.GetMethod("ClearState", BindingFlags.Instance | BindingFlags.Public);
                        SpeedrunTool_StateManagerInstance =
                            type?.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                    }
                } catch (Exception) {
                    Logger.Log(LogLevel.Error, nameof(StrawberryJam2021Module), $"Exception loading mod support for SpeedrunTool, getting the state manager.");
                }

                TryHookMethod("DJMapHelper", "Celeste.Mod.DJMapHelper.DebugFeatures.LookoutBuilder", 
                    "LevelOnUpdate", BindingFlags.Static | BindingFlags.NonPublic,
                    nameof(DJMapHelper_LevelOnUpdate), BindingFlags.Static | BindingFlags.NonPublic,
                    new HookConfig {After = new[] {"*"}});
            }

            internal void Unload() {
                foreach (IDetour hook in hooks) {
                    hook?.Dispose();
                }
                hooks.Clear();
            }

            private static bool SpeedrunTool_LoadState(Func<object, bool, bool> orig, object self, bool tas) {
                if (Engine.Scene is Level level) {
                    var controllers = level.Tracker.GetEntities<ArbiterOfCheeseCrimes>().Cast<ArbiterOfCheeseCrimes>();

                    if (controllers.Any(e => e.PlayerIsInside)) {
                        return false;
                    }
                }

                return orig(self, tas);
            }

            // This one is needed because the actual freeze after a state save is deeply nested inside SaveState()
            // in a part of code that frequently changes. Pretending this setting is unset while inside the trigger
            // is a much more stable solution, but this might have side effects.
            // It does not appear to permanently change any mod options, at least.
            private static bool SpeedrunTool_FreezeAfterLoadState_Get(Func<object, bool> orig, object self) {
                if (Engine.Scene is Level level) {
                    var controllers = level.Tracker.GetEntities<ArbiterOfCheeseCrimes>().Cast<ArbiterOfCheeseCrimes>();

                    if (controllers.Any(e => e.PlayerIsInside)) {
                        return false;
                    }
                }

                return orig(self);
            }

            private static MethodInfo SpeedrunTool_ClearState;
            private static PropertyInfo SpeedrunTool_StateManagerInstance;

            private static bool SpeedrunTool_SaveState(Func<object, bool, bool> orig, object self, bool tas) {
                if (Engine.Scene is Level level) {
                    var controllers = level.Tracker.GetEntities<ArbiterOfCheeseCrimes>().Cast<ArbiterOfCheeseCrimes>();

                    if (controllers.Any(e => e.PlayerIsInside)) {
                        bool res = orig(self, tas);

                        try {
                            SpeedrunTool_ClearState?.Invoke(SpeedrunTool_StateManagerInstance?.GetValue(null), new object[] { });
                        } catch (Exception) {
                            // Silently fail
                        }

                        return res;
                    }
                }

                return orig(self, tas);
            }

            private static void DJMapHelper_LevelOnUpdate(Action<object, Level> orig, On.Celeste.Level.orig_Update orig_orig, Level level) {
                var controllers = level.Tracker.GetEntities<ArbiterOfCheeseCrimes>().Cast<ArbiterOfCheeseCrimes>();

                if (controllers.Any(e => e.PlayerIsInside)) {
                    // TODO Just punishment
                    orig_orig(level);
                    return;
                }

                orig(orig_orig, level);
            }
        }
    }
}