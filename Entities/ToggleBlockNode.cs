using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {
	[Pooled]
	[Tracked(false)]
    public class ToggleBlockNode : Entity {
		private MTexture nodeTexture;

		private MTexture nodeCrystalTexture;

		public Color color = Color.Red;

		private SineWave sine;

		private float positionOffset;

		public ToggleBlockNode() : base() {
			nodeTexture = GFX.Game["objects/StrawberryJam2021/toggleSwapBlock/node"];
			nodeCrystalTexture = GFX.Game["objects/StrawberryJam2021/toggleSwapBlock/nodeCrystal"];
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
			nodeCrystalTexture.DrawCentered(Position + Vector2.UnitY * positionOffset, Color.White);
			nodeTexture.DrawCentered(Position + Vector2.UnitY * positionOffset, color);
		}
	}
}