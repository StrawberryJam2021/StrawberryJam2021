using Celeste.Mod.CollabUtils2.Triggers;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/StrawberryJamJar")]
    public class StrawberryJamJar : Entity {
        private readonly string map;
        private readonly string returnToLobbyMode;
        private readonly bool allowSaving;

        public StrawberryJamJar(Vector2 position, string spriteName, string map, string returnToLobbyMode, bool allowSaving) : base(position) {
            this.map = map;
            this.returnToLobbyMode = returnToLobbyMode;
            this.allowSaving = allowSaving;

            // check if the map was already completed
            AreaData areaData = AreaData.Get(map);
            bool complete = false;
            if (areaData != null) {
                complete = SaveData.Instance.GetAreaStatsFor(areaData.ToKey())?.Modes[0].Completed ?? false;
            }

            string animation;
            if (complete) {
                if (!StrawberryJam2021Module.SaveData.FilledJamJarSIDs.Contains(map)) {
                    // map is complete but jar wasn't filled yet!
                    animation = "before_fill";
                    StrawberryJam2021Module.SaveData.FilledJamJarSIDs.Add(map);
                } else {
                    // map is complete and jar was already filled.
                    animation = "full";
                }
            } else {
                // map wasn't completed yet.
                animation = "empty";
            }

            Sprite sprite = StrawberryJam2021Module.SpriteBank.Create("jamJar_" + spriteName);
            sprite.Play(animation);
            Add(sprite);

            // play the fill sound at the right time.
            if (animation == "before_fill") {
                sprite.OnChange = (lastAnimationId, currentAnimationId) => {
                    if (currentAnimationId == "fill") {
                        SoundSource sound = new SoundSource(new Vector2(0, -20f), pickFillSoundEffect(spriteName)) { RemoveOnOneshotEnd = true };
                        new DynData<SoundSource>(sound).Get<EventInstance>("instance").setVolume(0.3f);
                        Add(sound);
                    }
                };
            }

            Depth = Depths.NPCs;
        }

        public StrawberryJamJar(EntityData data, Vector2 offset) : this(data.Position + offset, data.Attr("sprite"), data.Attr("map"), data.Attr("returnToLobbyMode"), data.Bool("allowSaving")) { }

        public override void Added(Scene scene) {
            base.Added(scene);

            // spawn a chapter panel trigger from Collab Utils that will take care of the actual teleporting.
            scene.Add(new ChapterPanelTrigger(new EntityData {
                Position = Position - new Vector2(24f, 32f),
                Width = 48,
                Height = 32,
                Nodes = new[] { Position - new Vector2(0, 32) },
                Values = new Dictionary<string, object> {
                    { "map", map },
                    { "returnToLobbyMode", returnToLobbyMode },
                    { "allowSaving", allowSaving },
                },
            }, Vector2.Zero));
        }

        private string pickFillSoundEffect(string spriteName) {
            switch (spriteName) {
                case "beginner":
                    return CustomSoundEffects.game_jars_open_beginner;
                case "intermediate":
                    return CustomSoundEffects.game_jars_open_intermediate;
                case "advanced":
                    return CustomSoundEffects.game_jars_open_advanced;
                case "expert":
                    return CustomSoundEffects.game_jars_open_expert;
                case "grandmaster":
                    return CustomSoundEffects.game_jars_open_grandmaster;
                default:
                    return CustomSoundEffects.game_jars_open_demo;
            }
        }
    }
}
