using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using static Celeste.Mod.StrawberryJam2021.Entities.DashBoostField;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class DashBoostFieldParticleRenderer : Component {
        private static readonly Dictionary<Modes, Color[]> colors = new() {
            [Modes.Blue] = new[] {
                Calc.HexToColor("4040ff"),
                Calc.HexToColor("8080ff"),
                Calc.HexToColor("b0b0ff"),
                //Calc.HexToColor("d0d0ff"),
            },
            [Modes.Red] = new[] {
                Calc.HexToColor("ff4040"),
                Calc.HexToColor("ff8080"),
                Calc.HexToColor("ffb0b0"),
                //Calc.HexToColor("ffd0d0"),
            }
        };

        private const float density = 0.02f;

        private List<Particle> particles = new List<Particle>();
        //private int particleCount;

        private DashBoostField BoostField => Entity as DashBoostField;
        private float ParticlePositionRadius => BoostField.Radius * 1.75f;

        public DashBoostFieldParticleRenderer()
            : base(active: true, visible: true) { }

        public override void Added(Entity entity) {
            base.Added(entity);
            int particleCount = (int) (Math.PI * Math.Pow(ParticlePositionRadius, 2) * density);
            for (int i = 0; i < particleCount; i++) {
                particles.Add(new Particle(this));
            }
        }

        public override void Update() {
            base.Update();
            foreach (Particle particle in particles) {
                if (particle.Percent >= 1f)
                    particle.Reset();
                particle.Percent += Engine.DeltaTime / particle.Duration;
                particle.Alpha = (particle.Percent >= 0.7f) 
                    ? Calc.ClampedMap(particle.Percent, 0.7f, 1f, 1f, 0f) 
                    : Calc.ClampedMap(particle.Percent, 0f, 0.3f);

                particle.Position += particle.Velocity * Engine.DeltaTime;
                if (Vector2.Distance(BoostField.Position, particle.Position) <= BoostField.Radius) {
                    particle.RenderPosition = particle.Position;
                } else {
                    // clamp particle's render position to boost field radius to create a sort of halo
                    float angle = Calc.Angle(BoostField.Position, particle.Position);
                    particle.RenderPosition = BoostField.Position + Calc.AngleToVector(angle, BoostField.Radius);
                }
            }
        }

        public override void Render() {
            base.Render();
            foreach (Particle particle in particles) {
                Draw.Point(particle.RenderPosition, particle.Color * particle.Alpha);
            }
        }

        private class Particle {
            public Vector2 Position;
            public Vector2 RenderPosition;
            public Color Color;
            public Vector2 Velocity;
            public float Duration;
            public float Percent;
            public float Alpha;

            private DashBoostFieldParticleRenderer parent;

            public Particle(DashBoostFieldParticleRenderer parent) {
                this.parent = parent;
                Reset(/*Calc.Random.NextFloat()*/);
            }

            public void Reset(float percent = 0f) {
                Percent = percent;
                float offsetX, offsetY;
                Vector2 offset;
                // rejection sampling is apparently not a terrible way to do this
                do {
                    offsetX = Calc.Random.Range(-parent.ParticlePositionRadius, parent.ParticlePositionRadius);
                    offsetY = Calc.Random.Range(-parent.ParticlePositionRadius, parent.ParticlePositionRadius);
                    offset = new Vector2(offsetX, offsetY);
                } while (Vector2.Distance(Vector2.Zero, offset) > parent.ParticlePositionRadius);
                Position = parent.BoostField.Position + offset;
                Color = Calc.Random.Choose(colors[parent.BoostField.Mode]);
                float speed = Calc.Random.Range(4f, 8f);
                Velocity = Calc.AngleToVector(Calc.Random.NextAngle(), speed);
                Duration = Calc.Random.Range(0.6f, 1.5f);
            }
        }
    }
}
