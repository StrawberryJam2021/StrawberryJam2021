using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    [CustomEntity("SJ2021/AllInOneMask")]
    public class AllInOneMask : Mask {
        private readonly List<Entity> masks;

        public AllInOneMask(EntityData data, Vector2 offset) : base(data, offset) {
            float width = data.Width;
            float height = data.Height;

            masks = new List<Entity>();

            string stylemaskTag = data.Attr("stylemaskTag");
            float styleAlphaFrom = data.Float("styleAlphaFrom");
            float styleAlphaTo = data.Float("styleAlphaTo", 1f);
            bool entityRenderer = data.Bool("entityRenderer");
            bool styleBehindFg = data.Bool("styleBehindFg", true);
            if (!string.IsNullOrEmpty(stylemaskTag)) {
                masks.Add(new StylegroundMask(Position, width, height) {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    RenderTags = stylemaskTag.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    AlphaFrom = styleAlphaFrom,
                    AlphaTo = styleAlphaTo,
                    EntityRenderer = entityRenderer,
                    BehindForeground = styleBehindFg,
                });
            }

            string colorGradeFrom = data.Attr("colorGradeFrom", "(current)");
            string colorGradeTo = data.Attr("colorGradeTo", "(current)");
            if (colorGradeFrom != "(current)" || colorGradeTo != "(current)") {
                masks.Add(new ColorGradeMask(Position, width, height) {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    ColorGradeFrom = colorGradeFrom,
                    ColorGradeTo = colorGradeTo,
                });
            }

            float bloomBaseFrom = data.Float("bloomBaseFrom", -1f);
            float bloomBaseTo = data.Float("bloomBaseFrom", -1f);
            float bloomStrengthFrom = data.Float("bloomStrengthFrom", -1f);
            float bloomStrengthTo = data.Float("bloomStrengthTo", -1f);
            if (bloomBaseFrom >= 0f || bloomBaseTo >= 0f || bloomStrengthFrom >= 0f || bloomStrengthTo >= 0f) {
                masks.Add(new BloomMask(Position, width, height) {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    BaseFrom = bloomBaseFrom,
                    BaseTo = bloomBaseTo,
                    StrengthFrom = bloomStrengthFrom,
                    StrengthTo = bloomStrengthTo,
                });
            }

            float lightingFrom = data.Float("lightingFrom", -1f);
            float lightingTo = data.Float("lightingTo", -1f);
            bool addBaseLighting = data.Bool("addBaseLight", true);
            if (lightingFrom >= 0f || lightingTo >= 0f) {
                masks.Add(new LightingMask(Position, width, height) {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                    LightingFrom = lightingFrom,
                    LightingTo = lightingTo,
                    AddBase = addBaseLighting,
                });
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            foreach (Entity mask in masks)
                scene.Add(mask);
        }
    }
}
