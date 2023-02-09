using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    public class Credits : Entity {
        public static readonly Color BorderColor = Color.Black;
        public const float CreditSpacing = 64f;
        public const float SongLength = 174f;

        public float BottomTimer;

        private readonly List<CreditNode> credits;
        private readonly float height;
        private readonly float alignment;
        private readonly float scale;
        private readonly MTexture thanks;
        internal float scrollSpeed;
        private float scroll;

        public Credits(Vector2 position, MTexture thanks, float alignment = 0.5f, float scale = 1f, bool doubleColumns = true)
            : base(position) {
            this.alignment = alignment;
            this.scale = scale;
            this.thanks = thanks;
            credits = CreateCredits(doubleColumns);
            height = 0f;
            foreach (CreditNode creditNode in credits) {
                height += creditNode.Height(scale) + CreditSpacing * scale;
            }

            scrollSpeed = height / SongLength;

            Depth = Depths.FormationSequences;
            Tag = TagsExt.SubHUD;
        }

        public override void Update() {
            base.Update();
            scroll += scrollSpeed * Engine.DeltaTime;

            if (scroll < 0f || scroll > height) {
                scrollSpeed = 0f;
            }
            scroll = Calc.Clamp(scroll, 0f, height);
            if (scroll >= height) {
                BottomTimer += Engine.DeltaTime;
            } else {
                BottomTimer = 0f;
            }
        }

        public override void Render() {
            base.Render();
            Vector2 nodePos = new Vector2(Position.X, Celeste.TargetHeight - scroll).Floor();
            foreach (CreditNode creditNode in credits) {
                float nodeHeight = creditNode.Height(scale);

                if (nodePos.Y > -nodeHeight && nodePos.Y < Celeste.TargetHeight) {
                    creditNode.Render(nodePos, alignment, scale);
                }
                nodePos.Y += nodeHeight + CreditSpacing * scale;
            }
        }

        private List<CreditNode> CreateCredits(bool doubleColumns) {
            List<CreditNode> list = new() {
                new Heading("SJ2021_Credits_Heading_Captains"),

                new Team("SJ2021_Credits_Category_Hosts", "SJ2021_Credits_Names_Captains_Hosts"),
                new Team("SJ2021_Credits_Category_Mapping",
                    new Team.Section("SJ2021_Credits_Category_Beginner", "SJ2021_Credits_Names_Captains_Mapping_Beginner"),
                    new Team.Section("SJ2021_Credits_Category_Intermediate", "SJ2021_Credits_Names_Captains_Mapping_Intermediate"),
                    new Team.Section("SJ2021_Credits_Category_Advanced", "SJ2021_Credits_Names_Captains_Mapping_Advanced"),
                    new Team.Section("SJ2021_Credits_Category_Expert", "SJ2021_Credits_Names_Captains_Mapping_Expert"),
                    new Team.Section("SJ2021_Credits_Category_Grandmaster", "SJ2021_Credits_Names_Captains_Mapping_Grandmaster"),
                    new Team.Section("SJ2021_Credits_Category_Additional", "SJ2021_Credits_Names_Captains_Mapping_Additional")
                ),

                new Team("SJ2021_Credits_Category_Lobby",
                    new Team.Section("SJ2021_Credits_Category_Prologue", "SJ2021_Credits_Names_Captains_Lobby_Prologue"),
                    new Team.Section("SJ2021_Credits_Category_Beginner", "SJ2021_Credits_Names_Captains_Lobby_Beginner"),
                    new Team.Section("SJ2021_Credits_Category_Intermediate", "SJ2021_Credits_Names_Captains_Lobby_Intermediate"),
                    new Team.Section("SJ2021_Credits_Category_Advanced", "SJ2021_Credits_Names_Captains_Lobby_Advanced"),
                    new Team.Section("SJ2021_Credits_Category_Expert", "SJ2021_Credits_Names_Captains_Lobby_Expert"),
                    new Team.Section("SJ2021_Credits_Category_Grandmaster", "SJ2021_Credits_Names_Captains_Lobby_Grandmaster")
                ),

                new Team("SJ2021_Credits_Category_Gym", "SJ2021_Credits_Names_Captains_Gym"),
                new Team("SJ2021_Credits_Category_Coding", "SJ2021_Credits_Names_Captains_Coding"),
                new Team("SJ2021_Credits_Category_Art", "SJ2021_Credits_Names_Captains_Art"),
                new Team("SJ2021_Credits_Category_Music", "SJ2021_Credits_Names_Captains_Music"),
                new Team("SJ2021_Credits_Category_Decoration", "SJ2021_Credits_Names_Captains_Decoration"),
                new Team("SJ2021_Credits_Category_Playtesting", "SJ2021_Credits_Names_Captains_Playtesting"),
                new Team("SJ2021_Credits_Category_Media", "SJ2021_Credits_Names_Captains_Media"),
                new Team("SJ2021_Credits_Category_Localization", "SJ2021_Credits_Names_Captains_Localization"),
                new Team("SJ2021_Credits_Category_Accessibility", "SJ2021_Credits_Names_Captains_Accessibility"),

                new Break(180f),

                new Heading("SJ2021_Credits_Heading_Teams"),

                new Team("SJ2021_Credits_Category_Gameplay",
                    new Team.Section("SJ2021_Credits_Category_Beginner", "SJ2021_Credits_Names_Teams_Gameplay_Beginner", split: doubleColumns),
                    new Team.Section("SJ2021_Credits_Category_Intermediate", "SJ2021_Credits_Names_Teams_Gameplay_Intermediate", split: doubleColumns),
                    new Team.Section("SJ2021_Credits_Category_Advanced", "SJ2021_Credits_Names_Teams_Gameplay_Advanced", split: doubleColumns),
                    new Team.Section("SJ2021_Credits_Category_Expert", "SJ2021_Credits_Names_Teams_Gameplay_Expert", split: doubleColumns),
                    new Team.Section("SJ2021_Credits_Category_Grandmaster", "SJ2021_Credits_Names_Teams_Gameplay_Grandmaster", split: doubleColumns),
                    new Team.Section("SJ2021_Credits_Category_Gym", "SJ2021_Credits_Names_Teams_Gameplay_Gym", split: doubleColumns)
                ),

                new Team("SJ2021_Credits_Category_Coding", "SJ2021_Credits_Names_Teams_Coding", split: doubleColumns),
                new Team("SJ2021_Credits_Category_Art", "SJ2021_Credits_Names_Teams_Art", split: doubleColumns),
                new Team("SJ2021_Credits_Category_Music", "SJ2021_Credits_Names_Teams_Music", split: doubleColumns),
                new Team("SJ2021_Credits_Category_Decoration", "SJ2021_Credits_Names_Teams_Decoration", split : doubleColumns),
                new Team("SJ2021_Credits_Category_Playtesting", "SJ2021_Credits_Names_Teams_Playtesting", split: doubleColumns),
                new Special("SJ2021_Credits_Category_Special", "SJ2021_Credits_Names_Special"),
                new Team("SJ2021_Credits_Category_Helpers", "SJ2021_Credits_Names_Helpers", split: doubleColumns),

                new Thanks("SJ2021_Credits_Thanks", thanks)
            };

            return list;
        }

        public abstract class CreditNode {
            public abstract void Render(Vector2 position, float alignment = 0.5f, float scale = 1f);
            public abstract float Height(float scale = 1f);
            public float LineHeight => ActiveFont.LineHeight;
        }

        public class Heading : CreditNode {
            public const float HeadingScale = 2.5f;

            public readonly Color HeadingColor;
            public string HeadingString;

            public Heading(string headingKey) {
                HeadingColor = Color.Gray;
                HeadingString = Dialog.Clean(headingKey);
            }

            public override void Render(Vector2 position, float alignment = 0.5F, float scale = 1) {
                ActiveFont.DrawEdgeOutline(HeadingString, position.Floor(), new Vector2(alignment, 0f), Vector2.One * HeadingScale * scale, Color.Gray, 4f, Color.DarkSlateBlue, 2f, BorderColor);
            }

            public override float Height(float scale = 1f) {
                return LineHeight * HeadingScale * scale;
            }
        }

        public class Team : CreditNode {
            public const float TitleScale = 1.7f;
            public const float CreditsScale = 1.15f;
            public const float Spacing = 8f;
            public const float SubtitleScale = 0.9f;
            public const float SectionSpacing = 32f;

            public static readonly Color TitleColor = Color.White;
            public static readonly Color CreditsColor = Color.White * 0.8f;
            public static readonly Color SubtitleColor = Calc.HexToColor("a8a694");

            public string Title;
            public Section[] Sections;

            public Team(string titleKey, params Section[] sections) {
                Title = Dialog.Clean(titleKey);
                Sections = sections;
            }

            public Team(string titleKey, string creditsKey, bool split = false)
                : this(titleKey, new Section(null, creditsKey, split)) {
            }

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f) {
                Vector2 renderPos = position.Floor();
                Vector2 justify = new(alignment, 0f);
                Vector2 leftAlign = Vector2.Zero;
                Vector2 rightAlign = new(1f, 0f);
                Vector2 margin = new(LineHeight * CreditsScale * scale / 3, 0f);

                ActiveFont.DrawOutline(Title, renderPos, justify, Vector2.One * TitleScale * scale, TitleColor, 2f, BorderColor);
                renderPos.Y += (LineHeight * TitleScale + Spacing) * scale;

                for (int i = 0; i < Sections.Length; i++) {
                    if (!string.IsNullOrEmpty(Sections[i].Subtitle)) {
                        ActiveFont.DrawOutline(Sections[i].Subtitle, renderPos, justify, Vector2.One * SubtitleScale * scale, SubtitleColor, 2f, BorderColor);
                        renderPos.Y += LineHeight * SubtitleScale * scale;
                    }
                    for (int j = 0; j < Sections[i].Credits.Length; j++) {
                        if (!Sections[i].Split) {
                            ActiveFont.DrawOutline(Sections[i].Credits[j], renderPos, justify, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                        } else {
                            ActiveFont.DrawOutline(Sections[i].Credits[j], renderPos - margin, rightAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                            if (j < Sections[i].Credits.Length - 1) {
                                ActiveFont.DrawOutline(Sections[i].Credits[j + 1], renderPos + margin, leftAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                                j++;
                            }
                        }
                        renderPos.Y += LineHeight * CreditsScale * scale;
                    }
                    renderPos.Y += SectionSpacing * scale;
                }
            }

            public override float Height(float scale = 1f) {
                float height = 0f;
                height += LineHeight * TitleScale + Spacing;
                for (int i = 0; i < Sections.Length; i++) {
                    if (!string.IsNullOrEmpty(Sections[i].Subtitle)) {
                        height += LineHeight * SubtitleScale;
                    }
                    int lineCount = Sections[i].Split ? (Sections[i].Credits.Length / 2 + Sections[i].Credits.Length % 2) : Sections[i].Credits.Length;
                    height += LineHeight * CreditsScale * lineCount;
                }
                height += SectionSpacing * (Sections.Length - 1);
                return height * scale;
            }

            public struct Section {
                public string Subtitle;
                public string[] Credits;
                public bool Split;

                public Section(string subtitleKey, string creditsKey, bool split = false) {
                    Subtitle = !string.IsNullOrEmpty(subtitleKey) ? Dialog.Clean(subtitleKey) : "";
                    Credits = Dialog.Clean(creditsKey).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    Split = split;
                }
            }
        }

        public class Special : CreditNode {
            public const float TitleScale = 1.7f;
            public const float CreditsScale = 1.15f;
            public const float Spacing = 8f;
            public const float RoleScale = 0.9f;

            public static readonly Color TitleColor = Color.White;
            public static readonly Color CreditsColor = Color.White * 0.8f;
            public static readonly Color RoleColor = Calc.HexToColor("a8a694");

            public string Title;
            public string[] Credits;

            public Special(string titleKey, string creditsKey) {
                Title = Dialog.Clean(titleKey);
                Credits = Dialog.Clean(creditsKey).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f) {
                Vector2 renderPos = position.Floor();
                Vector2 justify = new(alignment, 0f);

                ActiveFont.DrawOutline(Title, renderPos, justify, Vector2.One * TitleScale * scale, TitleColor, 2f, BorderColor);
                renderPos.Y += (LineHeight * TitleScale + Spacing) * scale;

                for (int i = 0; i < Credits.Length; i++) {
                    ActiveFont.DrawOutline(Credits[i], renderPos, justify, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                    renderPos.Y += LineHeight * CreditsScale * scale;
                    if (++i < Credits.Length) {
                        ActiveFont.DrawOutline(Credits[i], renderPos, justify, Vector2.One * RoleScale * scale, RoleColor, 2f, BorderColor);
                        renderPos.Y += LineHeight * RoleScale * scale;
                    }
                }
            }

            public override float Height(float scale = 1f) {
                float height = 0f;
                height += LineHeight * TitleScale + Spacing;
                height += LineHeight * CreditsScale * (Credits.Length / 2);
                height += LineHeight * RoleScale * (Credits.Length / 2);
                return height * scale;
            }
        }

        public class Thanks : CreditNode {
            public const float ThanksScale = 1.5f;

            private readonly MTexture texture;
            private readonly string message;

            public Thanks(string dialogKey, MTexture texture) {
                message = Dialog.Clean(dialogKey);
                this.texture = texture;
            }

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f) {
                texture.DrawCentered(new(Celeste.TargetCenter.X, position.Y + Celeste.TargetCenter.Y), Color.White);
                ActiveFont.DrawOutline(message, new Vector2(Celeste.TargetCenter.X, position.Y + Celeste.TargetCenter.Y + texture.Height / 2f + CreditSpacing), new Vector2(0.5f, 0.5f), Vector2.One * ThanksScale, Color.White, 2f, BorderColor);
            }

            public override float Height(float scale = 1f) {
                return Celeste.TargetHeight;
            }
        }

        private class Break : CreditNode {
            public float Size;

            public Break(float size = 64f) {
                Size = size;
            }

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f) {
            }

            public override float Height(float scale = 1f) {
                return Size * scale;
            }
        }
    }
}
