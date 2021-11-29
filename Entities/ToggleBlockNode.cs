using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
	[Pooled]
	[Tracked(false)]
	internal class ToggleBlockNode : Entity {
		private MTexture nodeTexture;

		private MTexture nodeCrystalTexture;

		public Color color = Color.Red;

		private SineWave sine;

		private float positionOffset = 0f;

		public ToggleBlockNode() : base() {
			nodeTexture = GFX.Game["objects/canyon/toggleblock/node"];
			nodeCrystalTexture = GFX.Game["objects/canyon/toggleblock/nodeCrystal"];
			base.Depth = 8999;
			base.Add(sine = new SineWave(0.6f, 0f));
		}

		public override void Update() {
			base.Update();
			positionOffset = sine.Value - 2f;
		}

		public ToggleBlockNode Init(Vector2 position) {
			base.Position = position;
			return this;
		}

		public override void Render() {
			nodeCrystalTexture.DrawCentered(base.Position + Vector2.UnitY * positionOffset, Color.White);
			nodeTexture.DrawCentered(base.Position + Vector2.UnitY * positionOffset, color);
		}
	}
}