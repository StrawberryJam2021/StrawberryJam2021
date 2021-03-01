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
        }

        public override void Update() {
            base.Update();
            LiftSpeed = targetSpeed;
            MoveHExact(0);  //force an update, probably a bad method of doing this
        }
        

        public override void MoveHExact(int move) {
            LiftSpeed = targetSpeed;
            base.MoveHExact(move);
        }

        public override void MoveVExact(int move) {
            LiftSpeed = targetSpeed;
            base.MoveVExact(move);
        }

        public override void Render() {
            //simple debug render for the time being
            Vector2 direction = targetSpeed;
            direction.Normalize();
            Vector2 startPos = new Vector2(Position.X + Width / 2, Position.Y + Height / 2);
            Vector2 endPos   = new Vector2(Position.X + Width / 2 + direction.X * Width / 4, Position.Y + Height / 2 + direction.Y * Height/4);
            Draw.HollowRect(Position, Width, Height, Color.White);
            Draw.Line(startPos , endPos , Color.Red);
            Draw.Line(endPos,endPos + GetAngledVector(startPos - endPos, 16, 45), Color.Red);
            Draw.Line(endPos,endPos + GetAngledVector(startPos - endPos, 16, -45), Color.Red);
        }

        static Vector2 GetAngledVector(Vector2 vector, float magnitude, float alpha) 
        {
            Vector2 returnV = new Vector2(vector.X * (float)Math.Cos(alpha) + vector.Y * (float) Math.Sin(alpha), -vector.X * (float) Math.Sin(alpha) + vector.Y * (float) Math.Cos(alpha));
            returnV.Normalize();
            returnV *= magnitude;
            return returnV;
        }

        Vector2 targetSpeed;
    }

}