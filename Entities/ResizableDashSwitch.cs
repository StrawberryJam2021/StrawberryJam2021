using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/ResizableDashSwitch")]
    public class ResizableDashSwitch : DashSwitch {

        #region Fields/Properties

        public Sides Side;
        public Switch Switch;

        public static ParticleType P_Fizzle;
        public static ParticleType P_Shatter;

        private DynamicData baseData;
        private Vector2 spriteOffset;
        private int width;
        private List<Image> switchImages = new();

        private int widthTiles => width / 8;

        private float SwitchRotation => Side switch
        {
            Sides.Down => 0f,
            Sides.Up => Calc.HalfCircle,
            Sides.Right => -Calc.QuarterCircle,
            Sides.Left => Calc.QuarterCircle,
            _ => throw new InvalidOperationException("Dash switch direction is invalid.")
        };

        #endregion

        #region Base class fields

        private Vector2 pressedTarget {
            get => baseData.Get<Vector2>("pressedTarget");
            set => baseData.Set("pressedTarget", value);
        }
        private float startY {
            get => baseData.Get<float>("startY");
            set => baseData.Set("startY", value);
        }
        private Sprite sprite {
            get => baseData.Get<Sprite>("sprite");
            set => baseData.Set("sprite", value);
        }
        private bool persistent {
            get => baseData.Get<bool>("persistent");
            set => baseData.Set("persistent", value);
        }
        private bool pressed {
            get => baseData.Get<bool>("pressed");
            set => baseData.Set("pressed", value);
        }
        private Vector2 pressDirection {
            get => baseData.Get<Vector2>("pressDirection");
            set => baseData.Set("pressDirection", value);
        }
        private string FlagName => baseData.Get<string>("FlagName");

        #endregion

        #region Constructor stuff

        public ResizableDashSwitch(Vector2 position, Sides side, bool persistent, EntityID id, int width, bool actLikeTouchSwitch, bool attachToSolid)
            : base(position, side, persistent, false, id, "default") {
            baseData = new DynamicData(typeof(DashSwitch), this);

            Side = side;
            this.width = width;
            if (side == Sides.Up || side == Sides.Down) {
                Collider.Width = width;
            } else {
                Collider.Height = width;
            }

            if (attachToSolid) {
                Add(new StaticMover {
                    SolidChecker = IsRiding,
                    OnMove = OnMove,
                    OnAttach = OnAttach,
                    OnShake = OnShake,
                });
            }
            if (actLikeTouchSwitch) {
                Add(Switch = new Switch(groundReset: false));
                Switch.OnStartFinished = () => {
                    // if these are set, in Awake() it'll start out already pressed
                    this.persistent = true;
                    SceneAs<Level>().Session.SetFlag(FlagName);
                };
            }

            SurfaceSoundIndex = SurfaceIndex.DreamBlockInactive;

            Remove(sprite);
            CreateSprite(0);
        }

        public ResizableDashSwitch(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, SwitchSide(data.Enum("orientation", Sides.Up)),
                  data.Bool("persistent"), id, GetWidth(data), data.Bool("actLikeTouchSwitch", true),
                  data.Bool("attachToSolid", true)) { }

        // again copied from FlagDashSwitch :)
        private static Sides SwitchSide(Sides side) => side switch {
            Sides.Up => Sides.Down,
            Sides.Down => Sides.Up,
            Sides.Left => Sides.Right,
            Sides.Right => Sides.Left,
            _ => throw new InvalidOperationException("Dash switch direction is invalid.")
        };

        private static int GetWidth(EntityData data) {
            Sides side = SwitchSide(data.Enum("orientation", Sides.Left));
            return side switch {
                Sides.Up => data.Width,
                Sides.Down => data.Width,
                Sides.Left => data.Height,
                Sides.Right => data.Height,
                _ => throw new InvalidOperationException("Dash switch direction is invalid.")
            };
        }

        #endregion

        #region Overrides

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (pressed)
                Switch?.Activate();
        }

        public override void Render() {
            Vector2 oldPos = Position;
            Position += spriteOffset;
            foreach (Image image in switchImages)
                image.Texture.DrawOnlyOutlineCentered(Position + image.Position, image.Rotation);
            base.Render();
            Position = oldPos;
        }

        #endregion

        #region Static mover stuff

        private bool IsRiding(Solid solid) {
            return CollideCheck(solid, Position + pressDirection);
        }

        private void OnMove(Vector2 amount) {
            // if currently solid, move player and stuff along
            if (Collidable) {
                MoveH(amount.X);
                MoveV(amount.Y);
            } else { // otherwise, just move without doing that
                Position += amount;
            }
            pressedTarget += amount;
            startY += amount.Y;
        }

        private void OnAttach(Platform platform) {
            Depth = platform.Depth + 1;
        }

        private new void OnShake(Vector2 amount) {
            spriteOffset += amount;
        }

        #endregion

        #region Sprites

        private void CreateSprite(int index) {
            foreach (Image image in switchImages)
                Remove(image);
            switchImages.Clear();

            Vector2 startPos, posIncrement;
            switch (Side) {
                case Sides.Down:
                    startPos = new Vector2(0, 0);
                    posIncrement = new Vector2(8, 0);
                    break;
                case Sides.Up:
                    startPos = new Vector2(width - 8, 0);
                    posIncrement = new Vector2(-8, 0);
                    break;
                case Sides.Right:
                    startPos = new Vector2(0, width - 8);
                    posIncrement = new Vector2(0, -8);
                    break;
                case Sides.Left:
                    startPos = new Vector2(0, 0);
                    posIncrement = new Vector2(0, 8);
                    break;
                default:
                    throw new InvalidOperationException("Dash switch direction is invalid.");
            }

            MTexture switchTexture = GFX.Game[$"objects/StrawberryJam2021/bigDashSwitch/bigSwitch{index:D2}"];
            Vector2 currentPos = startPos;
            for (int i = 0; i < widthTiles; i++) {
                int subtextureOffset;
                if (i == 0) {
                    subtextureOffset = 0;
                } else if (i == widthTiles - 1) {
                    subtextureOffset = 2;
                } else {
                    subtextureOffset = 1;
                }
                Image switchImage = new Image(switchTexture.GetSubtexture(subtextureOffset * 8, 0, 8, 8));
                switchImage.JustifyOrigin(0.5f, 0.5f);
                switchImage.Position = currentPos + new Vector2(4, 4);
                switchImage.Rotation = SwitchRotation;
                Add(switchImage);
                switchImages.Add(switchImage);

                currentPos += posIncrement;
            }
            
            Image midImage = new Image(GFX.Game[$"objects/StrawberryJam2021/bigDashSwitch/bigSwitchMid{index:D2}"]);
            midImage.JustifyOrigin(0.5f, 0.5f);
            midImage.Position = Center - Position;
            midImage.Rotation = SwitchRotation;
            Add(midImage);
            switchImages.Add(midImage);
        }

        private void AddLightningSprite(Vector2 playerPosition) {
            Sprite lightningSprite = StrawberryJam2021Module.SpriteBank.Create("bigSwitch_lightning");
            Vector2 playerOffset = playerPosition - Position;
            Vector2 spritePosition = Side switch {
                Sides.Down => new Vector2(Calc.Clamp(playerOffset.X, 3f, width - 4f), -12f),
                Sides.Up => new Vector2(Calc.Clamp(playerOffset.X, 4f, width - 3f), 20f),
                Sides.Left => new Vector2(20f, Calc.Clamp(playerOffset.Y, 3f, width - 4f)),
                Sides.Right => new Vector2(-12f, Calc.Clamp(playerOffset.Y, 4f, width - 3f)),
                _ => throw new InvalidOperationException("Dash switch direction is invalid."),
            };
            lightningSprite.Rotation = SwitchRotation;
            lightningSprite.Position = spritePosition;
            lightningSprite.OnFinish = _ => lightningSprite.RemoveSelf();
            Add(lightningSprite);
        }

        #endregion

        #region OnPressed stuff

        private void OnPressed(Player player, Vector2 direction) {
            player.RefillDash();
            if (Switch?.Activate() == true)
                SoundEmitter.Play(SFX.game_gen_touchswitch_last_oneshot);
            Add(new Coroutine(PlayPushedAnimation()));
            AddLightningSprite(player.Center);

            // add particles
            Level level = SceneAs<Level>();
            float rotation = SwitchRotation - Calc.QuarterCircle;
            Vector2 shatterPos = player.Center + direction * 8f;
            level.ParticlesFG.Emit(P_Shatter, Calc.Random.Range(3, 4), shatterPos, Vector2.Zero, rotation);
            Vector2 fizzlePos = Center + direction * 4f;
            int fizzleCount = Math.Max(3 * widthTiles, 16);
            level.ParticlesFG.Emit(P_Fizzle, fizzleCount, fizzlePos, direction.Perpendicular() * width / 2, rotation);

            // play sounds
            float volume = 0.5f;
            Audio.Play(SFX.game_gen_diamond_touch).setVolume(volume);
            EventInstance pressSound = Audio.Play(SFX.game_03_clutterswitch_press_books);
            float quarterTone = 1.0293f;
            float clampedWidthTiles = Calc.Clamp(widthTiles, 2, 20);
            float pitch = 1.16f * (float) Math.Pow(quarterTone, -clampedWidthTiles);
            pressSound.setPitch(pitch);
            pressSound.setVolume(0.7f * volume);
            Alarm.Set(this, 0.05f / Calc.Min(pitch, 1f), () => Audio.Stop(pressSound));
        }

        private IEnumerator PlayPushedAnimation() {
            CreateSprite(1);
            yield return 0.15f;
            CreateSprite(2);
        }

        #endregion

        #region Hooks/Loading

        public static void Load() {
            On.Celeste.DashSwitch.OnDashed += On_DashSwitch_OnDashed;
            IL.Celeste.DashSwitch.OnDashed += IL_DashSwitch_OnDashed;
            IL.Celeste.DashSwitch.Update += IL_DashSwitch_Update;
        }

        public static void Unload() {
            On.Celeste.DashSwitch.OnDashed -= On_DashSwitch_OnDashed;
            IL.Celeste.DashSwitch.OnDashed -= IL_DashSwitch_OnDashed;
            IL.Celeste.DashSwitch.Update -= IL_DashSwitch_Update;
        }

        private static DashCollisionResults On_DashSwitch_OnDashed(
            On.Celeste.DashSwitch.orig_OnDashed orig, DashSwitch self, Player player, Vector2 direction) {
            if (self is ResizableDashSwitch dashSwitch && !dashSwitch.pressed && direction == dashSwitch.pressDirection)
                dashSwitch.OnPressed(player, direction);
            
            return orig(self, player, direction);
        }

        private static void IL_DashSwitch_OnDashed(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // basic technique here copied from max's helping hand lol
            // move to the start of where we want to skip
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Entity>(nameof(Entity.Position)))) {
                // create another cursor and move to the end of where we want to skip
                ILCursor cursorAfterParticles = cursor.Clone();
                // go to the second call of this method
                if (cursorAfterParticles.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<ParticleSystem>(nameof(ParticleSystem.Emit)))
                    && cursorAfterParticles.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<ParticleSystem>(nameof(ParticleSystem.Emit)))) {
                    // skip particle calls if this is a ResizableDashSwitch
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<DashSwitch, bool>>(IsResizableDashSwitch);
                    cursor.Emit(OpCodes.Brtrue, cursorAfterParticles.Next);
                }
            }

            // lower the volume of the pressed sound
            cursor.Index = 0;
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(typeof(Audio), nameof(Audio.Play)))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldc_R4, 0.8f);
                cursor.EmitDelegate<Func<EventInstance, DashSwitch, float, EventInstance>>(ModifyVolume);
            }
        }

        private static bool IsResizableDashSwitch(DashSwitch dashSwitch) => dashSwitch is ResizableDashSwitch;

        private static EventInstance ModifyVolume(EventInstance instance, DashSwitch self, float volume) {
            if (self is ResizableDashSwitch) {
                instance.setVolume(volume);
            }
            return instance;
        }

        private static void IL_DashSwitch_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // find where the "depress"/"return" sounds are played
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(typeof(Audio), "Play"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                //cursor.EmitDelegate<Func<EventInstance, DashSwitch, EventInstance>>(ModifyPitch);
                cursor.Emit(OpCodes.Ldc_R4, 0f);
                cursor.EmitDelegate<Func<EventInstance, DashSwitch, float, EventInstance>>(ModifyVolume);
            }
        }

        //private static EventInstance ModifyPitch(EventInstance instance, DashSwitch self) {
        //    if (self is ResizableDashSwitch) {
        //        instance.setPitch(3f);
        //    }
        //    return instance;
        //}

        public static void LoadParticles() {
            P_Fizzle = new ParticleType {
                Color = Calc.HexToColor("dce34f"),
                Color2 = Calc.HexToColor("fbffaf"),
                ColorMode = ParticleType.ColorModes.Blink,
                Size = 1f,
                SizeRange = 0f,
                SpeedMin = 20f,
                SpeedMax = 70f,
                LifeMin = 0.5f,
                LifeMax = 0.9f,
                Direction = Calc.QuarterCircle,
                DirectionRange = Calc.EighthCircle / 1.5f,
                SpeedMultiplier = 0.2f
            };
            P_Shatter = new ParticleType {
                Source = GFX.Game["particles/triangle"],
                Color = Calc.HexToColor("fff089"),
                Color2 = Calc.HexToColor("fff089"),
                FadeMode = ParticleType.FadeModes.Late,
                LifeMin = 0.35f,
                LifeMax = 0.5f,
                Size = 1f,
                Direction = Calc.HalfCircle * 1.5f,
                DirectionRange = Calc.QuarterCircle,
                SpeedMin = 120f,
                SpeedMax = 140f,
                SpeedMultiplier = 0.01f,
                RotationMode = ParticleType.RotationModes.Random,
                SpinMin = -Calc.QuarterCircle,
                SpinMax = 4.712389f,
                SpinFlippedChance = true
            };
        }

        #endregion
    }

    public static class ResizableDashSwitchExtensions {
        private static readonly Vector2[] outlineOffsets = new Vector2[] {
            -Vector2.UnitX,
            Vector2.UnitX,
            -Vector2.UnitY,
            Vector2.UnitY
        };

        public static void DrawOnlyOutlineCentered(this MTexture mTexture, Vector2 position, float rotation) {
            // copied and adapted from Monocle.MTexture.DrawOutlineCentered
            float scaleFix = mTexture.ScaleFix;
            Rectangle clipRect = mTexture.ClipRect;
            Vector2 origin = (mTexture.Center - mTexture.DrawOffset) / scaleFix;
            Texture2D textureSafe = mTexture.Texture.Texture_Safe;
            foreach (Vector2 offset in outlineOffsets) {
                Draw.SpriteBatch.Draw(textureSafe, position + offset, clipRect,
                    Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
            }
        }
    }
}
