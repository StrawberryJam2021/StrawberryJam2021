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

        public string AnimationKeyPrefix => $"{(CassetteIndex == 0 ? "blue" : "pink")}";

        private string idleAnimationKey => $"{AnimationKeyPrefix}_idle";
        private string chargingAnimationKey => $"{AnimationKeyPrefix}_charging";
        private string firingAnimationKey => $"{AnimationKeyPrefix}_firing";

        private float chargeTimeRemaining;

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
            EmitterSprite.Effects = Orientation is Orientations.Right or Orientations.Down
                    ? SpriteEffects.FlipVertically
                    : SpriteEffects.None;

            EmitterSprite.Play(idleAnimationKey);

            Add(EmitterSprite,
                new LedgeBlocker(),
                new PlayerCollider(OnPlayerCollide),
                new CassetteListener {
                    OnSixteenth = state => {
                        if (state.Sixteenth % 8 == 7 && state.NextTick.Index != state.CurrentTick.Index && state.NextTick.Index == CassetteIndex) {
                            chargeTimeRemaining = state.SwapLength / 8f;
                            var animation = EmitterSprite.Animations[chargingAnimationKey];
                            animation.Delay = chargeTimeRemaining / 2f;
                        }
                    },
                    OnSwap = state => {
                        if (state.CurrentTick.Index == CassetteIndex) {
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

        public override void Update() {
            base.Update();
            if (chargeTimeRemaining > 0) {
                chargeTimeRemaining -= Engine.DeltaTime;
                if (chargeTimeRemaining <= 0) {
                    EmitterSprite.Play(chargingAnimationKey);
                }
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

            private Orientations orientation;
            private string projectileAnimationKey;
            private string impactAnimationKey;

            private readonly Hitbox impactHitbox = new Hitbox(12, 12, -6, -6);
            private readonly Hitbox killHitbox = new Hitbox(8, 8, -4, -4);

            public PelletShot()
                : base(Vector2.Zero) {
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Depth = Depths.Top;

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
                orientation = emitter.Orientation;

                impactSprite.Rotation = projectileSprite.Rotation = emitter.EmitterSprite.Rotation;
                impactSprite.Effects = projectileSprite.Effects =
                    orientation is Orientations.Right or Orientations.Down
                        ? SpriteEffects.FlipVertically
                        : SpriteEffects.None;

                projectileAnimationKey = impactAnimationKey = emitter.AnimationKeyPrefix;

                impactSprite.Visible = false;
                projectileSprite.Visible = delay == 0;

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

                // our impact hitbox is bigger than our kill hitbox
                Collider = impactHitbox;
                Move();
                Collider = killHitbox;

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
                    float amount = delta.Length();

                    // move one pixel at a time to find the exact collision point
                    while (amount > 0) {
                        amount--;
                        Position += normal;

                        // if it collided...
                        if (CollideCheck<Solid>()) {
                            // snap the shot to the collision point
                            if (normal.X < 0) {
                                Position.X = (float) Math.Ceiling(Collider.AbsoluteLeft);
                            } else if (normal.X > 0) {
                                Position.X = (float) Math.Floor(Collider.AbsoluteRight - 1f);
                            }
                            if (normal.Y < 0) {
                                Position.Y = (float) Math.Ceiling(Collider.AbsoluteTop);
                            } else if (normal.Y > 0) {
                                Position.Y = (float) Math.Floor(Collider.AbsoluteBottom - 1f);
                            }

                            // stop... hammer time!
                            Speed = Vector2.Zero;

                            // trigger the impact animation
                            Impact();
                            return;
                        }
                    }
                }

                // naive movement since we're done
                Position = target;
            }

            private void Impact() {
                projectileSprite.Stop();
                projectileSprite.Visible = false;
                impactSprite.Play(impactAnimationKey);
                impactSprite.Visible = true;
                Collidable = false;
            }

            private void OnPlayerCollide(Player player) => player.Die((player.Center - Position).SafeNormalize());
        }
    }
}