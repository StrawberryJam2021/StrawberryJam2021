using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/BrushLaserEmitterUp = LoadUp",
        "SJ2021/BrushLaserEmitterDown = LoadDown",
        "SJ2021/BrushLaserEmitterLeft = LoadLeft",
        "SJ2021/BrushLaserEmitterRight = LoadRight")]
    public class BrushLaserEmitter : OrientableEntity {
        #region Static Loader Methods

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Up);

        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Down);

        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Left);

        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new BrushLaserEmitter(data, offset, Orientations.Right);

        #endregion

        #region Ahorn Properties

        public bool CollideWithSolids { get; }
        public bool KillPlayer { get; }
        public int CassetteIndex { get; }
        public bool HalfLength { get; }

        protected int DataWidth { get; }
        protected int DataHeight { get; }

        protected int Size => Orientation.Vertical() ? DataWidth : DataHeight;
        protected int Tiles => Size / tileSize;

        #endregion

        private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
        private string chargingAnimation => $"{animationPrefix}_charging";
        private string firingAnimation => $"{animationPrefix}_firing";
        private string burstAnimation => $"{animationPrefix}_burst";
        private string idleAnimation => $"{animationPrefix}_idle";
        private string cooldownAnimation => $"{animationPrefix}_cooldown";
        private string backAnimation => $"{animationPrefix}_a";
        private Vector2 beamOffset => Orientation.Normal() * beamOffsetMultiplier;
        private Color telegraphColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);
        private Color beamFillColor => CassetteIndex == 0 ? Calc.HexToColor("73efe8") : Calc.HexToColor("ff8eae");

        private readonly Sprite largeEmitterSprite;
        private readonly Sprite smallEmitterSprite;
        private readonly Sprite beamSprite;

        private readonly Collider[] emitterHitboxes;
        private readonly Hitbox[] laserHitboxes;
        private readonly ColliderList inactiveColliderList;
        private readonly ColliderList activeColliderList;
        private readonly CassetteListener cassetteListener;
        private LaserState laserState;
        private bool needsForcedUpdate;

        private const float chargeDelayFraction = 0.25f;
        private const float collisionDelaySeconds = 5f / 60f;
        private const int beamOffsetMultiplier = 4;
        private const int beamThickness = 12;
        private const float mediumRumbleEffectRange = 8f * 12;
        private const float strongRumbleEffectRange = 8f * 8;
        private const int tileSize = 8;

        private float collisionDelayRemaining;

        private static readonly ParticleType blueCooldownParticle = new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/blob"],
            Color = Calc.HexToColor("42bfe8"),
            Color2 = Calc.HexToColor("7550e8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.5f,
            LifeMax = 0.8f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 5.712389f,
            DirectionRange = 1.17453292f,
            SpeedMin = 40f,
            SpeedMax = 100f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        private static readonly ParticleType pinkCooldownParticle = new ParticleType(blueCooldownParticle) {
            Color = Calc.HexToColor("e84292"),
            Color2 = Calc.HexToColor("9c2a70"),
        };

        private static readonly ParticleType blueImpactParticle = new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/fire"],
            Color = Calc.HexToColor("ffffff"),
            Color2 = Calc.HexToColor("73efe8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.3f,
            LifeMax = 0.5f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 4.712389f,
            DirectionRange = 3.14159f,
            SpeedMin = 10f,
            SpeedMax = 80f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        private static readonly ParticleType pinkImpactParticle = new ParticleType(blueImpactParticle) {
            Color2 = Calc.HexToColor("ef73bf"),
        };

        private void setAnimationSpeed(string key, float totalRunTime) {
            if (largeEmitterSprite.Animations.TryGetValue(key, out var emitterAnimation))
                emitterAnimation.Delay = totalRunTime / emitterAnimation.Frames.Length;
            if (beamSprite.Animations.TryGetValue(key, out var beamAnimation))
                beamAnimation.Delay = totalRunTime / beamAnimation.Frames.Length;
        }

        public LaserState State {
            get => laserState;
            set {
                if (laserState == value)
                    return;

                var oldState = laserState;
                laserState = value;

                switch (value) {
                    case LaserState.Idle:
                    case LaserState.Precharge:
                        if (value == LaserState.Idle && oldState == LaserState.Firing) {
                            emitCooldownParticles();
                            largeEmitterSprite.Play(cooldownAnimation);
                        } else {
                            largeEmitterSprite.Play(idleAnimation);
                        }

                        beamSprite.Visible = false;
                        Collider = inactiveColliderList;
                        break;

                    case LaserState.Charging:
                        largeEmitterSprite.Play(chargingAnimation);
                        beamSprite.Visible = false;
                        Collider = inactiveColliderList;

                        break;

                    case LaserState.Burst:
                        largeEmitterSprite.Play(burstAnimation);
                        playNearbyEffects();
                        beamSprite.Visible = true;
                        beamSprite.Play(burstAnimation);
                        Collider = inactiveColliderList;
                        collisionDelayRemaining = collisionDelaySeconds;
                        break;

                    case LaserState.Firing:
                        beamSprite.Visible = true;
                        collisionDelayRemaining = 0;
                        Collider = activeColliderList;

                        if (oldState != LaserState.Burst) {
                            largeEmitterSprite.Play(firingAnimation);
                            beamSprite.Play(firingAnimation);
                        }

                        break;
                }
            }
        }

        private void playNearbyEffects() {
            if (Scene.Tracker.Entities.ContainsKey(typeof(Player)) && Scene.Tracker.GetEntity<Player>() is { } player) {
                float distanceSquared = (player.Position - Position).LengthSquared();
                if (distanceSquared <= strongRumbleEffectRange * strongRumbleEffectRange) {
                    SceneAs<Level>().Shake(0.2f);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                } else if (distanceSquared <= mediumRumbleEffectRange * mediumRumbleEffectRange) {
                    SceneAs<Level>().Shake(0.1f);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                } else {
                    Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
                }
            }
        }

        public BrushLaserEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            CollideWithSolids = data.Bool("collideWithSolids", true);
            KillPlayer = data.Bool("killPlayer", true);
            CassetteIndex = data.Int("cassetteIndex", 0);
            HalfLength = data.Bool("halfLength");
            DataWidth = data.Width;
            DataHeight = data.Height;

            largeEmitterSprite = configureSprite(StrawberryJam2021Module.SpriteBank.Create("brushLaserEmitter"));
            smallEmitterSprite = configureSprite(StrawberryJam2021Module.SpriteBank.Create("brushLaserEmitterSmall"));

            largeEmitterSprite.Play(idleAnimation);
            smallEmitterSprite.Play("pink_a");

            // emitterSprites = CreateEmitterSprites().ToArray();
            // backSprites = CreateBackSprites().ToArray();
            //
            // foreach (var emitterSprite in emitterSprites)
            //     emitterSprite.Play(idleAnimation);
            //
            // foreach (var backSprite in backSprites)
            //     backSprite.Play(backAnimation);

            beamSprite = StrawberryJam2021Module.SpriteBank.Create("brushLaserBeam");
            beamSprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up ? new Vector2(-1, 1) : Vector2.One;
            beamSprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down ? (float)Math.PI / 2f : 0f;
            beamSprite.Position = beamOffset;
            beamSprite.Visible = false;

            Add(cassetteListener = new CassetteListener {
                    OnBeat = state => {
                        int beat = state.Beat % state.BeatsPerTick;
                        float beatFraction = (float)beat / state.BeatsPerTick;

                        switch (State) {
                            case LaserState.Idle:
                                if (beat == 0 && state.CurrentTick.Index != CassetteIndex && state.NextTick.Index == CassetteIndex) {
                                    State = LaserState.Precharge;
                                }
                                break;

                            case LaserState.Precharge:
                                if (beatFraction >= chargeDelayFraction) {
                                    setAnimationSpeed(chargingAnimation, state.TickLength * (1 - chargeDelayFraction));
                                    State = LaserState.Charging;
                                }
                                break;

                            case LaserState.Charging:
                                if (beat == 0 && state.CurrentTick.Index == CassetteIndex) {
                                    State = LaserState.Burst;
                                }
                                break;

                            case LaserState.Firing:
                                if (State == LaserState.Firing && beat == 0 && (state.CurrentTick.Index != CassetteIndex || HalfLength)) {
                                    State = LaserState.Idle;
                                }
                                break;
                        }
                    }
                },
                new PlayerCollider(onPlayerCollide),
                new LedgeBlocker(_ => KillPlayer),
                beamSprite,
                smallEmitterSprite,
                largeEmitterSprite
            );

            var emitterHitboxList = new List<Collider>();
            var colliderOffset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);
            for (int i = 1; i < Tiles; i += 2) {
                var coll = (Collider) new Circle(6);
                coll.Position = Orientation.Normal() * 2f + colliderOffset * i;
                emitterHitboxList.Add(coll);
            }

            emitterHitboxes = emitterHitboxList.ToArray();
            inactiveColliderList = new ColliderList(emitterHitboxes);

            var components = CreateLaserColliders().ToArray();
            laserHitboxes = components.Select(c => c.Collider).ToArray();
            Add(components.Cast<Component>().ToArray());
            activeColliderList = new ColliderList(emitterHitboxes.Concat(laserHitboxes).ToArray());

            Get<StaticMover>().OnMove = v => {
                Position += v;
                foreach (var collider in components)
                    collider.UpdateBeam();
            };
        }

        private Sprite configureSprite(Sprite sprite) {
            sprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up
                ? new Vector2(-1, 1)
                : Vector2.One;
            sprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down
                ? (float) Math.PI / 2f
                : 0f;
            return sprite;
        }

        protected virtual IEnumerable<LaserColliderComponent> CreateLaserColliders() {
            var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

            if (Tiles == 2) {
                return new[] {
                    new LaserColliderComponent {
                        CollideWithSolids = CollideWithSolids, Thickness = beamThickness, Offset = offset + beamOffset,
                    }
                };
            }

            var start = offset / 2;
            return Enumerable.Range(0, Tiles).Select(i => new LaserColliderComponent {
                CollideWithSolids = CollideWithSolids, Offset = start + offset * i, Thickness = tileSize,
            });
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Add(new Coroutine(impactParticlesSequence()));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            needsForcedUpdate = true;
        }

        private void forceUpdate() {
            if (!cassetteListener.UpdateState())
                return;

            needsForcedUpdate = false;

            var cassetteState = cassetteListener.CurrentState;

            if (cassetteState.CurrentTick.Index == CassetteIndex && (cassetteState.NextTick.Index == CassetteIndex || !HalfLength)) {
                State = LaserState.Firing;
            } else if (cassetteState.CurrentTick.Index != CassetteIndex && cassetteState.NextTick.Index == CassetteIndex) {
                setAnimationSpeed(chargingAnimation, cassetteState.TickLength * (1 - chargeDelayFraction));
                State = LaserState.Charging;
            } else {
                laserState = LaserState.Precharge;
                State = LaserState.Idle;
            }
        }

        private void onPlayerCollide(Player player) {
            if (KillPlayer) {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction);
            }
        }

        public override void Update() {
            base.Update();

            if (needsForcedUpdate)
                forceUpdate();

            if (State == LaserState.Burst && collisionDelayRemaining > 0) {
                collisionDelayRemaining -= Engine.DeltaTime;
                if (collisionDelayRemaining <= 0)
                    State = LaserState.Firing;
            }
        }

        public override void Render() {
            if (State == LaserState.Burst || State == LaserState.Firing) {
                for (int i = 0; i < laserHitboxes.Length; i++)
                    renderBeam(laserHitboxes[i], i);
            } else if (State == LaserState.Charging) {
                foreach (var hitbox in laserHitboxes)
                    renderTelegraph(hitbox);
            }

            var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

            for (int i = 2; i < Tiles; i += 2) {
                smallEmitterSprite.Position = offset * i;
                smallEmitterSprite.Render();
            }

            for (int i = 1; i < Tiles; i += 2) {
                largeEmitterSprite.Position = offset * i;
                largeEmitterSprite.Render();
            }
        }

        private void renderBeam(Hitbox laserHitbox, int index) {
            var frame = beamSprite.GetFrame(beamSprite.CurrentAnimationID, beamSprite.CurrentAnimationFrame);
            float length = Math.Abs(Orientation switch {
                Orientations.Up => beamSprite.Y - laserHitbox.Top,
                Orientations.Down => beamSprite.Y - laserHitbox.Bottom,
                Orientations.Left => beamSprite.X - laserHitbox.Left,
                Orientations.Right => beamSprite.X - laserHitbox.Right,
                _ => 0,
            });

            var startPosition = Position + beamSprite.Position + (Orientation.Vertical()
                ? new Vector2(laserHitbox.CenterX, 0)
                : new Vector2(0, laserHitbox.CenterY));

            var frameOffset = Orientation.Normal() * frame.Width;
            var origin = beamSprite.Origin;

            if (laserHitboxes.Length > 1) {
                if (index == 0 && Orientation.Horizontal() || index == laserHitboxes.Length - 1 && Orientation.Vertical())
                    frame = frame.GetSubtexture(new Rectangle(0, 0, frame.Width, tileSize));
                else if (index == 0 && Orientation.Vertical() || index == laserHitboxes.Length - 1 && Orientation.Horizontal())
                    frame = frame.GetSubtexture(new Rectangle(0, frame.Height - tileSize, frame.Width, tileSize));
                else {
                    // TODO: draw rectangle instead
                    // var rectPos = Position + Orientation.Normal() * tileSize * index;
                    var rectTopLeft = Position + laserHitbox.TopLeft;
                    Draw.Rect(rectTopLeft.X, rectTopLeft.Y, laserHitbox.Width, laserHitbox.Height, beamFillColor);
                    return;
                }
                origin = new Vector2(0, tileSize / 2f);
            }

            return;
            int count = (int) Math.Ceiling(length / frame.Width);
            int remainder = (int) length % frame.Width;

            for (int i = 0; i < count; i++) {
                var position = startPosition + i * frameOffset;
                int width = i == count - 1 && remainder != 0 ? remainder : frame.Width;
                frame.Draw(position, origin, beamSprite.Color, beamSprite.Scale, beamSprite.Rotation , new Rectangle(0, 0, width, frame.Height));
            }
        }

        private void renderTelegraph(Hitbox laserHitbox) {
            // if (emitterSprites.FirstOrDefault() is not { } emitterSprite)
            //     return;
            //
            // float animationProgress = (float)emitterSprite.CurrentAnimationFrame / emitterSprite.CurrentAnimationTotalFrames;
            // int hitboxThickness = (int) Orientation.ThicknessOfHitbox(laserHitbox);
            // int lerped = (int)Calc.LerpClamp(0, hitboxThickness, Ease.QuintOut(animationProgress));
            // int thickness = Math.Min(lerped + 2, hitboxThickness);
            // thickness -= thickness % 2;
            //
            // var rect = Orientation == Orientations.Up || Orientation == Orientations.Down
            //     ? new Rectangle((int) (X + laserHitbox.CenterX) - thickness / 2, (int) (Y + laserHitbox.Top), thickness, (int) laserHitbox.Height)
            //     : new Rectangle((int) (X + laserHitbox.Left), (int) (Y + laserHitbox.CenterY) - thickness / 2, (int) laserHitbox.Width, thickness);
            //
            // Draw.Rect(rect, telegraphColor * 0.3f);
        }

        private void emitCooldownParticles() {
            int amount = laserHitboxes.Length == 1 ? 3 : 1;

            foreach (var laserHitbox in laserHitboxes) {
                var level = SceneAs<Level>();
                int length = (int) Orientation.LengthOfHitbox(laserHitbox) - beamOffsetMultiplier;
                var offset = Orientation.Normal();
                float angle = Orientation.Angle() - (float) Math.PI / 2f;
                var startPos =  Position + Orientation.OriginOfHitbox(laserHitbox) + beamOffset * 2;
                var particle = CassetteIndex == 0 ? blueCooldownParticle : pinkCooldownParticle;

                for (int i = 0; i < length; i += Calc.Random.Next(8, 16)) {
                    level.ParticlesBG.Emit(particle, amount, startPos + offset * i, Vector2.Zero, angle);
                }
            }
        }

        private void emitImpactParticles(Hitbox laserHitbox) {
            var level = SceneAs<Level>();
            var particle = CassetteIndex == 0 ? blueImpactParticle : pinkImpactParticle;
            var offset = Orientation == Orientations.Up || Orientation == Orientations.Down ? Vector2.UnitX : Vector2.UnitY;
            float angle = Orientation.Angle() + (float)Math.PI / 2f;

            int thickness = (int) Orientation.ThicknessOfHitbox(laserHitbox);
            var startPos = new Vector2(Orientation == Orientations.Right ? laserHitbox.Right + X : laserHitbox.Left + X,
                Orientation == Orientations.Down ? laserHitbox.Bottom + Y: laserHitbox.Top + Y);

            const int particleCount = 3;
            level.ParticlesFG.Emit(particle, particleCount, startPos, Vector2.Zero, angle);
            level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness / 2, Vector2.Zero, angle);
            level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness, Vector2.Zero, angle);
        }

        private IEnumerator impactParticlesSequence() {
            var laserColliders = Components.GetAll<LaserColliderComponent>().ToArray();

            while (Scene != null) {
                if (State != LaserState.Firing) {
                    yield return null;
                    continue;
                }

                object yieldValue = null;
                foreach (var laser in laserColliders) {
                    if (!laser.CollidedWithScreenBounds) {
                        yieldValue = 0.1f;
                        emitImpactParticles(laser.Collider);
                    }
                }

                yield return yieldValue;
            }
        }

        public enum LaserState {
            /// <summary>
            /// The laser is currently off.
            /// Collision = off.
            /// </summary>
            Idle,

            /// <summary>
            /// The laser is preparing to play the charge animation.
            /// It will wait a fraction of a tick before moving to the <see cref="Charging"/> state.
            /// <see cref="BrushLaserEmitter.chargeDelayFraction"/>
            /// Collision = off.
            /// </summary>
            Precharge,

            /// <summary>
            /// The laser is playing the charge animation.
            /// Starts shortly after the tick prior to the laser firing, and lasts for the rest of the tick.
            /// The telegraph beam should be displayed.
            /// Collision = off.
            /// </summary>
            Charging,

            /// <summary>
            /// The laser is playing the burst animation.
            /// Collision = off.
            /// </summary>
            Burst,

            /// <summary>
            /// The laser is firing.
            /// Starts shortly after the cassette swap.
            /// <see cref="BrushLaserEmitter.collisionDelaySeconds"/>
            /// Collision = on.
            /// </summary>
            Firing,
        }
    }
}