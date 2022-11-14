using System;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Effects {
    public class MeteorShower : Backdrop {
        private struct Meteor {
            public Vector2 Position;
            public int TextureSet;
            public float Timer;
            public float Rate;
        }

        private List<Meteor> meteors;
        public float MeteorCount;
        private Vector2 center;

        private List<List<MTexture>> textures;

        public MeteorShower(int numMeteors) {
            MeteorCount = numMeteors;
            textures = new List<List<MTexture>>();
            textures.Add(GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/arcComet"));
            textures.Add(GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/slantComet"));
            center = new Vector2(textures[0][0].Width, textures[0][0].Height) / 2;
            meteors = new List<Meteor>();
            for (int i = 0; i < MeteorCount; i++) {
                meteors.Add(new Meteor {
                    Position = new Vector2(Calc.Random.NextFloat(320f), Calc.Random.NextFloat(180f)),
                    Timer = Calc.Random.NextFloat((float) Math.PI * 2f),
                    Rate = 2f + Calc.Random.NextFloat(2f),
                    TextureSet = Calc.Random.Next(textures.Count)
                });
            }
        }

        public override void Update(Scene scene) {
            base.Update(scene);

            if (Visible) {
                for (int i = 0; i < meteors.Count; i++) {
                    meteors[i] = new Meteor {
                        Position = meteors[i].Position,
                        Timer = meteors[i].Timer + (Engine.DeltaTime * meteors[i].Rate),
                        Rate = meteors[i].Rate,
                        TextureSet = meteors[i].TextureSet,
                    };
                }
            }
        }

        public override void Render(Scene scene) {
            base.Render(scene);

            foreach (Meteor m in meteors) {
                int textAnimId = (int)(m.Timer * textures[m.TextureSet].Count) % textures[m.TextureSet].Count;
                MTexture texture = textures[m.TextureSet][textAnimId];
                texture.Draw(m.Position, center);
            }
        }
    }
}
