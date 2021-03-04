using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteConveyorBlock")]
    public class CassetteConveyorBlock : CassetteTimedBlock {
        private int nodeIndex;
        private readonly Vector2[] nodes;
        private readonly char tiletype;
        private readonly int waitTime;
        private readonly int transitionDuration;
        private readonly int preDelay;
        private readonly int position;

        public CassetteConveyorBlock(Vector2[] nodes, float width, float height, char tiletype, int waitTime, int transitionDuration, int preDelay, int position = 0)
            : base(nodes[0], width, height, false) {
            this.nodes = nodes;
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(tiletype, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite);
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

            // Align position when spawning
            var conveyorState = GetConveyorState(false);
            int previousIndex = conveyorState.Index - 1 < 0 ? nodes.Length - 1 : conveyorState.Index - 1;
            Teleport(conveyorState.MoveThisFrame ? previousIndex : conveyorState.Index);

            // We are the group leader
            if (position == 0) {
                for (int i = 1; i < nodes.Length; ++i) {
                    Scene.Add(new CassetteConveyorBlock(nodes, Width, Height, tiletype, waitTime, transitionDuration, preDelay, i));
                }
            }
        }

        private readonly struct ConveyorState {
            public readonly int Index;
            public readonly bool MoveThisFrame;

            public ConveyorState(int index, bool moveThisFrame) {
                Index = index;
                MoveThisFrame = moveThisFrame;
            }
        }

        /**
         * Returns the current index we are supposed to be at, as well as whether we should move this frame.
         */
        private ConveyorState GetConveyorState(bool updateLastBeat = true) {
            var timerState = GetCassetteTimerState(updateLastBeat);

            if (!timerState.HasValue || !timerState.Value.ChangedSinceLastBeat)
                return new ConveyorState(nodeIndex, false);

            int beat = timerState.Value.Beat;
            beat -= preDelay;

            if (beat < 0)
                return new ConveyorState(nodeIndex, false);

            int cycleLength = transitionDuration + waitTime;

            int targetNodeIndex = (beat / cycleLength + position + 1) % nodes.Length;

            return new ConveyorState(targetNodeIndex, beat % cycleLength == 0);
        }

        public override void Update() {
            base.Update();

            var state = GetConveyorState();

            if (!state.MoveThisFrame)
                return;

            if (state.Index == 0)
                Teleport(0);
            else
                Move();
        }

        private void Move() {
            // Convert from beats to seconds
            Level level = SceneAs<Level>();
            float actualTransitionDuration = transitionDuration * (1f / 6f) * level.CassetteBlockTempo;

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
    }
}
