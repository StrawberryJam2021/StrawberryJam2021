using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    public class Credits {
        public static readonly Color BorderColor = Color.Black;
        public const float CreditSpacing = 64f;
        public const float SongLength = 174f;
        public const float ScrollSpeed = 200f;

        public float BottomTimer;
        public bool Enabled;

        private readonly List<CreditNode> credits;
        private readonly float height;
        private readonly float alignment;
        private readonly float scale;
        private float scrollSpeed;
        private float scroll;

        public Credits(float alignment = 0.5f, float scale = 1f) {
            Enabled = true;
            this.alignment = alignment;
            this.scale = scale;
            credits = CreateCredits();
            height = 0f;
            foreach (CreditNode creditNode in credits) {
                height += creditNode.Height(scale) + CreditSpacing * scale;
            }
        }

        public float Duration => height / (ScrollSpeed * scale);

        public void Update() {
            if (Enabled) {
                scroll += scrollSpeed * Engine.DeltaTime * scale;

                // DEBUG
                if (Input.MenuDown.Check) {
                    scrollSpeed = ScrollSpeed * 10f;
                } else if (Input.MenuUp.Check) {
                    scrollSpeed = ScrollSpeed * -10f;
                } else {
                    scrollSpeed = ScrollSpeed;
                }

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
        }

        public void Render(Vector2 position) {
            Vector2 nodePos = position + new Vector2(0f, 1080f - scroll).Floor();
            foreach (CreditNode creditNode in credits) {
                float nodeHeight = creditNode.Height(scale);
                if (nodePos.Y > -nodeHeight && nodePos.Y < 1080f) {
                    creditNode.Render(nodePos, alignment, scale);
                }
                nodePos.Y += nodeHeight + CreditSpacing * scale;
            }
        }

        private static List<CreditNode> CreateCredits() {
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

                new Heading("SJ2021_Credits_Heading_Teams"),

                new Team("SJ2021_Credits_Category_Gameplay",
                    new Team.Section("SJ2021_Credits_Category_Beginner", "SJ2021_Credits_Names_Teams_Gameplay_Beginner"),
                    new Team.Section("SJ2021_Credits_Category_Intermediate", "SJ2021_Credits_Names_Teams_Gameplay_Intermediate"),
                    new Team.Section("SJ2021_Credits_Category_Advanced", "SJ2021_Credits_Names_Teams_Gameplay_Advanced"),
                    new Team.Section("SJ2021_Credits_Category_Expert", "SJ2021_Credits_Names_Teams_Gameplay_Expert"),
                    new Team.Section("SJ2021_Credits_Category_Grandmaster", "SJ2021_Credits_Names_Teams_Gameplay_Grandmaster"),
                    new Team.Section("SJ2021_Credits_Category_Gym", "SJ2021_Credits_Names_Teams_Gameplay_Gym")
                ),

                new Team("SJ2021_Credits_Category_Coding", "SJ2021_Credits_Names_Teams_Coding"),
                new Team("SJ2021_Credits_Category_Art", "SJ2021_Credits_Names_Teams_Art"),
                new Team("SJ2021_Credits_Category_Music", "SJ2021_Credits_Names_Teams_Music"),
                new Team("SJ2021_Credits_Category_Decoration", "SJ2021_Credits_Names_Teams_Decoration"),
                new Team("SJ2021_Credits_Category_Playtesting", "SJ2021_Credits_Names_Teams_Playtesting"),
                new Team("SJ2021_Credits_Category_Special", "SJ2021_Credits_Names_Special")
            };

            return list;
        }

        public abstract class CreditNode {
            public abstract void Render(Vector2 position, float alignment = 0.5f, float scale = 1f);
            public abstract float Height(float scale = 1f);
        }

        public class Heading : CreditNode {
            public const float HeadingScale = 3f;

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
                return ActiveFont.LineHeight * HeadingScale * scale;
            }
        }

        public class Team : CreditNode {
            public const float TitleScale = 1.7f;
            public const float CreditsScale = 1.15f;
            public const float Spacing = 8f;
            public const float SubtitleScale = 0.85f;
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

            private float LineHeight => ActiveFont.LineHeight;

            public override void Render(Vector2 position, float alignment = 0.5f, float scale = 1f) {
                Vector2 justify = new(alignment, 0f);
                Vector2 leftAlign = Vector2.Zero;
                Vector2 rightAlign = new(1f, 0f);
                Vector2 margin = new(ActiveFont.LineHeight * CreditsScale * scale / 3, 0f);

                ActiveFont.DrawOutline(Title, position.Floor(), justify, Vector2.One * TitleScale * scale, TitleColor, 2f, BorderColor);
                position.Y += (LineHeight * TitleScale + Spacing) * scale;

                for (int i = 0; i < Sections.Length; i++) {
                    if (!string.IsNullOrEmpty(Sections[i].Subtitle)) {
                        ActiveFont.DrawOutline(Sections[i].Subtitle, position.Floor(), justify, Vector2.One * SubtitleScale * scale, SubtitleColor, 2f, BorderColor);
                        position.Y += LineHeight * SubtitleScale * scale;
                    }
                    for (int j = 0; j < Sections[i].Credits.Length; j++) {
                        if (!Sections[i].Split) {
                            ActiveFont.DrawOutline(Sections[i].Credits[j], position.Floor(), justify, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                        } else {
                            ActiveFont.DrawOutline(Sections[i].Credits[j], position.Floor() - margin, rightAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                            if (j < Sections[i].Credits.Length - 1) {
                                ActiveFont.DrawOutline(Sections[i].Credits[j + 1], position.Floor() + margin, leftAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                                j++;
                            }
                        }
                        position.Y += LineHeight * CreditsScale * scale;
                    }
                    position.Y += SectionSpacing * scale;
                }
            }

            public override float Height(float scale = 1f) {
                float lineHeight = ActiveFont.LineHeight;
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
    }
}
