namespace Celeste.Mod.StrawberryJam2021.Entities {
	// Celeste.Mod.CanyonHelper.ToggleBlockLaser
	//using Celeste;
	//using Celeste.Mod.CanyonHelper;
	using Microsoft.Xna.Framework;
	using Microsoft.Xna.Framework.Graphics;
	using Monocle;
	using System;

	[Pooled]
	[Tracked(false)]
	internal class ToggleBlockLaser : Entity {
		private VertexPositionColor[] fade;

		private MTexture laserSprite;

		private float angle;

		public Color color;

        public Vector2 start;

		public Vector2 end;

		private float timer;

		private float sineTimer = 0f;

		public ToggleBlockLaser() : base() {
			timer = 0f;
			fade = (VertexPositionColor[]) (object) new VertexPositionColor[24];
			laserSprite = GFX.Game["objects/canyon/toggleblock/laser"];
			base.Depth = 8999;
			timer = Calc.NextFloat(Calc.Random);
		}

		public ToggleBlockLaser Init(Vector2 currentNode, Vector2 nextNode) {
			start = currentNode;
			end = nextNode;
			angle = Calc.Angle(currentNode, nextNode);
			return this;
		}

		public override void Update() {
			base.Update();
			timer += Engine.DeltaTime * 4f;
			sineTimer += Engine.DeltaTime * 12f;
		}

		public override void Render() {
			float num = 0.5f * (0.5f + ((float) Math.Sin(timer) + 1f) * 0.25f);
			Draw.SineTextureH(laserSprite, start, Vector2.Zero, new Vector2(Vector2.Distance(start, end) / 128f, 1.5f), angle, color * num, (SpriteEffects) 0, sineTimer, 1f, 1, 0.04f);
		}
	}
}