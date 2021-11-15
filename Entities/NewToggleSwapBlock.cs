using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Entities {
	[Tracked]
	[CustomEntity("SJ2021/ToggleSwapBlock")]
	public class NewToggleSwapBlock : Solid {
		private const int STAY = 8, DONE = 9;
		private static string[] paths = new string[] { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };
		private static string defaultIndicatorPath = "objects/StrawberryJam2021/toggleIndicator/plain/";
		private DashListener dashListener;
		private EventInstance moveSfx;
		private Color offColor = Color.DarkGray;
		private Color onColor = Color.MediumPurple;
		private Color endColor = new Color(0.65f, 0.4f, 0.4f);
		private ToggleBlockLaser[] lasers;
		private int laserCount;
		//private Sprite middleRed;
		private float lerp;
		private MTexture[,] nineSliceBlock;
		private Vector2[] nodes;
		private ToggleBlockNode[] nodeTextures;
		private int nodeIndex = 0;
		public bool moving = false;
		private float travelSpeed = 5f;
		private bool oscillate = false;
		private bool returning = false;
		private bool stopAtEnd = false;
		public Vector2 Direction = Vector2.Zero;
		private bool stopped = false;
		private string customTexturePath;
		private Level level => (Level) (object) (Level) base.Scene;

		private readonly bool useIndicators;
		public readonly string indicatorPath;
		public MTexture indicatorTexture;
		public readonly bool allowDashSliding;
		private readonly bool disableTracks;
		private readonly bool isConstant;
		private readonly float constantSpeed;
		private const float vanillaSpeed = 360f;
		private readonly bool accelerate;
		private float speed;
		private Vector2 targetPosition;
		private Vector2 startPosition;
		private Vector2 lerpVector;

		public NewToggleSwapBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
			base.Depth = -9999;
			nodes = new Vector2[data.Nodes.Length + 1];
			nodes[0] = base.Position;
			for (int i = 0; i < data.Nodes.Length; i++) {
				nodes[i + 1] = data.NodesOffset(offset)[i];
			}
			travelSpeed = data.Float("travelSpeed", 5f);
			oscillate = data.Bool("oscillate", false);
			stopAtEnd = data.Bool("stopAtEnd", false);
			customTexturePath = data.Attr("customTexturePath", "");
			allowDashSliding = data.Bool("allowDashSliding", false);
			isConstant = data.Bool("constantSpeed", false);
			if (isConstant) {
				constantSpeed = travelSpeed * vanillaSpeed;
				travelSpeed = constantSpeed;
			}
			accelerate = data.Bool("accelerate", false);
			disableTracks = nodes.Length < 2 || data.Bool("disableTracks", false);
			useIndicators = data.Bool("directionIndicator", false);
			indicatorPath = data.Attr("customIndicatorPath", "");
			if (indicatorPath == "") {
				indicatorPath = defaultIndicatorPath;
			}
			if (indicatorPath.Last() != '/') {
				indicatorPath += '/';
			}
			DashListener val = new DashListener();
			DashListener val2 = val;
			dashListener = val;
			base.Add(val2);
			base.Add(new LightOcclude(0.2f));
			dashListener.OnDash = OnPlayerDashed;
			MTexture val3 = GFX.Game[(customTexturePath.Length <= 0) ? "objects/canyon/toggleblock/block1" : customTexturePath];
			nineSliceBlock = new MTexture[3, 3];
			for (int j = 0; j < 3; j++) {
				for (int k = 0; k < 3; k++) {
					nineSliceBlock[j, k] = val3.GetSubtexture(new Rectangle(j * 8, k * 8, 8, 8));
				}
			}
			if (oscillate || stopAtEnd || nodes.Length <= 2) {
				laserCount = nodes.Length - 1;
			} else {
				laserCount = nodes.Length;
			}
			lasers = new ToggleBlockLaser[laserCount];
			nodeTextures = new ToggleBlockNode[nodes.Length];
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			if (!disableTracks) {
				Vector2 radius = new Vector2(base.Width, base.Height) / 2f;
				if (nodes.Length != 2) {
					for (int i = 0; i < laserCount; i++) {
						level.Add(lasers[i] = Engine.Pooler.Create<ToggleBlockLaser>().Init(nodes[i] + radius, nodes[GetNextNode(i)] + radius));
					}
				} else {
					level.Add(lasers[0] = Engine.Pooler.Create<ToggleBlockLaser>().Init(nodes[0] + radius, nodes[1] + radius));
				}
				for (int j = 0; j < nodes.Length; j++) {
					level.Add(nodeTextures[j] = Engine.Pooler.Create<ToggleBlockNode>().Init(nodes[j] + radius));
				}
			}
			UpdateSegment();
		}

		private int GetNextNode(int node) {
			if (!oscillate && node == nodes.Length - 1) {
				return 0;
			}
			if (!oscillate) {
				return node + 1;
			}
			if (node == nodes.Length - 1) {
				return node - 1;
			}
			if (node != 0) {
				return node + ((!returning) ? 1 : (-1));
			}
			return 1;
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			if (moveSfx != null) {
				Audio.Stop(moveSfx, true);
			}
		}

		public override void SceneEnd(Scene scene) {
			base.SceneEnd(scene);
			if (moveSfx != null) {
				Audio.Stop(moveSfx, true);
			}
		}

		public override void Update() {
			base.Update();

			if (moving) {
				if (base.Position != targetPosition) {
					speed = Calc.Approach(speed, travelSpeed, travelSpeed / 0.2f * Engine.DeltaTime);
					lerp = Calc.Approach(lerp, 1f, speed * Engine.DeltaTime);
					base.MoveTo(Vector2.Lerp(startPosition, targetPosition, lerp), lerpVector);
				} else {
					lerp = 0f;
					(base.Scene as Level).Displacement.AddBurst(base.Center, 0.2f, 0f, 16f, 1f, null, null);
					moving = false;
					//middleRed.Play("idle", false, false);
					Audio.Stop(moveSfx, true);
					moveSfx = null;
					Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", base.Center);
					UpdateSegment();
				}
			}
		}

		private void UpdateSegment() {
			Vector2 dir = nodes[GetNextNode(nodeIndex)] - nodes[nodeIndex];

			if (isConstant) {
				travelSpeed = constantSpeed / dir.Length();
			}

			if (useIndicators) {
				int indicator;
				if (stopAtEnd && nodeIndex == nodes.Length - 1) {
					indicator = DONE;
				} else {
					if (dir.Equals(Vector2.Zero)) {
						indicator = STAY;
					} else {
						indicator = (int) Math.Round(dir.Angle() * (4 / Math.PI));
						if (indicator < 0) {
							indicator += 8;
						}
					}
				}
				indicatorTexture = GFX.Game[indicatorPath + paths[indicator]];
			}

			if (!disableTracks) {
				Vector2 radius = new Vector2(base.Width, base.Height) / 2f;
				for (int i = 0; i < laserCount; i++) {
					if (lasers[i].start == nodes[nodeIndex] + radius && lasers[i].end == nodes[GetNextNode(nodeIndex)] + radius) {
						lasers[i].color = onColor;
					} else if (lasers[i].end == nodes[nodeIndex] + radius && lasers[i].start == nodes[GetNextNode(nodeIndex)] + radius) {
						lasers[i].color = onColor;
					} else {
						lasers[i].color = offColor;
					}
					if (stopAtEnd && i == laserCount - 1) {
						lasers[i].color = endColor;
					}
				}
			}
		}

		private void OnPlayerDashed(Vector2 direction) {

			if (moving || stopped) {
				if (stopped) {
					base.StartShaking(0.25f);
				}
				return;
			}
			moving = true;
			if (nodeIndex < nodes.Length - 1 && nodeIndex != 0) {
				if (!returning) {
					nodeIndex++;
				} else {
					nodeIndex--;
				}
			} else if (nodeIndex == 0) {
				if (returning) {
					returning = false;
				}
				nodeIndex++;
			} else {
				if (stopAtEnd) {
					nodeIndex = -1;
					stopped = true;
					moving = false;
					base.StartShaking(0.5f);
					return;
				}
				if (oscillate) {
					nodeIndex--;
					returning = true;
				} else {
					nodeIndex = 0;
				}
			}
			//middleRed.Play("moving", false, false);
			moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", base.Center);
			targetPosition = nodes[nodeIndex];
			startPosition = base.Position;
			Direction = targetPosition - startPosition;
			lerpVector = Direction * travelSpeed;
			base.Scene.Tracker.GetEntity<Player>();

			speed = accelerate ? MathHelper.Lerp(travelSpeed * 0.333f, travelSpeed, 0f) : travelSpeed;
		}

		public override void Render() {
			int num = (oscillate || nodeIndex != nodes.Length - 1) ? ((!oscillate) ? (nodeIndex + 1) : ((nodeIndex == nodes.Length - 1) ? (nodeIndex - 1) : ((nodeIndex == 0) ? 1 : (nodeIndex + ((!returning) ? 1 : (-1)))))) : 0;
			for (int i = 0; i < nodes.Length; i++) {
				if (stopAtEnd && i == nodes.Length - 1) {
					nodeTextures[i].color = endColor;
				} else if (stopAtEnd && (i == 0 || (oscillate && i == nodes.Length - 2))) {
					nodeTextures[i].color = offColor;
				} else if (moving || stopped) {
					nodeTextures[i].color = offColor;
				} else {
					nodeTextures[i].color = ((num == i) ? onColor : offColor);
				}
			}
			DrawBlockStyle(base.Position + base.Shake, base.Width, base.Height, nineSliceBlock, null, Color.White);
		}

		private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) {
			int num = (int) (width / 8f);
			int num2 = (int) (height / 8f);
			ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
			ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
			ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
			ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
			for (int i = 1; i < num - 1; i++) {
				ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
				ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
			}
			for (int j = 1; j < num2 - 1; j++) {
				ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
				ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
			}
			for (int k = 1; k < num - 1; k++) {
				for (int l = 1; l < num2 - 1; l++) {
					ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
				}
			}
			if (middle != null) {
				middle.Color = color;
				middle.RenderPosition = (pos + new Vector2(width / 2f, height / 2f));
				middle.Render();
			}
			if (useIndicators) {
				indicatorTexture.DrawCentered(pos + base.Center - base.Position);
			}
		}
	}
}
