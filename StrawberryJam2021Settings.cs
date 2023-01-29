using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Settings : EverestModuleSettings {
        [SettingName("StrawberryJam2021_DisplayDashSequence")]
        public bool DisplayDashSequence { get; set; } = false;

        [SettingName("StrawberryJam2021_TogglePlaybacks")]
        [DefaultButtonBinding(Buttons.Back, Keys.Tab)]
        public ButtonBinding TogglePlaybacks { get; set; }
    }
}
