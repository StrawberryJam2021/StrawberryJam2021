using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/JellyGlowController")]
    public class JellyGlowController : Entity {
        private readonly Color _lightColor;
        private readonly float _lightAlpha;
        private readonly int _lightStartFade;
        private readonly int _lightEndFade;
        private readonly Vector2 _lightOffset;

        public JellyGlowController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            _lightColor = data.HexColor("lightColor", Color.White);
            _lightAlpha = data.Float("lightAlpha", 1f);
            _lightStartFade = data.Int("lightStartFade", 24);
            _lightEndFade = data.Int("lightEndFade", 48);
            _lightOffset = new Vector2(data.Int("lightOffsetX"), data.Int("lightOffsetY", -10));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            var allGliders = scene.Entities.Concat(scene.Entities.GetToAdd()).OfType<Glider>();
            foreach (var glider in allGliders) {
                glider.Add(new VertexLight(_lightOffset, _lightColor, _lightAlpha, _lightStartFade, _lightEndFade));
            }
        }
    }
}