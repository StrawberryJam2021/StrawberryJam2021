using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /// <summary>
    /// Entity that emits a flickering laser beam.
    /// </summary>
    /// <remarks>
    /// Opacity of the beam edges is calculated as <see cref="Alpha"/> times a dynamic multiplier, where
    /// the multiplier represents an optional flickering based on a <see cref="SineWave"/>.
    /// The centre third of the beam is twice that opacity.<br/>
    ///
    /// Configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>alpha</term><description>
    /// The base alpha value for the beam.
    /// Defaults to 0.4 (40%).
    /// </description></item>
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>color</term><description>
    /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>colorChannel</term><description>
    /// The color hex code used to match lasers with LinkedZipMovers.
    /// </description></item>
    /// <item><term>disableLasers</term><description>
    /// Whether or not colliding with this beam will disable all beams of the same color.
    /// Defaults to false.
    /// </description></item>
    /// <item><term>flicker</term><description>
    /// Whether or not the beam should flicker.
    /// Defaults to true, flickering 4 times per second.
    /// </description></item>
    /// <item><term>killPlayer</term><description>
    /// Whether or not colliding with the beam will kill the player.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>thickness</term><description>
    /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
    /// Defaults to 6 pixels.
    /// </description></item>
    /// <item><term>triggerZipMovers</term><description>
    /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
    /// Defaults to false.
    /// </description></item>
    /// </list>
    /// </remarks>
    [CustomEntity("SJ2021/LaserEmitterUp = LoadUp",
        "SJ2021/LaserEmitterDown = LoadDown",
        "SJ2021/LaserEmitterLeft = LoadLeft",
        "SJ2021/LaserEmitterRight = LoadRight")]
    public class LaserEmitter : OrientableEntity {
        #region Static Loader Methods

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Up);

        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Down);

        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Left);

        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new LaserEmitter(data, offset, Orientations.Right);

        #endregion

        #region Properties

        /// <summary>
        /// The base alpha value for the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to 0.4 (40%).
        /// </remarks>
        public float Alpha { get; }

        /// <summary>
        /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool CollideWithSolids { get; }

        /// <summary>
        /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
        /// </summary>
        /// <remarks>
        /// If not set, will attempt to use the value of <see cref="ColorChannel"/>,
        /// otherwise defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public Color Color { get; protected set; }

        /// <summary>
        /// The color hex code used to match lasers with LinkedZipMovers.
        /// </summary>
        /// <remarks>
        /// If not set, will use the hex representation of the current <see cref="Color"/>.
        /// </remarks>
        public string ColorChannel { get; }

        /// <summary>
        /// Whether or not colliding with this beam will disable all beams of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableLasers { get; }

        /// <summary>
        /// Whether or not the beam should flicker.
        /// </summary>
        /// <remarks>
        /// Defaults to true, flickering 4 times per second.
        /// </remarks>
        public bool Flicker { get; }

        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; }

        /// <summary>
        /// The rendering style to use.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="EmitterStyle.Rounded"/>.
        /// </remarks>
        public EmitterStyle Style { get; }

        /// <summary>
        /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
        /// </summary>
        /// <remarks>
        /// Defaults to 6 pixels.
        /// </remarks>
        public float Thickness { get; }

        /// <summary>
        /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool TriggerZipMovers { get; }

        #endregion

        #region Private Fields

        private const float flickerFrequency = 4f;
        private const float beamFlickerRange = 0.25f;
        private const float emitterFlickerRange = 0.15f;

        private float sineValue;

        private readonly Sprite emitterSprite;
        private readonly Sprite tintSprite;

        #endregion

        private static void setLaserSyncFlag(string colorChannel, bool value) =>
            (Engine.Scene as Level)?.Session.SetFlag($"ZipMoverSyncLaser:{colorChannel.ToLower()}", value);

        private static bool getLaserSyncFlag(string colorChannel) =>
            (Engine.Scene as Level)?.Session.GetFlag($"ZipMoverSyncLaser:{colorChannel.ToLower()}") ?? false;

        public LaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            string colorString = data.Attr("color", null);
            string colorChannelString = data.Attr("colorChannel", null);
            colorString ??= colorChannelString ?? "ff0000";
            colorChannelString ??= colorString;

            Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Color = Calc.HexToColor(colorString.ToLower());
            ColorChannel = colorChannelString.ToLower();
            DisableLasers = data.Bool("disableLasers");
            Flicker = data.Bool("flicker", true);
            KillPlayer = data.Bool("killPlayer", true);
            Style = data.Enum("style", EmitterStyle.Rounded);
            Thickness = Math.Max(data.Float("thickness", 6f), 0f);
            TriggerZipMovers = data.Bool("triggerZipMovers");

            Add(new PlayerCollider(onPlayerCollide),
                new LaserColliderComponent {CollideWithSolids = CollideWithSolids, Thickness = Thickness,},
                new SineWave(flickerFrequency) {OnUpdate = v => sineValue = v},
                new LedgeBlocker(_ => KillPlayer)
            );

            var laserCollider = Get<LaserColliderComponent>();
            Collider = laserCollider.Collider;

            if (Style == EmitterStyle.Simple) {
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
                emitterSprite.Play("simple");
                emitterSprite.Rotation = Orientation.Angle();
                Add(emitterSprite);
            } else if (Style == EmitterStyle.Rounded) {
                emitterSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
                emitterSprite.Play("rounded_base");
                emitterSprite.Rotation = Orientation.Angle();
                Add(emitterSprite);

                tintSprite = StrawberryJam2021Module.SpriteBank.Create("laserEmitter");
                tintSprite.Play("rounded_tint");
                tintSprite.Color = Color;
                tintSprite.Rotation = Orientation.Angle();
                Add(tintSprite);
            }

            Get<StaticMover>().OnMove = v => {
                Position += v;
                laserCollider.UpdateBeam();
            };
        }

        public override void Render() {
            // only render beam if we're collidable
            if (Collidable) {
                float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * beamFlickerRange;
                var color = Color * Alpha * (Flicker ? alphaMultiplier : 1f);

                Draw.Rect(Collider.Bounds, color);

                Vector2 target = Orientation switch {
                    Orientations.Up => Collider.TopCenter,
                    Orientations.Down => Collider.BottomCenter,
                    Orientations.Left => Collider.CenterLeft,
                    Orientations.Right => Collider.CenterRight,
                    _ => Vector2.Zero
                };

                float lineThickness = Orientation == Orientations.Left || Orientation == Orientations.Right
                    ? Collider.Height / 3f
                    : Collider.Width / 3f;

                Draw.Line(X, Y, X + target.X, Y + target.Y, color, lineThickness);
            }

            // update tint layer based on multiplier and collision
            if (tintSprite != null) {
                Color color;
                if (!Collidable)
                    color = Color.Gray;
                else {
                    float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * emitterFlickerRange;
                    color = Color * (Flicker ? alphaMultiplier : 1f);
                }
                color.A = 255;
                tintSprite.Color = color;
            }

            // render the emitter etc. after the beam
            base.Render();
        }

        private void onPlayerCollide(Player player) {
            var level = player.SceneAs<Level>();

            if (DisableLasers) {
                level.Entities.With<LaserEmitter>(emitter => {
                    if (emitter.ColorChannel == ColorChannel)
                        emitter.Collidable = false;
                });
            }

            if (TriggerZipMovers) {
                setLaserSyncFlag(ColorChannel, true);
            }

            if (KillPlayer) {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction);
            }
        }

        public enum EmitterStyle {
            Simple,
            Rounded,
        }

        private const string linkedZipMoverTypeName = "Celeste.Mod.AdventureHelper.Entities.LinkedZipMover";
        private const string linkedZipMoverNoReturnTypeName = "Celeste.Mod.AdventureHelper.Entities.LinkedZipMoverNoReturn";
        private const string zipMoverSoundControllerTypeName = "Celeste.Mod.AdventureHelper.Entities.ZipMoverSoundController";

        private static readonly Type linkedZipMoverType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "AdventureHelper")!
            .GetType().Assembly.GetType(linkedZipMoverTypeName);
        private static readonly Type linkedZipMoverNoReturnType = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == "AdventureHelper")!
            .GetType().Assembly.GetType(linkedZipMoverNoReturnTypeName);

        private static readonly MethodInfo linkedZipMoverSequence = linkedZipMoverType.GetMethod("Sequence", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        private static readonly PropertyInfo linkedZipMoverColorCode = linkedZipMoverType.GetProperty("ColorCode", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo linkedZipMoverNoReturnSequence = linkedZipMoverNoReturnType.GetMethod("Sequence", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        private static readonly PropertyInfo linkedZipMoverNoReturnColorCode = linkedZipMoverNoReturnType.GetProperty("ColorCode", BindingFlags.Public | BindingFlags.Instance);

        private static ILHook linkedZipMoverHook;
        private static ILHook linkedZipMoverNoReturnHook;

        public static void Load() {
            linkedZipMoverHook = new ILHook(linkedZipMoverSequence, LinkedZipMover_Sequence);
            linkedZipMoverNoReturnHook = new ILHook(linkedZipMoverNoReturnSequence, LinkedZipMoverNoReturn_Sequence);
        }

        public static void Unload() {
            linkedZipMoverHook?.Dispose();
            linkedZipMoverHook = null;
            linkedZipMoverNoReturnHook?.Dispose();
            linkedZipMoverNoReturnHook = null;
        }

        private static void LinkedZipMover_Sequence(ILContext il) {
            var cursor = new ILCursor(il);

            // find the HasPlayerRider check
            cursor.GotoNext(instr => instr.MatchCallvirt<Solid>(nameof(Solid.HasPlayerRider)));

            // emit flag check
            cursor.EmitDelegate<Func<Solid, bool>>(self =>
                self.HasPlayerRider() || getLaserSyncFlag((string) linkedZipMoverColorCode.GetValue(self)));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // find code near the end of the loop
            cursor.GotoNext(instr => instr.MatchCall(zipMoverSoundControllerTypeName, "StopSound"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchCallvirt(linkedZipMoverTypeName, "get_ColorCode"));

            // emit clear flag
            cursor.EmitDelegate<Func<string, string>>(colorCode => {
                setLaserSyncFlag(colorCode, false);
                return colorCode;
            });
        }

        private static void LinkedZipMoverNoReturn_Sequence(ILContext il) {
            var cursor = new ILCursor(il);

            // find the HasPlayerRider check
            cursor.GotoNext(instr => instr.MatchCallvirt<Solid>(nameof(Solid.HasPlayerRider)));

            // emit flag check
            cursor.EmitDelegate<Func<Solid, bool>>(self =>
                self.HasPlayerRider() || getLaserSyncFlag((string) linkedZipMoverNoReturnColorCode.GetValue(self)));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // find code near the end of the loop
            cursor.GotoNext(instr => instr.MatchCall(zipMoverSoundControllerTypeName, "StopSound"));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchCallvirt(linkedZipMoverNoReturnTypeName, "get_ColorCode"));

            // emit clear flag
            cursor.EmitDelegate<Func<string, string>>(colorCode => {
                setLaserSyncFlag(colorCode, false);
                return colorCode;
            });
        }
    }
}