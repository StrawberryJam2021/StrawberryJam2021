using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ResettingRefill")]
    [Tracked]
    public class ResettingRefill : Refill {
        private readonly int dashes;
        private readonly bool extraJump;
        private readonly bool persistJump;

        private static readonly JumpCount JumpCountVariant = ExtendedVariantsModule.Instance.VariantHandlers[ExtendedVariantsModule.Variant.JumpCount] as JumpCount;

        public ResettingRefill(Vector2 position, int dashes, bool extraJump, bool persistJump, bool oneUse)
            : base(position, dashes == 2, oneUse) {
            Remove(Components.Get<PlayerCollider>());

            if (extraJump) {
                string texture = persistJump ? "ExtendedVariantMode/jumprefill" : "ExtendedVariantMode/jumprefillblue";
                outline.Texture = GFX.Game[$"objects/{texture}/outline"];

                sprite.Reset(GFX.Game, $"objects/{texture}/idle");
                sprite.AddLoop("idle", "", 0.1f);
                sprite.Play("idle");

                flash.Reset(GFX.Game, $"objects/{texture}/flash");
                flash.Add("flash", "", 0.05f);
                flash.OnFinish = delegate {
                    flash.Visible = false;
                };
            }

            Add(new PlayerCollider(OnPlayerRR));

            this.dashes = dashes;
            this.extraJump = extraJump;
            this.persistJump = persistJump;
            this.oneUse = oneUse;
        }

        public ResettingRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int(nameof(dashes)), data.Bool(nameof(extraJump)),
                data.Bool(nameof(persistJump)), data.Bool(nameof(oneUse))) {
        }

        private void OnPlayerRR(Player player) {
            var session = SceneAs<Level>().Session;
            session.Inventory.Dashes = dashes;
            player.Dashes = dashes;

            ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(
                ExtendedVariantsModule.Variant.JumpCount,
                persistJump ? 2 : 1,
                false, false, false, true
            );

            if (extraJump)
                JumpCountVariant.AddJumps(1, true, 1);
            else
                JumpCountVariant.AddJumps(0, true, 0);

            player.RefillStamina();

            // Everything after this line is roundabout ways of doing the same things Refill does
            if (dashes is 0 or 1) {
                Audio.Play("event:/game/general/diamond_touch");
            } else if (dashes is 2) {
                Audio.Play("event:/new_content/game/10_farewell/pinkdiamond_touch");
            }

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine(RefillRoutine(player)));
            respawnTimer = 2.5f;
        }

        // Keep the player's hair color as blue when they touch the ground with 0 max dashes
        private static void OnUpdateHair(On.Celeste.Player.orig_UpdateHair orig, Player self, bool gravity) {
            orig(self, gravity);

            if (self.Scene.Tracker.GetEntity<ResettingRefill>() == null)
                return;

            if (self.Dashes == 0 && self.lastDashes == self.Dashes && self.hairFlashTimer <= 0.0) {
                self.Hair.Color = Player.UsedHairColor;
            }
        }

        public static void Load() {
            On.Celeste.Player.UpdateHair += OnUpdateHair;
        }

        public static void Unload() {
            On.Celeste.Player.UpdateHair -= OnUpdateHair;
        }
    }
}
