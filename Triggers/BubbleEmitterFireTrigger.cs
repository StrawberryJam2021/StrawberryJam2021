using Microsoft.Xna.Framework;
using Celeste.Mod.StrawberryJam2021.Entities;

namespace Celeste.Mod.StrawberryJam2021.Triggers
{
    public class BubbleEmitterFireTrigger : Trigger
    {
        private string flag;
        private bool onlyOnce;

        public BubbleEmitterFireTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            flag = data.Attr("flag");
            onlyOnce = data.Bool("onlyOnce");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            foreach(FloatingBubbleEmitter emitter in Scene.Tracker.GetEntities<FloatingBubbleEmitter>())
            {
                if(flag == "")
                {
                    emitter.Fire();
                }
                else if(flag == emitter.flag)
                {
                    emitter.Fire();
                }
            }
            if(onlyOnce)
            {
                RemoveSelf();
            }
        }

    }
}