using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public abstract class CassetteTimedBlock : Solid {
        private CassetteBlockManager manager;
        private int lastBeat = -1;
        private int cassetteResetOffset;

        protected CassetteTimedBlock(Vector2 position, float width, float height, bool safe) : base(position, width, height, safe) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            Level level = scene as Level;

            // This flag tells CassetteBlockManager to do its thing
            if (!level.HasCassetteBlocks) {
                level.HasCassetteBlocks = true;
            }

            // Add a cassette manager if there isn't one already
            // just using scene.Tracker.GetEntity<CassetteBlockManager>() == null doesn't work here
            // because EntityList.Add only adds in batches when we explicitly tell it to update
            // so, reflection saves the day again :)
            manager = scene.Tracker.GetEntity<CassetteBlockManager>();

            if (manager != null) return;

            List<Entity> toAdd = scene.Entities
                .GetType().GetField("toAdd", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(scene.Entities) as List<Entity>;
            var possiblyManager = toAdd.OfType<CassetteBlockManager>().ToArray();

            if (possiblyManager.Any())
                manager = possiblyManager.First();
            else {
                manager = new CassetteBlockManager();
                level.Add(manager);
            }
        }

        protected readonly struct CassetteTimerState {
            public readonly int Beat;
            public readonly bool ChangedSinceLastBeat;

            public CassetteTimerState(int beat, bool changedSinceLastBeat) {
                Beat = beat;
                ChangedSinceLastBeat = changedSinceLastBeat;
            }
        }

        /**
         * Returns the normalized value of the current cassette sixteenth note. "Normalized" means that it keeps ticking past resets when the cassette loop restarts.
         * The return value of this method is also 0-based, unlike the 1-based manager. Returns null on failure. You should call this function exactly once per frame.
         */
        protected CassetteTimerState? GetCassetteTimerState(bool updateLastBeat = true) {
            int beat;

            try {
                beat = manager.GetSixteenthNote();
            } catch (NullReferenceException) {
                return null;
            }

            beat = beat + cassetteResetOffset - 1;

            // We reset to zero
            // Adjust offset to pretend we didn't reset
            if (beat < lastBeat) {
                beat -= cassetteResetOffset;
                cassetteResetOffset = lastBeat + 1;
                beat += cassetteResetOffset;
            }

            var res = new CassetteTimerState(beat, lastBeat != beat);
            if (updateLastBeat)
                lastBeat = beat;

            return res;
        }

        protected void StopParticles(Vector2 moved) {
            Level level = SceneAs<Level>();
            float direction = moved.Angle();
            if (moved.X > 0f) {
                Vector2 value = new Vector2(Right - 1f, Top);
                for (int i = 0; i < Height; i += 4) {
                    level.Particles.Emit(FinalBossMovingBlock.P_Stop, value + Vector2.UnitY * (2 + i + Calc.Random.Range(-1, 1)), direction);
                }
            } else if (moved.X < 0f) {
                Vector2 value2 = new Vector2(Left, Top);
                for (int j = 0; j < Height; j += 4) {
                    level.Particles.Emit(FinalBossMovingBlock.P_Stop, value2 + Vector2.UnitY * (2 + j + Calc.Random.Range(-1, 1)), direction);
                }
            }
            if (moved.Y > 0f) {
                Vector2 value3 = new Vector2(Left, Bottom - 1f);
                for (int k = 0; k < Width; k += 4) {
                    level.Particles.Emit(FinalBossMovingBlock.P_Stop, value3 + Vector2.UnitX * (2 + k + Calc.Random.Range(-1, 1)), direction);
                }
            } else if (moved.Y < 0f) {
                Vector2 value4 = new Vector2(Left, Top);
                for (int l = 0; l < Width; l += 4) {
                    level.Particles.Emit(FinalBossMovingBlock.P_Stop, value4 + Vector2.UnitX * (2 + l + Calc.Random.Range(-1, 1)), direction);
                }
            }
        }

        protected void ImpactParticles(Vector2 moved) {
            if (moved.X < 0f) {
                Vector2 offset = new Vector2(0f, 2f);
                for (int i = 0; i < Height / 8f; i++) {
                    Vector2 collideCheckPos = new Vector2(Left - 1f, Top + 4f + (i * 8));
                    if (!Scene.CollideCheck<Water>(collideCheckPos) && Scene.CollideCheck<Solid>(collideCheckPos)) {
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos + offset, 0f);
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos - offset, 0f);
                    }
                }
            } else if (moved.X > 0f) {
                Vector2 offset = new Vector2(0f, 2f);
                for (int j = 0; j < Height / 8f; j++) {
                    Vector2 collideCheckPos = new Vector2(Right + 1f, Top + 4f + (j * 8));
                    if (!Scene.CollideCheck<Water>(collideCheckPos) && Scene.CollideCheck<Solid>(collideCheckPos)) {
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos + offset, (float) Math.PI);
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos - offset, (float) Math.PI);
                    }
                }
            }
            if (moved.Y < 0f) {
                Vector2 offset = new Vector2(2f, 0f);
                for (int k = 0; k < Width / 8f; k++) {
                    Vector2 collideCheckPos = new Vector2(Left + 4f + (k * 8), Top - 1f);
                    if (!Scene.CollideCheck<Water>(collideCheckPos) && Scene.CollideCheck<Solid>(collideCheckPos)) {
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos + offset, (float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos - offset, (float) Math.PI / 2f);
                    }
                }
            } else {
                if (!(moved.Y > 0f)) {
                    return;
                }
                Vector2 offset = new Vector2(2f, 0f);
                for (int l = 0; l < Width / 8f; l++) {
                    Vector2 collideCheckPos = new Vector2(Left + 4f + (l * 8), Bottom + 1f);
                    if (!Scene.CollideCheck<Water>(collideCheckPos) && Scene.CollideCheck<Solid>(collideCheckPos)) {
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos + offset, -(float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, collideCheckPos - offset, -(float) Math.PI / 2f);
                    }
                }
            }
        }
    }
}
