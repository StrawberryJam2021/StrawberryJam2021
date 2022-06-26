namespace Celeste.Mod.StrawberryJam2021 {
    public class StrawberryJam2021Settings : EverestModuleSettings {
        public bool DisplayDashSequence { get; set; } = false;

        [DefaultButtonBinding(Buttons.Back, Keys.Tab)]
        public ButtonBinding TogglePlaybacks { get; set; } = new ButtonBinding();
    }
}
