using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteConveyorBlock")]
    public class CassetteConveyorBlock : Solid {
        private int nodeIndex;
        private readonly Vector2[] nodes;
        private readonly char tiletype;
        private readonly int waitTime;
        private readonly int transitionDuration;
        private int preDelay;
        private readonly int position;

        private int lastBeat = -1;
        private int cassetteResetOffset = 0;

        public CassetteConveyorBlock(Vector2[] nodes, float width, float height, char tiletype, int waitTime, int transitionDuration, int preDelay, int position = 0)
            : base(nodes[0], width, height, false) {
            this.nodes = nodes;
            int newSeed = Calc.Random.Next();
            Calc.PushRandom(newSeed);
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(tiletype, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite);
            Calc.PopRandom();
            Add(new TileInterceptor(sprite, false));
            Add(new LightOcclude());

            this.tiletype = tiletype;
            this.position = position;
            this.waitTime = waitTime;
            this.transitionDuration = transitionDuration;
            this.preDelay = preDelay;
        }

        public CassetteConveyorBlock(EntityData data, Vector2 offset)
            : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Char("tiletype", 'g'),
                data.Int("waitTime", 12), data.Int("transitionDuration", 4), data.Int("preDelay", 0)) {
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

            // Align position when spawning
            int beat = manager.GetSixteenthNote();

            // Special case when we spawn in to line up nicely
            Teleport(beat == 1 ? position : GetCurrentIndex(beat));

            // We are the group leader
            if (position == 0) {
                for (int i = 1; i < nodes.Length; ++i) {
                    Scene.Add(new CassetteConveyorBlock(nodes, Width, Height, tiletype, waitTime, transitionDuration, preDelay, i));
                }
            }
        }

        // Get the index that we're supposed to be at / moving to
        private int GetCurrentIndex(int beat) {
            // Convert 1-indexing to 0-indexing so we can use modulo on this
            beat -= 1;
            beat += cassetteResetOffset;
            beat -= preDelay;

            // We reset to zero
            // Adjust offset to pretend we didn't reset
            if (beat < lastBeat) {
                cassetteResetOffset = lastBeat;
                beat += cassetteResetOffset;
            }

            if (beat == lastBeat)
                return nodeIndex;

            lastBeat = beat;

            if (beat < 0)
                return nodeIndex;

            int cycleLength = transitionDuration + waitTime;

            return (beat / cycleLength + position + 1) % nodes.Length;
        }

        public override void Update() {
            base.Update();

            var manager = Scene.Tracker.GetEntity<CassetteBlockManager>();

            if (manager == null)
                return;

            int index = GetCurrentIndex(manager.GetSixteenthNote());

            if (index == nodeIndex)
                return;

            if (index == 0)
                Teleport(0);
            else
                Move();
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
                StopParticles(to - from);
            };

            Add(tween);
        }

        private void Teleport(int index) {
            nodeIndex = index;
            Vector2 to = nodes[nodeIndex];
            MoveStaticMovers(to - Position);
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
    }
}
