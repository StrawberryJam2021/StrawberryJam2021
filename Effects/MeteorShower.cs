using System;
using System.Collections.Generic;
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

        private List<List<MTexture>> textures;

        public MeteorShower(int numMeteors) {
            MeteorCount = numMeteors;
            textures = new List<List<MTexture>> {
                GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/arcComet"),
                GFX.Game.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/slantComet")
            };
            meteors = new List<Meteor>();

            Calc.PushRandom(Calc.Random.Next());
            for (int i = 0; i < MeteorCount; i++) {
                meteors.Add(newMeteor());
            }
            Calc.PopRandom();
        }

        private Meteor newMeteor() {
            return new Meteor {
                Position = new Vector2(Calc.Random.NextFloat(320f), Calc.Random.NextFloat(180f) - 20f),
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
                for (int i = 0; i < meteors.Count; i++) {
                    float newTimer = meteors[i].Timer + Engine.DeltaTime;

                    if (newTimer > meteors[i].ResetTime) {
                        meteors[i] = newMeteor();
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

                for (int i = meteors.Count; i < MeteorCount; i++) {
                    meteors.Add(newMeteor());
                }
                for (int i = meteors.Count - 1; i >= MeteorCount; i--) {
                    if ((int) (meteors[i].Timer * meteors[i].RateFPS) >= textures[meteors[i].TextureSet].Count) {
                        meteors.RemoveAt(i);
                    }
                }
            }
        }

        public override void Render(Scene scene) {
            base.Render(scene);

            foreach (Meteor m in meteors) {
                int textAnimId = (int)(m.Timer * m.RateFPS);

                if (textAnimId < textures[m.TextureSet].Count) {
                    MTexture texture = textures[m.TextureSet][textAnimId];
                    texture.DrawCentered(m.Position, Color.White, 1f, m.Rotation);
                }
            }
        }
    }
}
