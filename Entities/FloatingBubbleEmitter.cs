using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/FloatingBubbleEmitter")]
    public class FloatingBubbleEmitter : Entity {
        private Sprite sprite;
        private bool firing;
        public string flag;

        public FloatingBubbleEmitter(EntityData data, Vector2 offset) : base(data.Position + offset) {
            flag = data.Attr("flag");
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("bubbleEmitter"));
            sprite.CenterOrigin();
        }

        public void Fire() {
            if (!firing) {
                Add(new Coroutine(SpawnRoutine()));
            }
        }

        private IEnumerator SpawnRoutine() {
            firing = true;
            sprite.Play("open");
            while (sprite.CurrentAnimationFrame != 1) {
                yield return null;
            }
            Scene.Add(new FloatingBubble(new Vector2(Position.X, Position.Y - 18)));
            Audio.Play(CustomSoundEffects.game_bubble_emitter_emitter_generate, Position);
            yield return null;
            firing = false;
        }
    }
}