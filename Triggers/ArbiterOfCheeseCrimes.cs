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

        public ArbiterOfCheeseCrimes(EntityData data, Vector2 offset)
            : base(data, offset) { }

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

            private void TryBind(string modName, string typename, string methodName, BindingFlags flags, string targetMethod, BindingFlags targetMethodFlags, HookConfig cfg) {
                try {
                    var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == modName);
                    var type = mod?.GetType().Assembly.GetType(typename);

                    var hookedMethod = type?.GetMethod(methodName, flags);

                    var target = GetType().GetMethod(targetMethod, targetMethodFlags);
                    hooks.Add(new Hook(hookedMethod, target, cfg));

                } catch (Exception) {
                    Logger.Log(LogLevel.Error, nameof(StrawberryJam2021Module), $"Exception loading mod support for {modName}.");
                }
            }

            internal void Load() {
                TryBind("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.StateManager", 
                    "LoadState", BindingFlags.Instance | BindingFlags.NonPublic,
                    nameof(SpeedrunTool_LoadState), BindingFlags.Static | BindingFlags.NonPublic,
                    new HookConfig {After = new[] {"*"}});

                TryBind("DJMapHelper", "Celeste.Mod.DJMapHelper.DebugFeatures.LookoutBuilder", 
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
                        // TODO Just punishment
                        return false;
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