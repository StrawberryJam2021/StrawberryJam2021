using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [Tracked(true)]
    public class Mask : Entity {
        //private static List<VirtualRenderTarget> RenderTargets = new List<VirtualRenderTarget>();
        //private static Dictionary<Mask, int> RenderTargetIndex = new Dictionary<Mask, int>();

        public static BlendState DestinationAlphaBlend = new BlendState {
            ColorSourceBlend = Blend.DestinationAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
        };

        public static BlendState DestinationAlphaSourceColorBlend = new BlendState {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
        };


        public enum FadeType {
            None,
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop,
            Custom,
        }

        public FadeType Fade { get; set; }
        public MTexture FadeMask { get; set; }
        public string Flag { get; set; }
        public bool NotFlag { get; set; }
        public float ScrollX { get; set; }
        public float ScrollY { get; set; }

        public Level Level;
        protected Vector2 startPosition;

        public Mask(Vector2 position, float width, float height) : base(position) {
            Collider = new Hitbox(width, height);
            startPosition = Position;
        }

        public Mask(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Height) {
            Fade = data.Enum("fade", FadeType.None);
            FadeMask = GFX.Game.GetOrDefault($"fademasks/{data.Attr("customFade")}", null);
            Flag = data.Attr("flag");
            NotFlag = data.Bool("notFlag");
            ScrollX = data.Float("scrollX");
            ScrollY = data.Float("scrollY");

            startPosition = Position;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Level = scene as Level;
        }

        public override void Render() {
            var cameraCenter = SceneAs<Level>().Camera.Position + new Vector2(160f, 90f);
            if (ScrollX != 0f || ScrollY != 0f) {
                var baseCenter = startPosition + new Vector2(Width / 2f, Height / 2f);
                Center = Calc.Round(baseCenter + (baseCenter - cameraCenter) * new Vector2(ScrollX, ScrollY));
            }
            base.Render();
        }

        public void DrawFadeMask(Color? color = null)
            => FadeMask?.Draw(Position, Vector2.Zero, color ?? Color.White, new Vector2(Width / FadeMask.Width, Height / FadeMask.Height));

        public bool IsVisible() {
            if (!string.IsNullOrEmpty(Flag) && Level.Session.GetFlag(Flag) == NotFlag)
                return false;
            var visibleRect = GetVisibleRect();
            return visibleRect.Width > 0 && visibleRect.Height > 0;
        }

        public Rectangle GetVisibleRect()
            => Rectangle.Intersect(new Rectangle(0, 0, 320, 180), new Rectangle((int)(X - Level.Camera.X), (int)(Y - Level.Camera.Y), (int)Width, (int)Height));

        public Vector2 GetDrawPos()
            => new Vector2(Math.Max(X, Level.Camera.X), Math.Max(Y, Level.Camera.Y));

        public Vector2 GetDrawOffset()
            => new Vector2(Math.Max(0, Level.Camera.X - X), Math.Max(0, Level.Camera.Y - Y));

        public List<MaskSlice> GetMaskSlices() {
            var slices = new List<MaskSlice>();
            if (!IsVisible())
                return slices;
            var offset = Vector2.Zero;
            var source = Rectangle.Empty;
            switch (Fade) {
                case FadeType.None:
                case FadeType.Custom:
                    slices.Add(new MaskSlice(GetDrawPos(), GetVisibleRect()));
                    break;
                case FadeType.LeftToRight:
                case FadeType.RightToLeft:
                    offset = GetDrawOffset();
                    source = GetVisibleRect();
                    for (int x = (int)offset.X; x < Width; x++) {
                        if ((x - (int)offset.X) < source.Width) {
                            slices.Add(new MaskSlice(
                                Position + new Vector2(x, offset.Y),
                                new Rectangle(source.X + (x - (int)offset.X), source.Y, 1, source.Height),
                                Fade == FadeType.LeftToRight ? (x / Width) : (1 - x / Width)
                            ));
                        }
                    }
                    break;
                case FadeType.TopToBottom:
                case FadeType.BottomToTop:
                    offset = GetDrawOffset();
                    source = GetVisibleRect();
                    for (int y = (int)offset.Y; y < Height; y++) {
                        if ((y - (int)offset.Y) < source.Height) {
                            slices.Add(new MaskSlice(
                                Position + new Vector2(offset.X, y),
                                new Rectangle(source.X, source.Y + (y - (int)offset.Y), source.Width, 1),
                                Fade == FadeType.TopToBottom ? (y/ Height) : (1 - y / Height)
                            ));
                        }
                    }
                    break;
            }
            return slices;
        }


        public struct MaskSlice {
            public Vector2 Position;
            public Rectangle Source;
            public float Value;

            public MaskSlice(Vector2 position, Rectangle source, float val = 1f) {
                Position = position;
                Source = source;
                Value = val;
            }

            public float GetValue(float from, float to) {
                return Calc.LerpClamp(from, to, Value);
            }
        }
    }
}
