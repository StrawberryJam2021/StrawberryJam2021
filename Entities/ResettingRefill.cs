using Celeste.Mod.Entities;
using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    // ReSharper disable PossibleNullReferenceException
    [CustomEntity("SJ2021/ResettingRefill")]
    public class ResettingRefill : Refill {
        private static bool _hooked;

        private readonly int dashes;
        private readonly bool extraJump;
        private readonly bool persistJump;
        private readonly bool oneUse;

        private readonly JumpCount jumpCountVariant = ExtendedVariantsModule.Instance.VariantHandlers[ExtendedVariantsModule.Variant.JumpCount] as JumpCount;

        // ReSharper disable once InconsistentNaming
        private readonly MethodInfo RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo respawnTimer = typeof(Refill).GetField("respawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        public ResettingRefill(Vector2 position, int dashes, bool extraJump, bool persistJump, bool oneUse)
            : base(position, dashes == 2, oneUse) {
            Remove(Components.Get<PlayerCollider>());

            if (extraJump) {
                string texture = persistJump ? "ExtendedVariantMode/jumprefill" : "ExtendedVariantMode/jumprefillblue";

                Remove(Components.Where(c =>
                        c.GetType() == typeof(Sprite) || c.GetType() == typeof(Image) || c.GetType() == typeof(Wiggler))
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

                Add(wiggler = Wiggler.Create(1f, 4f, delegate(float v) {
                    sprite.Scale = flash.Scale = Vector2.One * (1f + v * 0.2f);
                }));

                typeof(Refill).GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, sprite);
                typeof(Refill).GetField("outline", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, outline);
                typeof(Refill).GetField("flash", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, flash);
                typeof(Refill).GetField("wiggler", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, wiggler);
            }

            Add(new PlayerCollider(OnPlayer));

            if (!_hooked) {
                // Keep the player's hair color as blue when they touch the ground with 0 max dashes
                On.Celeste.Player.UpdateHair += (orig, self, gravity) => {
                    orig(self, gravity);
                    float hairFlashTimer = (float) self.GetType().GetField("hairFlashTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
                    int lastDashes = (int) self.GetType().GetField("lastDashes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
                    if (self.Dashes == 0 && lastDashes == self.Dashes && hairFlashTimer <= 0.0) {
                        self.Hair.Color = Player.UsedHairColor;
                    }
                };

                _hooked = true;
            }

            this.dashes = dashes;
            this.extraJump = extraJump;
            this.persistJump = persistJump;
            this.oneUse = oneUse;
        }

        public ResettingRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int(nameof(dashes)), data.Bool(nameof(extraJump)), data.Bool(nameof(persistJump)), data.Bool(nameof(oneUse))) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            //Scene.Add(new ResettingRefillShockwave(Position, (int) (Width + 4), (int) (Height + 4)));
        }

        private void OnPlayer(Player player) {
            var session = SceneAs<Level>().Session;
            session.Inventory.Dashes = dashes;
            player.Dashes = dashes;

            jumpCountVariant.SetValue(persistJump ? 2 : 1);

            if (extraJump)
                jumpCountVariant.AddJumps(1, true, 1);
            else
                jumpCountVariant.AddJumps(0, true, 0);

            player.RefillStamina();

            // Everything after this line is roundabout ways of doing the same things Refill does
            if (dashes == 0 || dashes == 1) {
                Audio.Play("event:/game/general/diamond_touch");
            } else if (dashes == 2) {
                Audio.Play("event:/new_content/game/10_farewell/pinkdiamond_touch");
            }

            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;

            Add(new Coroutine((IEnumerator) RefillRoutine.Invoke(this, new object[] {player})));
            respawnTimer.SetValue(this, 2.5f);
        }
    }
}
