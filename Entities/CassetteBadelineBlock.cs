using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteBadelineBlock")]
    public class CassetteBadelineBlock : Solid {
        public bool HideFinalTransition { get; }
        public Vector2[] Nodes { get; }
        public int[] IgnoredNodes { get; }
        public bool OffBeat { get; }
        public char TileType { get; }

        private int offsetNodeIndex;
        private int sourceNodeIndex;
        private int targetNodeIndex;
        private Vector2 sourcePosition;
        private Vector2 targetPosition;
        private float moveTimeRemaining;

        private Vector2 lerpedPosition {
            get {
                float progress = 1 - moveTimeRemaining / cassetteListener.CurrentState.TickLength;
                float eased = MathHelper.Clamp(Ease.CubeIn(progress), 0, 1);
                return Vector2.Lerp(sourcePosition, targetPosition, eased);
            }
        }

        private readonly int initialNodeIndex;
        private CassetteListener cassetteListener;

        public CassetteBadelineBlock(CassetteBadelineBlock parent, int initialNodeIndex)
            : base(parent.Nodes[initialNodeIndex], parent.Width, parent.Height, false) {
            Nodes = parent.Nodes;
            TileType = parent.TileType;
            IgnoredNodes = parent.IgnoredNodes;
            HideFinalTransition = parent.HideFinalTransition;
            OffBeat = parent.OffBeat;

            this.initialNodeIndex = initialNodeIndex;
            sourcePosition = targetPosition = Position;
            moveTimeRemaining = 0;

            Tag = Tags.FrozenUpdate;

            AddComponents();
        }

        public CassetteBadelineBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, false) {
            Nodes = data.NodesWithPosition(offset);
            TileType = data.Char("tiletype", 'g');
            OffBeat = data.Bool("offBeat");
            HideFinalTransition = data.Bool("hideFinalTransition");

            string ignoredNodesString = data.Attr("ignoredNodes") ?? string.Empty;
            IgnoredNodes = ignoredNodesString
                .Trim()
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int value) ? value : int.MaxValue)
                .Where(i => Math.Abs(i) < Nodes.Length)
                .ToArray();

            sourcePosition = targetPosition = Position;
            moveTimeRemaining = 0;

            Tag = Tags.FrozenUpdate;

            AddComponents();
        }

        private void AddComponents() {
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(TileType, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite,
                new TileInterceptor(sprite, false),
                new LightOcclude(),
                cassetteListener = new CassetteListener {
                    OnBeat = state => {
                        bool indexWillChange = state.NextTick.Index != state.CurrentTick.Index;
                        if (OffBeat != indexWillChange && moveTimeRemaining <= 0) {
                            if (offsetNodeIndex < 0)
                                offsetNodeIndex = state.NextTick.Index;
                            else
                                offsetNodeIndex++;

                            sourceNodeIndex = (initialNodeIndex + offsetNodeIndex) % Nodes.Length;
                            targetNodeIndex = (sourceNodeIndex + 1) % Nodes.Length;
                            sourcePosition = Nodes[sourceNodeIndex];
                            targetPosition = Nodes[targetNodeIndex];

                            float mod = (state.Beat % state.BeatsPerTick) / (float)state.BeatsPerTick;
                            moveTimeRemaining = state.TickLength * (1 - mod);

                            bool shouldTeleport = targetNodeIndex == 0 && HideFinalTransition;
                            TeleportTo(shouldTeleport ? targetPosition : lerpedPosition);
                        }
                    },
                }
            );
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            offsetNodeIndex = -1;

            if (initialNodeIndex == 0) {
                for (int i = 1; i < Nodes.Length; i++) {
                    if (!IgnoredNodes.Contains(i) && !IgnoredNodes.Contains(i - Nodes.Length))
                        scene.Add(new CassetteBadelineBlock(this, i));
                }

                if (IgnoredNodes.Contains(0))
                    RemoveSelf();
            }
        }

        public override void Update() {
            base.Update();

            Visible = Collidable = !HideFinalTransition || moveTimeRemaining <= 0 || targetNodeIndex != 0;

            if (moveTimeRemaining >= 0) {
                moveTimeRemaining -= Engine.DeltaTime;

                if (!HideFinalTransition || targetNodeIndex != 0)
                    MoveTo(lerpedPosition);

                if (moveTimeRemaining < 0) {
                    var moveAmount = targetPosition - sourcePosition;
                    if (Math.Abs(moveAmount.LengthSquared()) > 0.1f) {
                        if (CollideCheck<SolidTiles>(Position + moveAmount.SafeNormalize() * 2f)) {
                            Audio.Play("event:/game/06_reflection/fallblock_boss_impact", Center);
                            ImpactParticles(moveAmount);
                        } else {
                            StopParticles(moveAmount);
                        }
                    }
                }
            }
        }

        private void TeleportTo(Vector2 to) {
            MoveStaticMovers(to - Position);
            Position = to;
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
