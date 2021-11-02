using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/PelletEmitterUp = LoadUp",
        "SJ2021/PelletEmitterDown = LoadDown",
        "SJ2021/PelletEmitterLeft = LoadLeft",
        "SJ2021/PelletEmitterRight = LoadRight")]
    public class PelletEmitter : OrientableEntity {
        #region Static Loader Methods

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Up);

        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Down);

        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Left);

        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData data) =>
            new PelletEmitter(data, offset, Orientations.Right);

        #endregion

        #region Ahorn Properties

        public bool CollideWithSolids { get; }
        public int Count { get; }
        public float Delay { get; }
        public float Speed { get; }
        public int CassetteIndex { get; }

        #endregion

        public Vector2 Direction { get; }
        public Vector2 Origin { get; }

        public readonly Sprite EmitterSprite;

        public string AnimationKeyPrefix => $"{CassetteIndex switch {0 => "blue", 1 => "pink", _ => "both"}}";

        private string idleAnimationKey => $"{AnimationKeyPrefix}_idle";
        private string chargingAnimationKey => $"{AnimationKeyPrefix}_charging";
        private string firingAnimationKey => $"{AnimationKeyPrefix}_firing";

        protected PelletEmitter(EntityData data, Vector2 offset, Orientations orientation)
            : base(data, offset, orientation) {
            const float shotOriginOffset = 12f;
            CassetteIndex = data.Int("cassetteIndex");
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Count = data.Int("pelletCount", 1);
            Delay = data.Float("pelletDelay", 0.25f);
            Speed = data.Float("pelletSpeed", 100f);

            Direction = Orientation.Direction();
            Origin = Orientation.Direction() * shotOriginOffset;
            Collider = new Circle(6, Direction.X * 2, Direction.Y * 2);

            EmitterSprite = StrawberryJam2021Module.SpriteBank.Create("pelletEmitter");
            EmitterSprite.Rotation = Orientation.Angle() - (float)Math.PI / 2f;
            EmitterSprite.Effects = Orientation is Orientations.Left or Orientations.Down
                    ? SpriteEffects.FlipVertically
                    : SpriteEffects.None;

            EmitterSprite.Play(idleAnimationKey);

            Add(EmitterSprite,
                new LedgeBlocker(),
                new PlayerCollider(OnPlayerCollide),
                new CassetteListener {
                    OnTick = state => {
                        if (state.NextTick.Index != state.CurrentTick.Index &&
                            (CassetteIndex < 0 || CassetteIndex == state.NextTick.Index)) {
                            string key = chargingAnimationKey;
                            var animation = EmitterSprite.Animations[key];
                            animation.Delay = state.TickLength / animation.Frames.Length;
                            EmitterSprite.Play(key);
                        }
                    },
                    OnSwap = state => {
                        if (CassetteIndex < 0 || CassetteIndex == state.NextTick.Index) {
                            EmitterSprite.Play(firingAnimationKey);
                            Fire();
                        }
                    },
                });
        }

        public void Fire(Action<PelletShot> action = null) {
            for (int i = 0; i < Count; i++) {
                var shot = Engine.Pooler.Create<PelletShot>().Init(this, i * Delay);
                action?.Invoke(shot);
                Scene.Add(shot);
            }
        }

        private void OnPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());

        [Pooled]
        [Tracked]
        public class PelletShot : Entity {
            public bool Dead { get; set; }
            public Vector2 Speed { get; set; }
            public bool CollideWithSolids { get; set; }

            private readonly Sprite projectileSprite;
            private readonly Sprite impactSprite;

            private float delayTimeRemaining;

            private string projectileAnimationKey;
            private string impactAnimationKey;

            private readonly Hitbox killHitbox = new Hitbox(8, 8, -4, -4);

            public PelletShot() : base(Vector2.Zero) {
                Depth = Depths.Above;
                Add(projectileSprite = StrawberryJam2021Module.SpriteBank.Create("pelletProjectile"),
                    impactSprite = StrawberryJam2021Module.SpriteBank.Create("pelletImpact"),
                    new PlayerCollider(OnPlayerCollide));
            }

            public PelletShot Init(PelletEmitter emitter, float delay) {
                delayTimeRemaining = delay;
                Dead = false;
                Speed = emitter.Direction * emitter.Speed;
                Position = emitter.Position + emitter.Origin;
                CollideWithSolids = emitter.CollideWithSolids;
                Collider = killHitbox;
                Collidable = true;

                impactSprite.Rotation = projectileSprite.Rotation = emitter.EmitterSprite.Rotation;
                impactSprite.Effects = projectileSprite.Effects = emitter.EmitterSprite.Effects;

                projectileAnimationKey = impactAnimationKey = emitter.AnimationKeyPrefix;

                impactSprite.Visible = false;
                impactSprite.Stop();

                projectileSprite.Visible = delay == 0;
                projectileSprite.Stop();

                return this;
            }

            public override void Added(Scene scene) {
                base.Added(scene);
                if (delayTimeRemaining <= 0) {
                    projectileSprite.Play(projectileAnimationKey);
                }
            }

            public void Destroy(float delay = 0) {
                projectileSprite.Stop();
                impactSprite.Stop();
                Dead = true;
                RemoveSelf();
            }

            public override void Update() {
                base.Update();

                // fast fail if the pooled shot is no longer alive
                if (Dead) return;

                // only show the impact sprite if it's animating
                impactSprite.Visible = impactSprite.Animating;

                // if we're not collidable and no longer animating the impact, destroy
                if (!Collidable) {
                    if (!impactSprite.Animating) {
                        Destroy();
                    }
                    return;
                }

                // delayed init
                if (delayTimeRemaining > 0) {
                    delayTimeRemaining -= Engine.DeltaTime;
                    if (delayTimeRemaining > 0) {
                        return;
                    }

                    projectileSprite.Visible = true;
                    projectileSprite.Play(projectileAnimationKey);
                }

                Move();

                // destroy the shot if it leaves the room bounds
                var level = SceneAs<Level>();
                if (!level.IsInBounds(this)) {
                    Destroy();
                }
            }

            private void Move() {
                var delta = Speed * Engine.DeltaTime;
                var target = Position + delta;

                // check whether the target position would trigger a new solid collision
                if (CollideWithSolids && CollideCheckOutside<Solid>(target)) {
                    var normal = delta.SafeNormalize();

                    // snap the current position away from the solid
                    if (normal.X != 0) {
                        Position.X = normal.X < 0 ? (float) Math.Ceiling(Position.X) : (float) Math.Floor(Position.X);
                    }
                    if (normal.Y != 0) {
                        Position.Y = normal.Y < 0 ? (float) Math.Ceiling(Position.Y) : (float) Math.Floor(Position.Y);
                    }

                    // move one pixel at a time to find the exact collision point (with safety counter)
                    int safety = 50;
                    while (safety-- > 0) {
                        // if it collided...
                        var solid = CollideFirst<Solid>(Position + normal);
                        if (solid != null) {
                            // snap the shot to the collision point
                            if (normal.X < 0) {
                                Position.X = (float) Math.Floor(Collider.AbsoluteLeft);
                            } else if (normal.X > 0) {
                                Position.X = (float) Math.Ceiling(Collider.AbsoluteRight);
                            }
                            if (normal.Y < 0) {
                                Position.Y = (float) Math.Floor(Collider.AbsoluteTop);
                            } else if (normal.Y > 0) {
                                Position.Y = (float) Math.Ceiling(Collider.AbsoluteBottom);
                            }

                            // stop... hammer time!
                            Speed = Vector2.Zero;

                            // trigger the impact animation
                            Impact(solid is not SolidTiles);
                            return;
                        }

                        Position += normal;
                    }
                }

                Position = target;
            }

            private void Impact(bool air) {
                projectileSprite.Stop();
                projectileSprite.Visible = false;
                impactSprite.Play(air ? $"{impactAnimationKey}_air" : impactAnimationKey);
                impactSprite.Visible = true;
                Collidable = false;
            }

            private void OnPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}