using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/StrawberryJamJar")]
    public class StrawberryJamJar : Entity {
        public StrawberryJamJar(Vector2 position, string spriteName, string map) : base(position) {
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
                        SoundSource sound = new SoundSource(new Vector2(0, -20f), CustomSoundEffects.game_jars_open_demo) { RemoveOnOneshotEnd = true };
                        new DynData<SoundSource>(sound).Get<EventInstance>("instance").setVolume(0.3f);
                        Add(sound);
                    }
                };
            }
        }

        public StrawberryJamJar(EntityData data, Vector2 offset) : this(data.Position + offset, data.Attr("sprite"), data.Attr("map")) { }
    }
}
