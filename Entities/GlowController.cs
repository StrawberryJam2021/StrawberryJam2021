using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
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

                if (_lightWhitelist.Contains(typeName)) {
                    entity.Add(new VertexLight(_lightOffset, _lightColor, _lightAlpha, _lightStartFade, _lightEndFade));
                } else if (_lightBlacklist.Contains(typeName)) {
                    entity.Remove(entity.Components.GetAll<VertexLight>().ToArray<Component>());
                }

                if (_bloomWhitelist.Contains(typeName)) {
                    entity.Add(new BloomPoint(_bloomOffset, _bloomAlpha, _bloomRadius));
                } else if (_bloomBlacklist.Contains(typeName)) {
                    entity.Remove(entity.Components.GetAll<BloomPoint>().ToArray<Component>());
                    entity.Remove(entity.Components.GetAll<CustomBloom>().ToArray<Component>());
                }
            }
        }
    }
}