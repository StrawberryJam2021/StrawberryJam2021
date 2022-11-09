using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
	[Pooled]
	[Tracked(false)]
	internal class ToggleBlockNode : Entity {
		private MTexture nodeTexture;

		public Color color = Color.Red;

		private SineWave sine;

		private float positionOffset = 0f;

		public ToggleBlockNode() : base() {
			nodeTexture = GFX.Game["objects/StrawberryJam2021/toggleSwapBlock/node"];
			Depth = Depths.BGDecals - 1;
			Add(sine = new SineWave(0.6f, 0f));
		}

		public override void Update() {
			base.Update();
			positionOffset = sine.Value - 2f;
		}

		public ToggleBlockNode Init(Vector2 position) {
			Position = position;
			return this;
		}

		public override void Render() {
			nodeTexture.DrawCentered(Position + Vector2.UnitY * positionOffset, color);
		}
	}
}