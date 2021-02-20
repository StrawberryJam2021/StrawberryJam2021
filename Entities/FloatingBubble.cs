using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities
{
    public class FloatingBubble : Actor
    {
        private Vector2 Speed;
        private float NoFloatTimer;
        public static MethodInfo SpringBounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);

        public FloatingBubble(Vector2 position) : base(position) {
            Speed = Vector2.Zero;
            Collider = new Circle(8);
            Add(new PlayerCollider(OnPlayer));
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
            if(NoFloatTimer > 0)
            {
                NoFloatTimer -= Engine.DeltaTime;
            }
            else
            {
                ActualSpeed += new Vector2(0, -60f);
            }
            Position += ActualSpeed * Engine.DeltaTime;
            Speed.X = Calc.Approach(Speed.X, 0, -60f);
            Speed.Y = Calc.Approach(Speed.Y, -60f, -20f);
            if(CollideCheck<Solid>())
            {
                Burst();
            }
            foreach(BubbleCollider collider in Scene.Tracker.GetComponents<BubbleCollider>())
            {
                if(collider.Check(this))
                {
                    if(collider.Entity is Spring)
                    {
                        HitSpring(collider.Entity as Spring);
                        SpringBounceAnimate.Invoke(collider.Entity as Spring, null);
                    }
                    else if(collider.Entity is TouchSwitch)
                    {
                        (collider.Entity as TouchSwitch).TurnOn();
                    }
                }
            }
        }

        public override void Render() {
            Draw.Circle(Position, 8, Color.White, 18);
        }

        public bool HitSpring(Spring spring) {
            switch(spring.Orientation) {
                case Spring.Orientations.WallLeft:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case Spring.Orientations.WallRight:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case Spring.Orientations.Floor:
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    NoFloatTimer = 0.1f;
                    break;
            }
            return true;
        }

        public void Burst()
        {
            RemoveSelf();
        }

        public void OnPlayer(Player player)
        {
            player.SuperBounce(Top);
            Burst();
        }
        
    }
}