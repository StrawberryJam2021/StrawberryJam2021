using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    /* Large parts of this code are copied from Brokemia's non-badeline-dependant moving block helper.
       Thank you to Brokemia for letting me use his code in this project! */

    [CustomEntity("SJ2021/CassetteBadelineBlock")]
    public class CassetteBadelineBlock : CassetteTimedBlock {
        private int nodeIndex;
        private readonly Vector2[] nodes;

        private int moveForwardBeat;
        private int moveBackBeat;
        private readonly int preDelay;
        private readonly int transitionDuration;
        private readonly bool oneWay;
        private readonly bool teleportBack;

        public CassetteBadelineBlock(Vector2[] nodes, float width, float height, char tiletype,
            int moveForwardBeat, int moveBackBeat, int preDelay, int transitionDuration, bool oneWay,
            bool teleportBack)
            : base(nodes[0], width, height, false) {
            this.nodes = nodes;
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(tiletype, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite);
            Add(new TileInterceptor(sprite, false));
            Add(new LightOcclude());

            this.moveForwardBeat = moveForwardBeat;
            this.moveBackBeat = moveBackBeat;
            this.preDelay = preDelay;
            this.transitionDuration = transitionDuration;
            this.oneWay = oneWay;
            this.teleportBack = teleportBack;
        }

        public CassetteBadelineBlock(EntityData data, Vector2 offset)
            : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Char("tiletype", 'g'),
                data.Int("moveForwardBeat", 0), data.Int("moveBackBeat", 8), data.Int("preDelay", 0),
                data.Int("transitionDuration", 4), data.Bool("oneWay", false), data.Bool("teleportBack", false)) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            // If we spawn in and we're supposed to be at the end or moving from the end, place us there
            var timerState = GetCassetteTimerState(false);
            var blockState = GetMovingBlockState(timerState.Value.Beat);

            if (blockState == MovingBlockState.MoveToStart || blockState == MovingBlockState.AtEnd)
                Teleport();
        }

        // "AtStart" and "AtEnd" signify that we're either at the start/end or we're actively moving to them
        // "MoveToStart" and "MoveToEnd" signify that we should move to the start (or the end) *on this frame*
        private enum MovingBlockState {
            AtStart, AtEnd, MoveToStart, MoveToEnd
        }

        private MovingBlockState GetMovingBlockState(int beat) {
            int segment = beat % 16;

            if (beat < preDelay)
                return MovingBlockState.AtStart;

            if (segment == moveForwardBeat)
                return MovingBlockState.MoveToEnd;
            if (segment == moveBackBeat)
                return MovingBlockState.MoveToStart;

            if (moveForwardBeat < moveBackBeat) {
                if (segment > moveForwardBeat && segment < moveBackBeat)
                    return MovingBlockState.AtEnd;
                else // (segment < moveForwardBeat || segment > moveBackBeat)
                    return MovingBlockState.AtStart;
            } else {
                if (segment > moveBackBeat && segment < moveForwardBeat)
                    return MovingBlockState.AtStart;
                else // (segment < moveBackBeat || segment > moveForwardBeat)
                    return MovingBlockState.AtEnd;
            }
        }

        public override void Update() {
            base.Update();

            var timerState = GetCassetteTimerState();

            if (!timerState.HasValue || !timerState.Value.ChangedSinceLastBeat)
                return;

            var blockState = GetMovingBlockState(timerState.Value.Beat);
            if (blockState == MovingBlockState.MoveToStart || blockState == MovingBlockState.MoveToEnd) {
                if (blockState == MovingBlockState.MoveToStart && teleportBack)
                    Teleport();
                else
                    Move();

                if (oneWay) {
                    moveForwardBeat = -1;
                    moveBackBeat = -1;
                }
            }
        }

        private void Move() {
            float actualTransitionDuration = BeatsToSeconds(transitionDuration);

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
            MoveStaticMovers(to - Position);
            Position = to;
        }
    }
}
