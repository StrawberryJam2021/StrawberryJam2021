using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using On.Celeste;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/SkateboardTrigger")]
    [Tracked]
    class SkateboardTrigger : Trigger {
        /**
         * Static functions and hooks
         */       
        private static bool SkateboardEnabled = false;
        private static Vector2 PlayerSpriteOffset = new Vector2(0, -3);
        private static MTexture SkateboardSprite;

        public static void InitializeTextures() {
            SkateboardSprite = GFX.Game["objects/StrawberryJam2021/skateboard/skateboard"];
        }

        public static void Load() {
            On.Celeste.Level.Begin += Level_Begin;
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.PlayerHair.Render += PlayerHair_Render;
        }

        public static void Unload() {
            On.Celeste.Level.Begin -= Level_Begin;
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.PlayerHair.Render -= PlayerHair_Render;
        }
        private static void Level_Begin(On.Celeste.Level.orig_Begin orig, Level self) {
            orig(self);
            SkateboardEnabled = false;
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self) {
            if (SkateboardEnabled) {
                self.Sprite.RenderPosition += PlayerSpriteOffset;
            }
            orig(self);
            if (SkateboardEnabled) {
                self.Sprite.RenderPosition -= PlayerSpriteOffset;
                SkateboardSprite.Draw(
                    self.Sprite.RenderPosition + new Vector2(self.Facing == Facings.Left ? 8 : -8, -4),
                    Vector2.Zero, Color.White, new Vector2(self.Facing == Facings.Left ? -1 : 1, 1)
                );
            }
        }

        static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            if (SkateboardEnabled) {
                for (int i = 0; i < self.Nodes.Count; i++) {
                    self.Nodes[i] += PlayerSpriteOffset;
                }
            }
            orig(self);
            if (SkateboardEnabled) {
                for (int i = 0; i < self.Nodes.Count; i++) {
                    self.Nodes[i] -= PlayerSpriteOffset;
                }
            }
        }

        /**
         * Trigger instance implementation       
         */
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
                    SkateboardEnabled = true;
                    break;
                case TriggerMode.Disable:
                    SkateboardEnabled = false;
                    break;
                case TriggerMode.Toggle:
                    SkateboardEnabled = !SkateboardEnabled;
                    break;
                default: break;
            }
        }
    }
}
