using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    public class FloatingBubble : Actor {
        private Vector2 Speed;
        private float NoFloatTimer;
        private float springCooldownTimer;
        private Sprite sprite;

        private bool broken = false;
        private static MethodInfo SpringBounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);


        public FloatingBubble(Vector2 position) : base(position) {
            Speed = Vector2.Zero;
            Collider = new Hitbox(14, 14, -7, -7);
            Add(new PlayerCollider(OnPlayer));
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("bubble"));
            sprite.OnFinish = OnAnimationFinished;
            sprite.CenterOrigin();
        }

        public override bool IsRiding(JumpThru jumpThru) {
            return false;
        }

        public override bool IsRiding(Solid solid) {
            return false;
        }

        public override void Update() {
            base.Update();
            Vector2 ActualSpeed = Speed;
            if (springCooldownTimer > 0) {
                springCooldownTimer -= Engine.DeltaTime;
            }
            if (NoFloatTimer > 0) {
                NoFloatTimer -= Engine.DeltaTime;
            } else {
                ActualSpeed += new Vector2(0, -60f);
            }
            Position += ActualSpeed * Engine.DeltaTime;
            Speed.X = Calc.Approach(Speed.X, 0, 40f * Engine.DeltaTime);
            Speed.Y = Calc.Approach(Speed.Y, -60f, 20f * Engine.DeltaTime);
            if (CollideCheck<Solid>()) {
                Burst();
            }
            Rectangle levelBounds = SceneAs<Level>().Bounds;
            if ((Position.X > levelBounds.Right + 10 || Position.X < levelBounds.Left - 10) || (Position.Y > levelBounds.Bottom + 10 || Position.Y < levelBounds.Top - 10)) {
                Burst();
            }
            foreach (BubbleCollider collider in Scene.Tracker.GetComponents<BubbleCollider>()) {
                if (collider.Check(this)) {
                    if (collider.Entity is Spring) {
                        if (springCooldownTimer <= 0) {
                            HitSpring(collider.Entity as Spring);
                            SpringBounceAnimate.Invoke(collider.Entity as Spring, null);
                        }
                    } else if (collider.Entity is TouchSwitch) {
                        (collider.Entity as TouchSwitch).TurnOn();
                    }
                }
            }
            if (sprite.CurrentAnimationID == "pop") {
                if (sprite.CurrentAnimationFrame == 1) {
                    if (broken == false) {
                        Collidable = false;
                        Vector2 position = Position + new Vector2(0f, 1f) + Calc.AngleToVector(Calc.Random.NextAngle(), 5f);
                        SceneAs<Level>().ParticlesFG.Emit(Player.P_CassetteFly, 10, position, new Vector2(8, 8), Color.White, 0);
                        SceneAs<Level>().Displacement.AddBurst(Position, 0.6f, 4f, 28f, 0.2f);
                        Audio.Play(CustomSoundEffects.game_bubble_emitter_bubble_pop, Position);
                        broken = true;
                    }
                }
            }
        }

        public bool HitSpring(Spring spring) {
            springCooldownTimer = 0.05f;
            switch (spring.Orientation) {
                case Spring.Orientations.WallLeft:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case Spring.Orientations.WallRight:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case Spring.Orientations.Floor:
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    NoFloatTimer = 0.15f;
                    springCooldownTimer += 0.2f;
                    break;
            }
            return true;
        }

        public void Burst() {
            sprite.Play("pop");
        }

        public void OnPlayer(Player player) {
            player.SuperBounce(Top);
            Burst();
        }

        public void OnAnimationFinished(string id) {
            Remove(sprite);
            RemoveSelf();
        }
    }
}