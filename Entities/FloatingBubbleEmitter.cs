using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Entities
{
    [CustomEntity("SJ2021/FloatingBubbleEmitter")]
    public class FloatingBubbleEmitter : Entity
    {
        float spawnTimer;
        float spawnTimerMax;
        public FloatingBubbleEmitter(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            spawnTimer = spawnTimerMax = data.Float("spawnTimer",2f);
        }

        public override void Update() {
            base.Update();
            if(spawnTimer > 0)
            {
                spawnTimer -= Engine.DeltaTime;
            }
            else
            {
                spawnTimer = spawnTimerMax;
                Scene.Add(new FloatingBubble(Position));
            }
        }

        public override void Render() {
            base.Render();
            Draw.Rect(Position.X - 8, Position.Y - 8, 16, 16, Color.CornflowerBlue);
        }
    }
}