using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021.Entities
{   
    [Tracked]
    public class BubbleCollider : Component
    {
        private Collider collider;

        public BubbleCollider(Collider collider = null) : base(true, false)
        {
            this.collider = collider;
        }

        public bool Check(FloatingBubble bubble)
        {
            Collider collider = Entity.Collider;
            if (this.collider != null) {
                Entity.Collider = this.collider;
            }
            bool result = bubble.CollideCheck(Entity);
            Entity.Collider = collider;
            return result;
        }

        public static void Load()
        {
            On.Celeste.TouchSwitch.ctor_Vector2 += OnTouchSwitchCtor;
            On.Celeste.Spring.ctor_Vector2_Orientations_bool += OnSpringCtor;
        }

        public static void Unload()
        {
            On.Celeste.TouchSwitch.ctor_Vector2 -= OnTouchSwitchCtor;
            On.Celeste.Spring.ctor_Vector2_Orientations_bool -= OnSpringCtor;
        }

        private static void OnTouchSwitchCtor(On.Celeste.TouchSwitch.orig_ctor_Vector2 orig, TouchSwitch self, Vector2 position)
        {
            orig(self, position);
            self.Add(new BubbleCollider());
        }

        private static void OnSpringCtor(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 position, Spring.Orientations orientation, bool playerCanUse){
            orig(self, position, orientation, playerCanUse);
            self.Add(new BubbleCollider());
        }

    }
}