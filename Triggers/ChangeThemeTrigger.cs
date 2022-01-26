using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/ChangeThemeTrigger")]
    public class ChangeThemeTrigger : Trigger {
        private readonly bool enableMode;
        private readonly bool toggleMode;
        private readonly string[] gymSids = { 
            "StrawberryJam2021/0-Gyms/1-Beginner", 
            "StrawberryJam2021/0-Gyms/2-Intermediate", 
            "StrawberryJam2021/0-Gyms/3-Advanced", 
            "StrawberryJam2021/0-Gyms/4-Expert",
            "StrawberryJam2021/0-Gyms/5-Grandmaster",
            "StrawberryJam2021/0-Gyms/6-Library"
        };

        public ChangeThemeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            enableMode = data.Bool("enable", false);
            toggleMode = data.Bool("toggle", false);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            ApplyTheme();
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            Level level = Scene as Level;
            string sid = level.Session.Area.GetSID();
            bool enabled = StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Contains(sid);

            if (toggleMode) {
                if (enabled) {
                    RemoveMap(sid);
                }
                else {
                    AddMap(sid);
                }
            }
            else if (enableMode) {
                AddMap(sid);
            } 
            else {
                RemoveMap(sid);
            }

            ApplyTheme();
        }

        private void AddMap(string sid) {
            if (gymSids.Contains(sid)) {
                foreach (string gymSid in gymSids) {
                    StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Add(gymSid);
                }
            }
            else {
                StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Add(sid);
            }
        }

        private void RemoveMap(string sid) {
            if (gymSids.Contains(sid)) {
                foreach (string gymSid in gymSids) {
                    StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Remove(gymSid);
                }
            } else {
                StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Remove(sid);
            }
        }

        private void ApplyTheme() {
            Level level = Scene as Level;
            string sid = level.Session.Area.GetSID();
            bool enabled = StrawberryJam2021Module.SaveData.ModifiedThemeMaps.Contains(sid);

            switch (sid) {
                case "StrawberryJam2021/0-Gyms/1-Beginner":
                case "StrawberryJam2021/0-Gyms/2-Intermediate":
                case "StrawberryJam2021/0-Gyms/3-Advanced":
                case "StrawberryJam2021/0-Gyms/4-Expert":
                case "StrawberryJam2021/0-Gyms/5-Grandmaster":
                case "StrawberryJam2021/0-Gyms/6-Library":
                    // Light Mode alternate
                    level.Session.SetFlag("lightMode", enabled);
                    break;
                case "StrawberryJam2021/3-Advanced/Citrea":
                    // Dark Mode alternate
                    SetLighting(level, enabled ? 0.7f : 0f);
                    ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(ExtendedVariantsModule.Variant.BackgroundBrightness, enabled ? 3 : 10, revertOnLeave: false, isFade: false, revertOnDeath: false);
                    break;
                case "StrawberryJam2021/4-Expert/Skunkynator":
                    // Low-Detail Mode alternate
                    level.SnapColorGrade(enabled ? "none" : "SJ2021/Skunkynator/thereisaoddglitch");
                    level.Session.SetFlag("purplesunsetfade", enabled);
                    SetBloom(level, enabled ? 0f : 1.1f);
                    break;
                case "StrawberryJam2021/4-Expert/Yoshachobi7":
                    // Dark Mode alternate
                    level.Session.SetFlag("lightmode", !enabled);
                    SetLighting(level, enabled ? 0.25f : 0f);
                    ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(ExtendedVariantsModule.Variant.BackgroundBrightness, enabled ? 8 : 10, revertOnLeave: false, isFade: false, revertOnDeath: false);
                    break;
            } 
        }

        private void SetBloom(Level level, float bloomAdd) {
            level.Session.BloomBaseAdd = bloomAdd;
            level.Bloom.Base = AreaData.Get(level).BloomBase + bloomAdd;
        }

        private void SetLighting(Level level, float lightingAdd) {
            level.Session.LightingAlphaAdd = lightingAdd;
            level.Lighting.Alpha = level.BaseLightingAlpha + level.Session.LightingAlphaAdd;
        }
    }
}