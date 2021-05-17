using System;
using Celeste;
using Celeste.Mod.Entities;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using MonoMod.Utils;
using MonoMod.Cil;
using Celeste.Mod;
using Mono.Cecil.Cil;

namespace Celeste.Mod.StrawberryJam2021.Entities
{
	[Tracked(false)]
	[CustomEntity("SJ2021/HorizontalTempleGate")]
	public class HorizontalTempleGate : Solid
	{
		public enum Types
		{
			NearestSwitch,
			TouchSwitches,
			FlagActive
		}

		public enum OpenDirections//for context, this is implemented to allow the gate to open to the right, to the left or from the center of the gate's position. The center direction applies both the left and right door's hitboxes and sprites, modified to halve their extended distance from their respective side.
		{
			Left,
			Right,
			Center
		}

		private Hitbox collider;

		private Hitbox collideralt;

		private bool Inverted;

		private string Flag;

		private string Texture;

		private int OpenWidth;

		private int MinDrawWidth;

		private string LevelID;

		public OpenDirections OpenDirection;

		public Types Type;

		public bool ClaimedByASwitch;

		private Sprite sprite;

		private Sprite spritealt;

		private Shaker shaker;

		private float drawWidth;

		private float drawWidthMoveSpeed;

		private bool open;

		private bool lockState;

		private float width;

//The full section between this comment and the next was written by lilybeevee, intended to allow this entity to function with dash switches while in the same room as a regular temple gate. This doesn't yet work and is where I'm currently stumped.
		public static void Load()
		{
			IL.Celeste.DashSwitch.OnDashed += DashSwitch_OnDashed;
		}

		private static void DashSwitch_OnDashed(ILContext il)
		{
			var cursor = new ILCursor(il);

			if (cursor.TryGotoNext(instr => instr.MatchLdfld<DashSwitch>("allGates")))
			{
				if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out _)))
				{
					Logger.Log("SJ2021/HorizontalTempleGate", $"Adding IL hook at {cursor.Index} in DashSwitch.OnDashed (1/2)");
					cursor.Emit(OpCodes.Ldarg_0);
					cursor.EmitDelegate<Action<DashSwitch>>(self => {
						var data = new DynData<DashSwitch>(self);
						foreach (HorizontalTempleGate entity in self.Scene.Tracker.GetEntities<HorizontalTempleGate>())
						{
							if (entity.Type == HorizontalTempleGate.Types.NearestSwitch && entity.LevelID == data.Get<EntityID>("id").Level)
							{
								entity.SwitchOpen();
							}
						}
					});
				}
			}
			cursor.Index = 0;

			if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<DashSwitch>("GetGate")))
			{
				Logger.Log("SJ2021/HorizontalTempleGate", $"Adding IL hook at {cursor.Index} in DashSwitch.OnDashed (2/2)");

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.EmitDelegate<Func<TempleGate, DashSwitch, TempleGate>>((templeGate, self) => {
					var data = new DynData<DashSwitch>(self);
					var entities = self.Scene.Tracker.GetEntities<HorizontalTempleGate>();
					HorizontalTempleGate hTempleGate = null;
					float dist = 0f;
					foreach (HorizontalTempleGate item in entities)
					{
						if (item.Type == HorizontalTempleGate.Types.NearestSwitch && !item.ClaimedByASwitch && item.LevelID == data.Get<EntityID>("id").Level)
						{
							float currentDist = Vector2.DistanceSquared(self.Position, item.Position);
							if (hTempleGate == null || currentDist < dist)
							{
								hTempleGate = item;
								dist = currentDist;
							}
						}
					}
					if (hTempleGate != null && (templeGate == null || dist < Vector2.DistanceSquared(self.Position, templeGate.Position)))
					{
						if (templeGate != null)
						{
							templeGate = null;
							templeGate.ClaimedByASwitch = false;
						}
						hTempleGate.ClaimedByASwitch = true;
						hTempleGate.SwitchOpen();
					}
					return templeGate;
				});
			}
		}
		public static void Unload()
		{
			IL.Celeste.DashSwitch.OnDashed -= DashSwitch_OnDashed;
		}
//comment to indicate end of lilybeevee's IL hook
//the bulk of the rest of this code is primarily based on the vanilla templegate, modified for a horizontal direction and to function with Ahorn and Everest. Likely reading the vanilla templegate's code will provide better indication to what this code's counterparts' purposes are.
		public HorizontalTempleGate(EntityData data, Vector2 offset)
			: base(data.Position + offset, data.Enum<OpenDirections>("direction", OpenDirections.Left) == OpenDirections.Center ? 24f : 48f, 8f, safe: true)
		{
			this.Type = data.Enum<Types>("type", Types.FlagActive);
			this.OpenDirection = data.Enum<OpenDirections>("direction", OpenDirections.Left);
			this.OpenWidth = data.Int("openWidth", 0);
			this.Inverted = data.Bool("inverted", false);
			this.Flag = data.Attr("flag", "");
			this.Texture = data.Attr("texture", "objects/horizontalTempleGate/default");
			base.Depth = -9000;
			Add(shaker = new Shaker(on: false));
			MinDrawWidth = Math.Max(4, this.OpenWidth);
			this.width = base.Width;
			base.Collider = new ColliderList();
			if (this.OpenDirection != OpenDirections.Right)
			{
				collider = new Hitbox(this.width, 8f, 0, 0);
				((ColliderList)base.Collider).Add(collider);
//it is worth noting that these sprites work for a single frame, but I haven't yet implemented multiple frames in the texture so I do not know yet whether the gate animates correctly. This goes for both sprite and spritealt.
//sprite is used in rendering the left door, while spritealt is used in rendering the right. In the Left or Right open directions, spritealt or sprite respectively is unutilized.
				Add(sprite = new Sprite(GFX.Game, Texture + "/left"));
				sprite.Add("open", "", 1f, "idle");
				sprite.Add("close", "", -1f, "idle");
				sprite.Add("idle", "", 0f);
				sprite.Play("idle");
				sprite.SetAnimationFrame(0);
			}
			if (this.OpenDirection != OpenDirections.Left)
			{
				collideralt = new Hitbox(this.width, 8f, 48f - this.width, 0);
				((ColliderList)base.Collider).Add(collideralt);
				Add(spritealt = new Sprite(GFX.Game, Texture + "/right"));
				spritealt.Add("open", "", 1f, "idle");
				spritealt.Add("close", "", -1f, "idle");
				spritealt.Add("idle", "", 0f);
				spritealt.Play("idle");
				spritealt.SetAnimationFrame(0);
			}
		}
		public override void Awake(Scene scene)
		{
			base.Awake(scene);
			Level level = Scene as Level;
			if (this.Type == Types.TouchSwitches)
			{
				Add(new Coroutine(CheckTouchSwitches(), false));
			}
			if (this.Type == Types.FlagActive)
            {
				Add(new Coroutine(CheckFlag(this.Flag), false));
            }
			drawWidth = Math.Max(MinDrawWidth, this.width);
		}

		public void SwitchOpen()//applies a delay to utilization of Open in the case the door is opened by a dash switch. I don't know why this was implemented, but for consistency with the vanilla temple gate this is being used.
		{
			if (this.OpenDirection != OpenDirections.Right)
			{
				sprite.Play("open");
			}
			if (this.OpenDirection != OpenDirections.Left)
			{
				spritealt.Play("open");
			}
			Alarm.Set(this, 0.2f, delegate
			{
				shaker.ShakeFor(0.2f, removeOnFinish: false);
				Alarm.Set(this, 0.2f, Open);
			});
		}

		public void Open()
		{
			Audio.Play("event:/game/05_mirror_temple/gate_main_open", Position);
			drawWidthMoveSpeed = 200f;
			drawWidth = this.width;
			shaker.ShakeFor(0.2f, removeOnFinish: false);
			SetWidth(OpenWidth);
			if (this.OpenDirection != OpenDirections.Right)
			{
				sprite.Play("open");
			}
			if (this.OpenDirection != OpenDirections.Left)
			{
				spritealt.Play("open");
			}
			this.open = true;
		}

		public void StartOpen()
		{
			SetWidth(OpenWidth);
			drawWidth = MinDrawWidth;
			this.open = true;
		}

		public void Close()
		{
			Audio.Play("event:/game/05_mirror_temple/gate_main_close", Position);
			drawWidthMoveSpeed = 300f;
			drawWidth = Math.Max(MinDrawWidth, this.width);
			shaker.ShakeFor(0.2f, removeOnFinish: false);
			SetWidth((int)this.width);
			if (this.OpenDirection != OpenDirections.Right)
			{
				sprite.Play("close");
			}
			if (this.OpenDirection != OpenDirections.Left)
			{
				spritealt.Play("close");
			}
			this.open = false;
		}

		private IEnumerator CheckTouchSwitches()
		{
			while (true)
				{
					while ((this.open == Switch.Check(Scene)) != this.Inverted)
				{
					yield return null;
				}
				if (this.OpenDirection != OpenDirections.Right)
				{
					sprite.Play(this.open ? "close" : "open");
				}
				if (this.OpenDirection != OpenDirections.Left)
				{
					spritealt.Play(this.open ? "close" : "open");
				}
				yield return 0.5f;
				shaker.ShakeFor(0.2f, removeOnFinish: false);
				yield return 0.2f;
				while (lockState)
				{
					yield return null;
				}
				if (Switch.Check(Scene) != this.Inverted)
				{
					Open();
				}
				else
				{
					Close();
				}
				yield return null;
			}
		}

		private IEnumerator CheckFlag(string flag)
        {
			while (true)
			{
				Level level = Scene as Level;
				while ((this.open == level.Session.GetFlag(flag)) != this.Inverted)
				{
					yield return null;
				}
				if (this.OpenDirection != OpenDirections.Right)
				{
					sprite.Play(this.open ? "close" : "open");
				}
				if (this.OpenDirection != OpenDirections.Left)
				{
					spritealt.Play(this.open ? "close" : "open");
				}
				while (lockState)
				{
					yield return null;
				}
				if (level.Session.GetFlag(flag) != this.Inverted)
				{
					Open();
				}
				else
				{
					Close();
				}
				yield return null;
			}
		}

		private void SetWidth(int width)
		{
			this.Collidable = (width == 0) ? false : true;
			if (this.OpenDirection != OpenDirections.Right)
            {
				if (this.width < collider.Width)
				{
					collider.Width = width;
					return;
				}
				float x = collider.Position.X;
				if (collider.Width < this.width)
				{
					collider.Position.X -= this.width - collider.Width;
					collider.Width = this.width;
				}
				collider.Position.X = x;
				collider.Width = width;
			}
            if (this.OpenDirection != OpenDirections.Left)
//I do not remember the purpose of the following and currently commented code.
			{
//				if (this.width < collideralt.Width)
//				{
//					collideralt.Width = width;
//					return;
//				}
//				float x = collideralt.Position.X;
//				int num = (int)collideralt.Width;
//				if (collideralt.Width < this.width)
//				{
//					collider.Position.X -= this.width - collideralt.Width;
//					collideralt.Width = this.width;
//				}
//				MoveHExact(width - num);
				collideralt.Position.X = 48f - width;
				collideralt.Width = width;
			}
		}

		public override void Update()
		{
			base.Update();
			float num = Math.Max(MinDrawWidth, this.OpenDirection == OpenDirections.Right ? collideralt.Width : collider.Width);
			if (drawWidth != num)
			{
				lockState = true;
				drawWidth = Calc.Approach(drawWidth, num, drawWidthMoveSpeed * this.width / 48 * Engine.DeltaTime);
			}
			else
			{
				lockState = false;
			}
		}

		public override void Render()
//this section has somewhat overcomplicated formulae to calculate width and position of door sprites, as potential for overlap between sprites using the center direction should a sprite be made larger than its default size is being accounted for. This is to allow for textures to expand beyond the halfway point for more intricate overlap between the left and right door with center open direction.
		{
            if (this.OpenDirection != OpenDirections.Right)
			{
				Vector2 value = new Vector2(0f, Math.Sign(shaker.Value.X));
				sprite.DrawSubrect(new Vector2(0, -3) + value, new Rectangle((int)(sprite.Width - ((sprite.Width - 48f) * (1 - ((this.width - drawWidth) / (this.width - MinDrawWidth)))) - drawWidth), 0, (int)sprite.Width, (int)sprite.Height));
			}
			if (this.OpenDirection != OpenDirections.Left)
            {
				Vector2 valuealt = new Vector2(0f, Math.Sign(shaker.Value.Y));
				spritealt.DrawSubrect(new Vector2(48f - drawWidth - ((spritealt.Width - 48f) * (1 - ((this.width - drawWidth) / (this.width - MinDrawWidth)))), -3) + valuealt, new Rectangle(0, 0, (int)(drawWidth + ((spritealt.Width - 48f) * (1 - ((this.width - drawWidth) / (this.width - MinDrawWidth))))), (int)spritealt.Height));
			}
		}
	}

}