using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    public class CassetteListener : Component {
        public static Color ColorFromCassetteIndex(int index) => index switch {
            0 => Calc.HexToColor("49aaf0"),
            1 => Calc.HexToColor("f049be"),
            2 => Calc.HexToColor("fcdc3a"),
            3 => Calc.HexToColor("38e04e"),
            _ => Color.White
        };
        
        public Action OnFinish;
        public Action OnWillToggle;
        public Action OnActivated;
        public Action OnDeactivated;
        public Action<bool> OnSilentUpdate;
        public Action<int, bool> OnTick;
        
        public int Index;
        public bool Activated;
        
        private CassetteBlockManager cassetteBlockManager;

        public CassetteListener(int index) : base(false, false) {
            Index = index;
        }

        public override void EntityAdded(Scene scene) {
            base.EntityAdded(scene);

            if (!(scene is Level level)) return;
            level.HasCassetteBlocks = true;

            cassetteBlockManager = scene.Tracker.GetEntity<CassetteBlockManager>() ?? scene.Entities.ToAdd.OfType<CassetteBlockManager>().FirstOrDefault();
            if (cassetteBlockManager == null)
                scene.Add(cassetteBlockManager = new CassetteBlockManager());
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            cassetteBlockManager = null;
        }
        
        public static void Load() {
            IL.Celeste.CassetteBlockManager.AdvanceMusic += CassetteBlockManager_AdvanceMusic;
            On.Celeste.CassetteBlockManager.StopBlocks += CassetteBlockManager_StopBlocks;
            On.Celeste.CassetteBlockManager.SilentUpdateBlocks += CassetteBlockManager_SilentUpdateBlocks;
            On.Celeste.CassetteBlockManager.SetActiveIndex += CassetteBlockManager_SetActiveIndex;
            On.Celeste.CassetteBlockManager.SetWillActivate += CassetteBlockManager_SetWillActivate;
        }

        public static void Unload() {
            IL.Celeste.CassetteBlockManager.AdvanceMusic -= CassetteBlockManager_AdvanceMusic;
            On.Celeste.CassetteBlockManager.StopBlocks -= CassetteBlockManager_StopBlocks;
            On.Celeste.CassetteBlockManager.SilentUpdateBlocks -= CassetteBlockManager_SilentUpdateBlocks;
            On.Celeste.CassetteBlockManager.SetActiveIndex -= CassetteBlockManager_SetActiveIndex;
            On.Celeste.CassetteBlockManager.SetWillActivate -= CassetteBlockManager_SetWillActivate;
        }

        private static void CassetteBlockManager_AdvanceMusic(ILContext il) {
            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.AfterLabel,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<CassetteBlockManager>("leadBeats"),
                instr => instr.MatchLdcI4(0))) {
                cursor.Emit(OpCodes.Ldarg_0);
                
                cursor.EmitDelegate<Action<CassetteBlockManager>>(self => {
                    var data = DynamicData.For(self);
                    var beatIndex = data.Get<int>("beatIndex");
                    var beatsPerTick = data.Get<int>("beatsPerTick");
                    var ticksPerSwap = data.Get<int>("ticksPerSwap");
                    var currentIndex = data.Get<int>("currentIndex");
                    if (beatIndex % beatsPerTick == 0 &&
                        beatIndex % (beatsPerTick * ticksPerSwap) != 0 &&
                        self.Scene is not null) {
                        var components = self.Scene.Tracker.GetComponents<CassetteListener>();
                        foreach (CassetteListener component in components) {
                            component.OnTick?.Invoke(currentIndex, false);
                        }
                    }
                });
            }
        }
        
        private static void CassetteBlockManager_StopBlocks(On.Celeste.CassetteBlockManager.orig_StopBlocks orig, CassetteBlockManager self) {
            orig(self);
            if (self.Scene == null) return;
            var components = self.Scene.Tracker.GetComponents<CassetteListener>();
            foreach (CassetteListener component in components) {
                component.OnFinish?.Invoke();
            }
        }
        
        private static void CassetteBlockManager_SilentUpdateBlocks(On.Celeste.CassetteBlockManager.orig_SilentUpdateBlocks orig, CassetteBlockManager self) {
            orig(self);
            if (self.Scene == null) return;
            
            var data = DynamicData.For(self);
            var currentIndex = data.Get<int>("currentIndex");
            var components = self.Scene.Tracker.GetComponents<CassetteListener>();
            foreach (CassetteListener component in components) {
                component.OnSilentUpdate?.Invoke(component.Index == currentIndex);
            }
        }
        
        private static void CassetteBlockManager_SetActiveIndex(On.Celeste.CassetteBlockManager.orig_SetActiveIndex orig, CassetteBlockManager self, int index) {
            orig(self, index);
            if (self.Scene == null) return;
            
            var components = self.Scene.Tracker.GetComponents<CassetteListener>();
            foreach (CassetteListener component in components) {
                if (component.Activated && component.Index != index) {
                    component.Activated = false;
                    component.OnDeactivated?.Invoke();
                } else if (!component.Activated && component.Index == index) {
                    component.Activated = true;
                    component.OnActivated?.Invoke();
                }
                
                component.OnTick?.Invoke(index, true);
            }
        }
        
        private static void CassetteBlockManager_SetWillActivate(On.Celeste.CassetteBlockManager.orig_SetWillActivate orig, CassetteBlockManager self, int index) {
            orig(self, index);
            if (self.Scene == null) return;
            
            var components = self.Scene.Tracker.GetComponents<CassetteListener>();
            foreach (CassetteListener component in components) {
                if (component.Index == index || component.Activated) {
                    component.OnWillToggle?.Invoke();
                }
            }
        }
    }
}
