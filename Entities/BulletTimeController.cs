using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[CustomEntity("SJ2021/BTController")]


public class BTController : Entity
{
	float timerate = 1;
	public BTController(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		timerate = data.Float("speed");
	}
	public override void Update()
	{
		base.Update();
        Player player = Scene.Tracker.GetEntity<Player>();
        if (player != null)
            if (player.Dashes == 0 || player.IsIntroState == true || player.JustRespawned == true)
			    Engine.TimeRate = 1.0F;
		    else
			    Engine.TimeRate = timerate;
        else
            Engine.TimeRate = 1.0F;
	}
}

