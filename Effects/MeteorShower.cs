using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Effects {
    public class MeteorShower : Backdrop {
        private struct Meteor {
            public Vector2 Position;
            public int TextureSet;
            public float Timer;
            public float RateFPS;
            public float Rotation;
            public float ResetTime;
        }

        private List<Meteor> meteors;
        public int MeteorCount;
        private Vector2 center;

        private List<List<MTexture>> textures;

        public MeteorShower(int numMeteors) {
            MeteorCount = numMeteors;
            textures = new List<List<MTexture>> {
                GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/arcComet"),
                GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/slantComet")
            };
            center = new Vector2(textures[0][0].Width, textures[0][0].Height) / 2;
            meteors = new List<Meteor>();
            for (int i = 0; i < MeteorCount; i++) {
                meteors.Add(newMeteor());
            }
        }

        private Meteor newMeteor() {
            return new Meteor {
                Position = new Vector2(Calc.Random.NextFloat(320f), Calc.Random.NextFloat(180f)),
                Timer = 0f,
                RateFPS = 12.5f + Calc.Random.NextFloat(7.5f),
                TextureSet = Calc.Random.Next(textures.Count),
                Rotation = Calc.Random.NextFloat((float) Math.PI / 3),
                ResetTime = 3f + Calc.Random.NextFloat(2f)
            };
        }

        public override void Update(Scene scene) {
            base.Update(scene);

            if (Visible) {
                for (int i = 0; i < MeteorCount; i++) {
                    float newTimer = meteors[i].Timer + Engine.DeltaTime;

                    if (newTimer > meteors[i].ResetTime) {
                        if (i >= meteors.Count) {
                            meteors.Add(newMeteor());
                        } else {
                            meteors[i] = newMeteor();
                        }
                    } else {
                        meteors[i] = new Meteor {
                            Position = meteors[i].Position,
                            Timer = newTimer,
                            RateFPS = meteors[i].RateFPS,
                            TextureSet = meteors[i].TextureSet,
                            Rotation = meteors[i].Rotation,
                            ResetTime = meteors[i].ResetTime
                        };
                    }
                }

                for (int j = MeteorCount; j > meteors.Count; j--) {
                    meteors.RemoveAt(j);
                }
            }
        }

        public override void Render(Scene scene) {
            base.Render(scene);

            foreach (Meteor m in meteors) {
                int textAnimId = (int)(m.Timer * m.RateFPS);

                if (textAnimId < textures[m.TextureSet].Count) {
                    MTexture texture = textures[m.TextureSet][textAnimId];
                    texture.Draw(m.Position, center, Color.White, 1f, m.Rotation);
                }
            }
        }
    }
}
