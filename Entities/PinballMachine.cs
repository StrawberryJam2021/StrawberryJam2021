using Monocle;
using System;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PinballMachine")]
    class PinballMachine : Entity {
        private struct Debris {
            public Vector2 Direction;
            public float Percent;
            public float Duration;
            public bool Enabled;
        }
        private TalkComponent talk;
        private Sprite sprite;
        private Debris[] debris = new Debris[50];
        private Color debrisColorFrom = Calc.HexToColor("f442d4");
        private Color debrisColorTo = Calc.HexToColor("000000");
        private MTexture debrisTexture = GFX.Game["particles/blob"];
        private float bufferAlpha;
        private float bufferTimer;
        public float DistortionFade = 1f;
        public PinballMachine(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Depth = 2000;
            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 40), new Vector2(-0.5f, -20f), Interact));
            talk.PlayerMustBeFacing = false;
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("pinballMachine"));
            sprite.CenterOrigin();
        }
        public void Activate() {
            Add(new Coroutine(ActivateRoutine()));
        }
        public IEnumerator ActivateRoutine() {

            Level level = SceneAs<Level>();
            float debrisStart = 0f;
            sprite.Play("consoleLights");
            Audio.Play("event:/classic/sfx2");
            yield return .3f;
            Audio.Play("event:/classic/sfx1");
            yield return .24f;
            Audio.Play("event:/classic/sfx7");
            yield return 1f;
            Audio.Play("event:/classic/pico8_boot");
            yield return 2.5f;
            Add(new DisplacementRenderHook(RenderDisplacement));
            LightingRenderer light = level.Lighting;
            sprite.Play("glitchLoop");
            yield return .5f;
            sprite.Play("glitchEnd");
            while (true) {
                bufferAlpha = Calc.Approach(bufferAlpha, 1f, Engine.DeltaTime);
                bufferTimer += 4f * Engine.DeltaTime;
                light.Alpha = Calc.Approach(light.Alpha, 0.2f, Engine.DeltaTime * 0.05f);
                if (debrisStart < (float) debris.Length) {
                    int num = (int) debrisStart;
                    debris[num].Direction = Calc.AngleToVector(Calc.Random.NextFloat((float) Math.PI * 2f), 1f);
                    debris[num].Enabled = true;
                    debris[num].Duration = 0.5f + Calc.Random.NextFloat(0.7f);
                }
                debrisStart += Engine.DeltaTime * 10f;
                for (int i = 0; i < debris.Length; i++) {
                    if (debris[i].Enabled) {
                        debris[i].Percent %= 1f;
                        debris[i].Percent += Engine.DeltaTime / debris[i].Duration;
                    }
                }
                yield return null;
            }
        }

        private void RenderDisplacement() {
            Draw.Rect(base.X - 28f, base.Y - 31f, 54f, 30f, new Color(0.5f, 0.5f, 0.25f * DistortionFade * bufferAlpha, 1f));
        }
        public void Interact(Player player) {
            Scene.Add(new PinballIntroCutscene(player, this));
        }

        public override void Render() {
            base.Render();
            Level level = SceneAs<Level>();
      
            for (int i = 0; i < debris.Length; i++) {
                Debris debris = this.debris[i];
                if (debris.Enabled) {
                    switch (i % 3) {
                        case 0:
                            debrisColorFrom = Calc.HexToColor("ff5cf0");
                            break;
                        case 1:
                            debrisColorFrom = Calc.HexToColor("82ffff");
                            break;
                        case 2:
                            debrisColorFrom = Calc.HexToColor("ff4a4a");
                            break;     
                    }
                    float num = Ease.SineOut(debris.Percent);
                    Vector2 position = Position + debris.Direction * (1f - num) * (190f - level.Zoom * 30f);
                    Color color = Color.Lerp(debrisColorFrom, debrisColorTo, num);
                    float scale = Calc.LerpClamp(1f, 0.2f, num);
                    debrisTexture.DrawCentered((position - new Vector2(0, 15f)), color, scale, (float) i * 0.05f);
                }
            }
        }
    }
}
