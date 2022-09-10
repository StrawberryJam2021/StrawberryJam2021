using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/GlowController")]
    public class GlowController : Entity {
        private readonly string[] _lightWhitelist;
        private readonly string[] _lightBlacklist;
        private readonly Color _lightColor;
        private readonly float _lightAlpha;
        private readonly int _lightStartFade;
        private readonly int _lightEndFade;
        private readonly Vector2 _lightOffset;
        private readonly string _targetEntityType;

        private readonly string[] _bloomWhitelist;
        private readonly string[] _bloomBlacklist;
        private readonly float _bloomAlpha;
        private readonly float _bloomRadius;
        private readonly Vector2 _bloomOffset;

        private const string deathAnimationId = "death";
        private const string respawnAnimationId = "respawn";
        
        public GlowController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            _lightWhitelist = data.Attr("lightWhitelist").Split(',');
            _lightBlacklist = data.Attr("lightBlacklist").Split(',');
            _lightColor = data.HexColor("lightColor", Color.White);
            _lightAlpha = data.Float("lightAlpha", 1f);
            _lightStartFade = data.Int("lightStartFade", 24);
            _lightEndFade = data.Int("lightEndFade", 48);
            _lightOffset = new Vector2(data.Int("lightOffsetX"), data.Int("lightOffsetY", -10));
            _targetEntityType = data.Attr("targetEntityType");

            _bloomWhitelist = data.Attr("bloomWhitelist").Split(',');
            _bloomBlacklist = data.Attr("bloomBlacklist").Split(',');
            _bloomAlpha = data.Float("bloomAlpha", 1f);
            _bloomRadius = data.Float("bloomRadius", 8f);
            _bloomOffset = new Vector2(data.Int("bloomOffsetX"), data.Int("bloomOffsetY", -10));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            var allEntities = scene.Entities.Concat(scene.Entities.GetToAdd());
            foreach (var entity in allEntities) {
                var type = entity.GetType();
                var typeName = type.FullName;
                var requiresRemovalRoutine = false;

                if (_lightWhitelist.Contains(typeName)) {
                    entity.Add(new VertexLight(_lightOffset, _lightColor, _lightAlpha, _lightStartFade, _lightEndFade));
                    requiresRemovalRoutine = true;
                } else if (_lightBlacklist.Contains(typeName)) {
                    entity.Remove(entity.Components.GetAll<VertexLight>().ToArray<Component>());
                }

                if (_bloomWhitelist.Contains(typeName)) {
                    entity.Add(new BloomPoint(_bloomOffset, _bloomAlpha, _bloomRadius));
                    requiresRemovalRoutine = true;
                } else if (_bloomBlacklist.Contains(typeName)) {
                    entity.Remove(entity.Components.GetAll<BloomPoint>().ToArray<Component>());
                    entity.Remove(entity.Components.GetAll<CustomBloom>().ToArray<Component>());
                }

                // some entities get a special coroutine that hides lights and blooms
                // if it's a glider or otherwise has a sprite with a "death" animation
                if (requiresRemovalRoutine &&
                    entity.Components.GetAll<Sprite>().FirstOrDefault(s => s.Has(deathAnimationId)) is { } sprite) {
                    entity.Add(new Coroutine(DeathRemovalRoutine(entity, sprite)));
                }
            }
        }

        private IEnumerator DeathRemovalRoutine(Entity entity, Sprite sprite) {
            void SetAlpha(float alpha) {
                foreach (VertexLight vertexLight in entity.Components.GetAll<VertexLight>()) {
                    vertexLight.Alpha = alpha;
                }
                foreach (BloomPoint bloomPoint in entity.Components.GetAll<BloomPoint>()) {
                    bloomPoint.Alpha = alpha;
                }
            }
            
            if (!sprite.Animations.TryGetValue(deathAnimationId, out var deathAnimation)) {
                yield break;
            }

            while (entity.Scene != null) {
                // wait until the sprite plays the death animation
                while (entity.Scene != null && sprite.CurrentAnimationID != deathAnimationId) {
                    yield return null;
                }
                
                // fade out over the length of that animation
                var fadeTime = deathAnimation.Frames.Length * deathAnimation.Delay;
                var fadeRemaining = fadeTime;
        
                while (entity.Scene != null && sprite.CurrentAnimationID == deathAnimationId && fadeRemaining > 0) {
                    fadeRemaining -= Engine.DeltaTime;
                    SetAlpha(Math.Max(fadeRemaining / fadeTime, 0f));
                    yield return null;
                }
                
                // if it's a respawning jelly, wait until the sprite is playing the respawn animation
                if (!sprite.Animations.TryGetValue(respawnAnimationId, out var respawnAnimation)) break;
                while (entity.Scene != null && sprite.CurrentAnimationID != respawnAnimationId) {
                    yield return null;
                }
                
                // fade in over the length of that animation
                fadeTime = respawnAnimation.Frames.Length * respawnAnimation.Delay;
                fadeRemaining = fadeTime;

                while (entity.Scene != null && sprite.CurrentAnimationID == respawnAnimationId && fadeRemaining > 0) {
                    fadeRemaining -= Engine.DeltaTime;
                    SetAlpha(1f - Math.Max(fadeRemaining / fadeTime, 0f));
                    yield return null;
                }
            }
        }
    }
}