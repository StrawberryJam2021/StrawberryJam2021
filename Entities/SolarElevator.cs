using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SolarElevator")]
    public class SolarElevator : Solid {
        private readonly TalkComponent interaction;
        private readonly SoundSource moveSfx;

        private readonly int distance;
        private readonly float time;

        private bool enabled = false;
        private bool atGroundFloor = true;

        public SolarElevator(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int("distance", 128), data.Float("time", 3.0f)) { }

        public SolarElevator(Vector2 position, int distance, float time)
            : base(position, 56, 80, safe: true) {
            Depth = Depths.Player - 1;
            SurfaceSoundIndex = SurfaceIndex.MoonCafe;

            this.distance = distance;
            this.time = time;

            Collider = new Hitbox(48, 5, -24, 0);

            Add(moveSfx = new());

            Add(interaction = new TalkComponent(new Rectangle(-12, -8, 24, 8), Vector2.UnitY * -16, Activate));

            Image img = new(GFX.Game["objects/StrawberryJam2021/solarElevator/elevator"]);
            img.JustifyOrigin(0.5f, 1.0f);
            img.Position.Y = 10;
            Add(img);
        }

        private void Activate(Player player) {
            if (!enabled)
                Add(new Coroutine(Sequence()));
        }

        private IEnumerator Sequence() {
            enabled = true;
            interaction.Enabled = false;
            Audio.Play(SFX.game_10_ppt_mouseclick, Position);

            yield return 1f;

            moveSfx.Play(SFX.game_04_gondola_cliffmechanism_start);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.15f);

            float start = Y;
            float end = atGroundFloor ? (start - distance) : (start + distance);
            float t = 0.0f;
            while (t < time) {
                float percent = t / time;
                MoveToY(start + (end - start) * percent);

                t = Calc.Approach(t, time, Engine.DeltaTime);
                yield return null;
            }

            MoveToY(end);
            moveSfx.Stop();

            enabled = false;
            interaction.Enabled = true;
            atGroundFloor = !atGroundFloor;
        }
    }
}
