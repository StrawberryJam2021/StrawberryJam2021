using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/CassetteBadelineBlock")]
    public class CassetteBadelineBlock : Solid {
        public bool HideFinalTransition { get; }
        public Vector2[] Nodes { get; }
        public int[] IgnoredNodes { get; }
        public bool OffBeat { get; }
        public char TileType { get; }
        public bool PlayImpactSounds { get; }
        public bool EmitImpactParticles { get; }

        private int offsetNodeIndex;
        private int sourceNodeIndex;
        private int targetNodeIndex;
        private readonly int initialNodeIndex;

        public CassetteBadelineBlock(CassetteBadelineBlock parent, int initialNodeIndex)
            : base(parent.Nodes[initialNodeIndex], parent.Width, parent.Height, false) {
            Nodes = parent.Nodes;
            TileType = parent.TileType;
            IgnoredNodes = parent.IgnoredNodes;
            HideFinalTransition = parent.HideFinalTransition;
            OffBeat = parent.OffBeat;
            PlayImpactSounds = parent.PlayImpactSounds;
            EmitImpactParticles = parent.EmitImpactParticles;

            sourceNodeIndex = targetNodeIndex = this.initialNodeIndex = initialNodeIndex;

            Tag = Tags.FrozenUpdate;

            AddComponents();
        }

        public CassetteBadelineBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, false) {
            Nodes = data.NodesWithPosition(offset);
            TileType = data.Char("tiletype", 'g');
            OffBeat = data.Bool("offBeat");
            HideFinalTransition = data.Bool("hideFinalTransition");
            PlayImpactSounds = data.Bool("playImpactSounds", true);
            EmitImpactParticles = data.Bool("emitImpactParticles", true);

            string ignoredNodesString = data.Attr("ignoredNodes") ?? string.Empty;
            IgnoredNodes = ignoredNodesString
                .Trim()
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int value) ? value : int.MaxValue)
                .Where(i => Math.Abs(i) < Nodes.Length)
                .ToArray();

            Tag = Tags.FrozenUpdate;

            AddComponents();
        }

        private void AddComponents() {
            TileGrid sprite = GFX.FGAutotiler.GenerateBox(TileType, (int) Width / 8, (int) Height / 8).TileGrid;
            Add(sprite,
                new TileInterceptor(sprite, false),
                new LightOcclude(),
                new CassetteListener(initialNodeIndex) {
                    OnTick = (_, isSwap) => {
                        if (isSwap != OffBeat) return;
                        offsetNodeIndex++;
                        targetNodeIndex = (initialNodeIndex + offsetNodeIndex) % Nodes.Length;
                    },
                    OnSilentUpdate = activated => {
                        offsetNodeIndex = 0;
                        if (initialNodeIndex < Nodes.Length)
                            Position = Nodes[initialNodeIndex];
                    },
                },
                new Coroutine(MoveSequence())
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

        private void TeleportTo(Vector2 to) {
            MoveStaticMovers(to - Position);
            Position = to;
        }

        private IEnumerator MoveSequence() {
            var block = this;
            
            if (Scene == null) yield break;

            var time = -1f;
            while (Scene != null && time < 0) {
                var cbm = Scene.Tracker.GetEntity<CassetteBlockManager>();
                if (cbm == null) {
                    yield return null;
                } else {
                    var data = DynamicData.For(cbm);
                    var beatsPerTick = data.Get<int>("beatsPerTick");
                    var tempoMult = data.Get<float>("tempoMult");
                    var beatLength = (10 / 60f) / tempoMult;
                    time = beatLength * beatsPerTick;
                }
            }
            
            while (Scene != null) {
                while (sourceNodeIndex == targetNodeIndex) {
                    yield return null;
                }
                
                var to = block.Nodes[targetNodeIndex];
                var from = block.Nodes[sourceNodeIndex];
        
                if (targetNodeIndex == 0 && HideFinalTransition) {
                    block.TeleportTo(to);
                    Visible = Collidable = false;
                } else {
                    var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, time, true);
                    tween.OnUpdate = t => MoveTo(Vector2.Lerp(from, to, t.Eased));
                    tween.OnComplete = _ => {
                        if (block.CollideCheck<SolidTiles>(block.Position + (to - from).SafeNormalize() * 2f)) {
                            if (block.PlayImpactSounds) {
                                Audio.Play("event:/game/06_reflection/fallblock_boss_impact", block.Center);
                            }
                            if (block.EmitImpactParticles) {
                                block.ImpactParticles(to - from);
                            }
                        } else {
                            block.StopParticles(to - from);
                        }
                    };
        
                    block.Add(tween);
                }
        
                yield return time;
                sourceNodeIndex = targetNodeIndex;
                Visible = Collidable = true;
            }
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
