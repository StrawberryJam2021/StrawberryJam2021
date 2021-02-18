using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked(false)]
    public class ClassicZoneController : Entity {
        private static ClassicZoneController _instance;
        private bool PlayerInZone { get; set; }
        public bool SkipFrame { get; private set; }

        public ClassicZoneController() : base() {
            Tag = Tags.Global | Tags.PauseUpdate;
            _instance = this;
        }

        public override void Update() {
            base.Update();
            SkipFrame = !SkipFrame;
        }

        public static void Load() {
            On.Celeste.Player.Update += OnPlayerUpdate;
            On.Celeste.Player.Render += OnPlayerRender;
        }

        public static void Unload() {
            On.Celeste.Player.Update -= OnPlayerUpdate;
            On.Celeste.Player.Render -= OnPlayerRender;
        }

        private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
            if (_instance == null) {
                return;
            }
            
            _instance.PlayerInZone = self.CollideCheck<ClassicZone>();

            if (!_instance.PlayerInZone) {
                orig(self);
                return;
            }

            if (_instance.SkipFrame) {
                return;
            }
            
        }

        private static void OnPlayerRender(On.Celeste.Player.orig_Render orig, Player self) {
            if (_instance == null) {
                return;
            }

            if (!_instance.PlayerInZone) {
                orig(self);
                return;
            }
            
        }
    }
}