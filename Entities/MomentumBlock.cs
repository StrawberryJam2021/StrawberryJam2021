using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using System;
using System.Globalization;
namespace Celeste.Mod.StrawberryJam2021.Entities {

[CustomEntity("SJ2021/MomentumBlock")]
    public class MomentumBlock : Solid {

        const float MAX_SPEED = 282;
        const float MAX_SPEED_X = 250; //internally the player has a max lift boost
        const float MAX_SPEED_Y = -130;

        public MomentumBlock(EntityData data, Vector2 offset) :this(data.Position + offset, data.Width, data.Height, data.Float("speed"), data.Float("direction"), data.Attr("startColor"), data.Attr("endColor")){
            //
        }

        public MomentumBlock(Vector2 position, int width, int height, float spd, float dir, string startC, string endC) : base(position, width, height, safe: false) {            
            dir = dir / 180.0f * (float)Math.PI;  //convert to radians
            targetSpeed = new Vector2(spd * (float)Math.Cos(dir), spd * (float)Math.Sin(dir));
            
            //bound the components to their respective max for accurate angles
            targetSpeed.X = Calc.Clamp(targetSpeed.X, -MAX_SPEED_X, MAX_SPEED_X);
            targetSpeed.Y = Calc.Clamp(targetSpeed.Y, MAX_SPEED_Y , 0);

            //get the newly bounded angle

            angle = dir;
            
            startColor = ToColor(startC);
            endColor = ToColor(endC);
            speedColor = CalculateGradient(spd);
            
            int value = (int)Math.Floor((0f - angle + (float)Math.PI * 2f) % ((float)Math.PI * 2f) / ((float)Math.PI * 2f) * 8f + 0.5f);
            arrowTexture = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow")[Calc.Clamp(value, 0, 7)];
        }

        public Color CalculateGradient(float spd) {
            float g = (float)(1 -Math.Abs((1.0 - spd / MAX_SPEED) % 2.0f - 1)); //smooth the gradient
            g=-g+1;
            
            return new Color((int)(g * (endColor.R - startColor.R) + startColor.R),
                             (int)(g * (endColor.G - startColor.G) + startColor.G),
                             (int)(g * (endColor.B - startColor.B) + startColor.B),
                             255);
        }

        public override void Update() {
            base.Update();
            MoveHExact(0);  //force a lift update
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
            Draw.HollowRect(Position, Width, Height, speedColor);
            Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, speedColor);    
            arrowTexture.DrawCentered(Center);            
        }

        //should probably be moved elsewhere?
        static Color ToColor(string str) {
            try {
                return new Color(
                            int.Parse(str.Substring(0, 2), NumberStyles.HexNumber),
                            int.Parse(str.Substring(2, 2), NumberStyles.HexNumber),    
                            int.Parse(str.Substring(4, 2), NumberStyles.HexNumber),
                            255);
            }
            catch {
                Logger.Log(LogLevel.Warn,"StrawberryJam2021", "Error reading momentum block color value.\n");
            }
            return new Color();
        }


        private Vector2 targetSpeed;
        private Color speedColor;
        private Color startColor, endColor;
        private MTexture arrowTexture;
        private float angle;

    }

}