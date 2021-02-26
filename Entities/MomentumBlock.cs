using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {

[CustomEntity("SJ2021/MomentumBlock")]
    public class MomentumBlock : Solid {

        public MomentumBlock(EntityData data, Vector2 offset) :this(data.Position + offset, data.Width, data.Height, data.Float("speed"), data.Float("direction")){
            //
        }

        public MomentumBlock(Vector2 position, int width, int height, float spd, float dir) : base(position, width, height, safe: false) {
            
            dir = dir / 180.0f * (float)Math.PI;  //convert to radians
            targetSpeed = new Vector2(spd * (float)Math.Cos(dir), spd * (float)Math.Sin(dir));
            lastPlayer = null;
        }


	    public override void Update() {
            base.Update();
            Player player;
            if(HasPlayerOnTop())
                player = GetPlayerOnTop();
            else
            if(HasPlayerClimbing())
                player = GetPlayerClimbing();
            else
            {
                if(lastPlayer != null && lastPlayer.Speed.Y < -0.01) //epsilon
                {
                    //lastPlayer.StartJumpGraceTime();
                    lastPlayer.Speed += targetSpeed;
    		    }
                lastPlayer = null;
                return;
            }
            lastPlayer = player;
        }

        public override void Render() {
            //simple debug render for the time being
            Draw.Line(Position, Position + new Vector2(0, Height), Color.White);
            Draw.Line(Position, Position + new Vector2(Width, 0), Color.White);
            Draw.Line(Position + new Vector2(Width, 0), Position + new Vector2(Width, Height), Color.White);
            Draw.Line(Position + new Vector2(0, Height), Position + new Vector2(Width, Height), Color.White);
        }
       Player lastPlayer;

        Vector2 targetSpeed;
    }

}