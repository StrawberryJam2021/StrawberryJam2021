using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/SkateboardTrigger")]
    [Tracked]
    class SkateboardTrigger : Trigger {
        private static bool skateboardEnabled = false;
        private static Vector2 SkateboardSpriteOffset = new Vector2(0, -3);
        public static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.PlayerHair.Render += PlayerHair_Render;
        }

        public static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.PlayerHair.Render -= PlayerHair_Render;
        }

        static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            skateboardEnabled = false;
        }


        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self) {
            if (skateboardEnabled) {
                self.Sprite.RenderPosition += SkateboardSpriteOffset;
            }
            orig(self);
            if (skateboardEnabled) {
                self.Sprite.RenderPosition -= SkateboardSpriteOffset;
            }
        }

        static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            if (skateboardEnabled) {
                for (int i = 0; i < self.Nodes.Count; i++) {
                    self.Nodes[i] += SkateboardSpriteOffset;
                }
            }
            orig(self);
            if (skateboardEnabled) {
                for (int i = 0; i < self.Nodes.Count; i++) {
                    self.Nodes[i] -= SkateboardSpriteOffset;
                }
            }
        }


        enum TriggerMode {
            Enable,
            Disable,
            Toggle
        }
        private TriggerMode triggerMode;

        public SkateboardTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            triggerMode = data.Enum<TriggerMode>("mode", TriggerMode.Enable);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            switch (triggerMode) {
                case TriggerMode.Enable:
                    skateboardEnabled = true;
                    break;
                case TriggerMode.Disable:
                    skateboardEnabled = false;
                    break;
                case TriggerMode.Toggle:
                    skateboardEnabled = !skateboardEnabled;
                    break;
                default: break;
            }
        }
    }
}