using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/FloatingBubbleEmitter")]
    public class FloatingBubbleEmitter : Entity {
        private float spawnTimer;
        private float spawnTimerMax;
        private Sprite sprite;

        public FloatingBubbleEmitter(EntityData data, Vector2 offset) : base(data.Position + offset) {
            spawnTimer = spawnTimerMax = data.Float("spawnTimer", 2f);
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("bubbleEmitter"));
            sprite.CenterOrigin();
        }

        public override void Update() {
            base.Update();
            if (spawnTimer > 0) {
                spawnTimer -= Engine.DeltaTime;
            } else {
                spawnTimer = spawnTimerMax;
                Add(new Coroutine(SpawnRoutine()));
            }
        }

        private IEnumerator SpawnRoutine() {
            sprite.Play("open");
            while (sprite.CurrentAnimationFrame != 1) {
                yield return null;
            }
            Scene.Add(new FloatingBubble(new Vector2(Position.X, Position.Y - 18)));
            Audio.Play(CustomSoundEffects.game_bubble_emitter_emitter_generate, Position);
            yield return null;
        }

        public override void DebugRender(Camera camera) {
            base.DebugRender(camera);
            Draw.Rect(Position.X, Position.Y, 32, 32, Color.White);
            Draw.Rect(sprite.Position.X, sprite.Position.Y, 8, 8, Color.CornflowerBlue);
        }
    }
}