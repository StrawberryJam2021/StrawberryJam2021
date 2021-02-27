using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /* Large parts of this code are copied from Brokemia's non-badaline-dependant moving block helper.
       Thank you to Brokemia for letting me use his code in this project! */

    [CustomEntity("SJ2021/CassetteBadalineBlock")]
    public class CassetteBadalineBlock : Solid {
        private int nodeIndex;
        private Vector2[] nodes;

        private int moveForwardBeat;
        private int moveBackBeat;
        private int preDelay;
        private int transitionDuration;
        private bool oneWay;
        private bool teleportBack;
        private bool alignToCassetteTimer;

        private int lastBeat = -1;

        public CassetteBadalineBlock(Vector2[] nodes, float width, float height, char tiletype,
            int moveForwardBeat, int moveBackBeat, int preDelay, int transitionDuration, bool oneWay,
            bool teleportBack, bool alignToCassetteTimer)
            : base(nodes[0], width, height, false) {
            this.nodes = nodes;
            int newSeed = Calc.Random.Next();
            Calc.PushRandom(newSeed);
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(tiletype, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite);
            Calc.PopRandom();
            Calc.PushRandom(newSeed);
            Calc.PopRandom();
            Add(new TileInterceptor(sprite, false));
            Add(new LightOcclude());

            this.moveForwardBeat = moveForwardBeat;
            this.moveBackBeat = moveBackBeat;
            this.preDelay = preDelay;
            this.transitionDuration = transitionDuration;
            this.oneWay = oneWay;
            this.teleportBack = teleportBack;
            this.alignToCassetteTimer = alignToCassetteTimer;
        }

        public CassetteBadalineBlock(EntityData data, Vector2 offset)
            : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Char("tiletype", 'g'),
                data.Int("moveForwardBeat", 0), data.Int("moveBackBeat", 8), data.Int("preDelay", 0),
                data.Int("transitionDuration", 4), data.Bool("oneWay", false),
                data.Bool("teleportBack", false), data.Bool("alignToCassetteTimer", false)) {
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
            var manager = scene.Tracker.GetEntity<CassetteBlockManager>();

            if (manager == null) {
                var toAddField = scene.Entities.GetType().GetField("toAdd", BindingFlags.NonPublic | BindingFlags.Instance);
                List<Entity> toAdd = toAddField.GetValue(scene.Entities) as List<Entity>;
                var possiblyManager = toAdd.OfType<CassetteBlockManager>().ToArray();

                if (possiblyManager.Any())
                    manager = possiblyManager.First();
                else {
                    manager = new CassetteBlockManager();
                    level.Add(manager);
                }
            }

            if (alignToCassetteTimer) {
                // If we spawn in and we're supposed to be at the end or moving to the end, place us there
                switch (GetCurrentState(manager.GetSixteenthNote())) {
                    case MovingBlockState.MoveToEnd:
                        goto case MovingBlockState.AtEnd;
                    case MovingBlockState.AtEnd:
                        Teleport();

                        break;
                }
            }
        }

        // Note that "AtStart" and "AtEnd" signify that we're either at the start/end or we're actively moving to them
        private enum MovingBlockState {
            AtStart, AtEnd, MoveToStart, MoveToEnd
        }

        private MovingBlockState? GetCurrentState(int beat) {
            // Convert 1-indexing to 0-indexing so we can use modulo on this
            beat = beat - 1;
            int segment = beat % 16;

            if (beat < preDelay)
                return MovingBlockState.AtStart;

            if (segment == moveForwardBeat)
                return MovingBlockState.MoveToEnd;
            if (segment == moveBackBeat)
                return MovingBlockState.MoveToStart;

            if (moveForwardBeat < moveBackBeat) {
                if (segment > moveForwardBeat && segment < moveBackBeat)
                    return MovingBlockState.AtStart;
                if (segment < moveForwardBeat || segment > moveBackBeat)
                    return MovingBlockState.AtEnd;
            } else {
                if (segment > moveBackBeat && segment < moveForwardBeat)
                    return MovingBlockState.AtEnd;
                if (segment < moveBackBeat || segment > moveForwardBeat)
                    return MovingBlockState.AtStart;
            }

            return null;
        }

        public override void Update() {
            base.Update();

            var manager = Scene.Tracker.GetEntity<CassetteBlockManager>();

            if (manager == null)
                return;

            int beat = manager.GetSixteenthNote();

            if (beat == lastBeat)
                return;

            switch (GetCurrentState(beat)) {
                case MovingBlockState.MoveToStart:
                    if (teleportBack) {
                        Teleport();
                        if (oneWay) {
                            moveForwardBeat = -1;
                            moveBackBeat = -1;
                        }

                        break;
                    }
                    goto case MovingBlockState.MoveToEnd;
                case MovingBlockState.MoveToEnd:
                    Move();
                    if (oneWay) {
                        moveForwardBeat = -1;
                        moveBackBeat = -1;
                    }

                    break;
            }

            lastBeat = beat;
        }

        private void Move() {
            // Convert from beats to seconds
            Level level = Engine.Scene as Level;
            float actualTransitionDuration = (float) (transitionDuration * (1.0 / 6.0) * level.CassetteBlockTempo);

            nodeIndex++;
            nodeIndex %= nodes.Length;
            Vector2 from = Position;
            Vector2 to = nodes[nodeIndex];
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, actualTransitionDuration, true);
            tween.OnUpdate = delegate(Tween t) {
                MoveTo(Vector2.Lerp(from, to, t.Eased));
            };
            tween.OnComplete = delegate {
                if (CollideCheck<SolidTiles>(Position + (to - from).SafeNormalize() * 2f)) {
                    Audio.Play("event:/game/06_reflection/fallblock_boss_impact", Center);
                    ImpactParticles(to - from);
                } else {
                    StopParticles(to - from);
                }
            };
            Add(tween);
        }

        private void Teleport() {
            nodeIndex++;
            nodeIndex %= nodes.Length;
            Vector2 to = nodes[nodeIndex];
            Position = to;
        }

        private void StopParticles(Vector2 moved) {
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

        private void ImpactParticles(Vector2 moved) {
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
