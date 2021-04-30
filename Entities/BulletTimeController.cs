using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class BTController : Entity
{
	public BTController(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		X = data.Float("speed");
	}
	public override void Update()
	{
		base.Update();
		Player player = Scene.Tracker.GetEntity<Player>();
		if (player.Dashes == 0)
			Engine.TimeRate = 1.0F;
		else
			Engine.TimeRate = X;
	}
}

