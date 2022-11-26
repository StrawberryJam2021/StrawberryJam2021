using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/LaunchPlatform")]
    public class LaunchPlatform : JumpThru {
        private readonly Vector2 start;
        private readonly Vector2 end;
        private readonly SoundSource sfx;
        private float addY;
        private float sinkTimer;
        
        private MTexture[] textures;
        private PlatformState state = PlatformState.Start;
        
        public LaunchPlatform(Vector2 position, int width, Vector2 node) : base(position, width, false) {
            start = Position;
            end = node;
            SurfaceSoundIndex = SurfaceIndex.Wood;
            
            Add(sfx = new SoundSource());
            Add(new LightOcclude(0.2f));
            Add(new Coroutine(Sequence()));
        }

        public LaunchPlatform(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Nodes[0] + offset) {
        }
        
        public override void Added(Scene scene) {
            base.Added(scene);

            var texture = GFX.Game["objects/StrawberryJam2021/launchPlatform/default"];
            textures = Enumerable
                .Range(0, texture.Width / 8)
                .Select(i => texture.GetSubtexture(i * 8, 0, 8, 8))
                .ToArray();

            var offset = new Vector2(Width, Height + 4f) / 2f;
            scene.Add(new MovingPlatformLine(start + offset, end + offset));
        }
        
        public override void Render() {
            textures[0].Draw(Position);
            for (int x = 8; x < Width - 8; x += 8) {
                textures[1].Draw(Position + new Vector2(x, 0.0f));
            }
            textures[3].Draw(Position + new Vector2(Width - 8f, 0.0f));
            textures[2].Draw(Position + new Vector2((float) (Width / 2.0 - 4.0), 0.0f));
        }

        public override void Update() {
            base.Update();

            if (HasPlayerRider()) {
                sinkTimer = 0.2f;
                addY = Calc.Approach(addY, 3f, 50f * Engine.DeltaTime);
            } else if (sinkTimer > 0f) {
                sinkTimer -= Engine.DeltaTime;
                addY = Calc.Approach(addY, 3f, 50f * Engine.DeltaTime);
            } else {
                addY = Calc.Approach(addY, 0f, 20f * Engine.DeltaTime);
            }

            if (state is PlatformState.Start or PlatformState.End) {
                var target = (state == PlatformState.Start ? start : end) + Vector2.UnitY * addY;
                if (target != Position) MoveTo(target);
            }
        }
        
        // ReSharper disable IteratorNeverReturns
        private IEnumerator Sequence() {
            var platform = this;
            while (true) {
                platform.state = PlatformState.Start;
                while (!platform.HasPlayerRider()) {
                    yield return null;
                }

                platform.sfx.Play("event:/game/01_forsaken_city/zip_mover");
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                platform.StartShaking(0.1f);
                yield return 0.1f;

                platform.state = PlatformState.Launching;
                float at = 0f;
                while (at < 1f) {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                    platform.MoveTo(Vector2.Lerp(start, end, Ease.CubeIn(at)) + Vector2.UnitY * platform.addY);
                }
                platform.state = PlatformState.End;

                platform.StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                platform.SceneAs<Level>().Shake();
                yield return 0.5f;

                platform.state = PlatformState.Returning;
                at = 0f;
                while (at < 1f) {
                    yield return null;
                    at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                    platform.MoveTo(Vector2.Lerp(end, start, at) + Vector2.UnitY * addY);
                }
                platform.state = PlatformState.Start;
                
                platform.StartShaking(0.2f);
                yield return 0.5f;
            }
        }

        private enum PlatformState {
            Start,
            Launching,
            End,
            Returning,
        }
    }
}
