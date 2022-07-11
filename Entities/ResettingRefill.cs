using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ResettingRefill")]
    [Tracked]
    public class ResettingRefill : Refill {
        private readonly int dashes;
        private readonly bool extraJump;
        private readonly bool persistJump;
        private readonly bool oneUse;

        private static readonly JumpCount JumpCountVariant = ExtendedVariantsModule.Instance.VariantHandlers[ExtendedVariantsModule.Variant.JumpCount] as JumpCount;

        private static readonly MethodInfo RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo respawnTimer = typeof(Refill).GetField("respawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo sprite = typeof(Refill).GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo outline = typeof(Refill).GetField("outline", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo flash = typeof(Refill).GetField("flash", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo wiggler = typeof(Refill).GetField("wiggler", BindingFlags.NonPublic | BindingFlags.Instance);

        public ResettingRefill(Vector2 position, int dashes, bool extraJump, bool persistJump, bool oneUse)
            : base(position, dashes == 2, oneUse) {
            Remove(Components.Get<PlayerCollider>());

            if (extraJump) {
                string texture = persistJump ? "ExtendedVariantMode/jumprefill" : "ExtendedVariantMode/jumprefillblue";

                Remove(Components.Where(c =>
                        c.GetType() == typeof(Sprite) ||
                        c.GetType() == typeof(Image) ||
                        c.GetType() == typeof(Wiggler))
                    .ToArray());

                Sprite sprite;
                Sprite flash;
                Image outline;
                Wiggler wiggler;

                Add(outline = new Image(GFX.Game[$"objects/{texture}/outline"]));
                outline.CenterOrigin();
                outline.Visible = false;

                Add(sprite = new Sprite(GFX.Game, $"objects/{texture}/idle"));
                sprite.AddLoop("idle", "", 0.1f);
                sprite.Play("idle");
                sprite.CenterOrigin();

                Add(flash = new Sprite(GFX.Game, $"objects/{texture}/flash"));
                flash.Add("flash", "", 0.05f);
                flash.OnFinish = delegate {
                    flash.Visible = false;
                };
                flash.CenterOrigin();

                Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v) {
                    sprite.Scale = flash.Scale = Vector2.One * (1f + v * 0.2f);
                }));

                ResettingRefill.sprite.SetValue(this, sprite);
                ResettingRefill.outline.SetValue(this, outline);
                ResettingRefill.flash.SetValue(this, flash);
                ResettingRefill.wiggler.SetValue(this, wiggler);
            }

            Add(new PlayerCollider(OnPlayer));

            this.dashes = dashes;
            this.extraJump = extraJump;
            this.persistJump = persistJump;
            this.oneUse = oneUse;
        }

        public ResettingRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int(nameof(dashes)), data.Bool(nameof(extraJump)),
                data.Bool(nameof(persistJump)), data.Bool(nameof(oneUse))) {
        }

        private void OnPlayer(Player player) {
            var session = SceneAs<Level>().Session;
            session.Inventory.Dashes = dashes;
            player.Dashes = dashes;

            ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(
                ExtendedVariantsModule.Variant.JumpCount,
                persistJump ? 2 : 1,
                false, false, false, true
            );

            if (extraJump) {
                JumpCountVariant.AddJumps(1, true, 1);
            } else {
                JumpCountVariant.AddJumps(0, true, 0);
            }

            player.RefillStamina();

            // Everything after this line is roundabout ways of doing the same things Refill does
            if (dashes is 0 or 1) {
                Audio.Play("event:/game/general/diamond_touch");
            } else if (dashes is 2) {
                Audio.Play("event:/new_content/game/10_farewell/pinkdiamond_touch");
            }

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine((IEnumerator) RefillRoutine.Invoke(this, new object[] { player })));
            respawnTimer.SetValue(this, 2.5f);
        }

        private static readonly FieldInfo hairFlashTimer = typeof(Player).GetField("hairFlashTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo lastDashes = typeof(Player).GetField("lastDashes", BindingFlags.NonPublic | BindingFlags.Instance);

        // Keep the player's hair color as blue when they touch the ground with 0 max dashes
        private static void OnUpdateHair(On.Celeste.Player.orig_UpdateHair orig, Player self, bool gravity) {
            orig(self, gravity);

            if (self.Scene.Tracker.GetEntity<ResettingRefill>() == null) {
                return;
            }

            float hairFlashTimer = (float) ResettingRefill.hairFlashTimer.GetValue(self);
            int lastDashes = (int) ResettingRefill.lastDashes.GetValue(self);
            if (self.Dashes == 0 && lastDashes == self.Dashes && hairFlashTimer <= 0.0) {
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
