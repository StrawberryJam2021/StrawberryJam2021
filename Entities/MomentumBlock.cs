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

        public MomentumBlock(EntityData data, Vector2 offset) :this(data.Position + offset, data.Width, data.Height, data.Float("speed"), data.Float("direction"), data.Float("speedFlagged"), data.Float("directionFlagged"), data.Attr("startColor"), data.Attr("endColor"), data.Attr("flag")){
            //
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            UpdateFlag();
        }

        public MomentumBlock(Vector2 position, int width, int height, float spd, float dir, float spdFlagged, float dirFlagged, string startC, string endC, string flg) : base(position, width, height, safe: false) {
            flag = flg;
            isFlagged = false;
            dir = dir / 180.0f * (float)Math.PI;  //convert to radians
            dirFlagged = dirFlagged / 180.0f * (float) Math.PI;

            targetSpeed = new Vector2(spd * (float)Math.Cos(dir), spd * (float)Math.Sin(dir));
            targetSpeedFlagged = new Vector2(spdFlagged * (float) Math.Cos(dirFlagged), spdFlagged * (float) Math.Sin(dirFlagged));

            //bound the components to their respective max for accurate angles
            targetSpeed.X = Calc.Clamp(targetSpeed.X, -MAX_SPEED_X, MAX_SPEED_X);
            targetSpeed.Y = Calc.Clamp(targetSpeed.Y, MAX_SPEED_Y , 0);

            targetSpeedFlagged.X = Calc.Clamp(targetSpeedFlagged.X, -MAX_SPEED_X, MAX_SPEED_X);
            targetSpeedFlagged.Y = Calc.Clamp(targetSpeedFlagged.Y, MAX_SPEED_Y, 0);
            //get the newly bounded angle

            angle = dir;
            angleFlagged = dirFlagged;
            
            startColor = Monocle.Calc.HexToColor(startC);
            endColor = Monocle.Calc.HexToColor(endC);

            speedColor = CalculateGradient(spd);
            speedColorFlagged = CalculateGradient(spdFlagged);
            
            arrowTexture = GetArrowTexture(angle);
            arrowTextureFlagged = GetArrowTexture(angleFlagged);
        }

        public static MTexture GetArrowTexture(float angle) {
            int value = (int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
            return GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow")[Calc.Clamp(value, 0, 7)];
        }

        public Color CalculateGradient(float spd) {
            float g = (float)(1 -Math.Abs((1.0 - spd / MAX_SPEED) % 2.0f - 1)); //smooth the linear gradient
            g=-g+1;
            return Color.Lerp(startColor, endColor, g);
        }

        public override void Update() {
            base.Update();
            UpdateFlag();
            MoveHExact(0);  //force a lift update
        }
        

        public override void MoveHExact(int move) {
            LiftSpeed = isFlagged ? targetSpeedFlagged : targetSpeed;
            base.MoveHExact(move);
        }

        public override void MoveVExact(int move) {
            LiftSpeed = isFlagged ? targetSpeedFlagged : targetSpeed;
            base.MoveVExact(move);
        }
        
        public override void Render() {
            Draw.HollowRect(Position, Width, Height, isFlagged? speedColorFlagged : speedColor);
            Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, isFlagged ? speedColorFlagged : speedColor);
            if (isFlagged)
                arrowTextureFlagged.DrawCentered(Center);
            else
                arrowTexture.DrawCentered(Center);            
        }

        private void UpdateFlag() {
            if(!String.IsNullOrEmpty(flag))
                isFlagged = SceneAs<Level>().Session.GetFlag(flag);
        }

        private Vector2 targetSpeed, targetSpeedFlagged;
        private Color speedColor, speedColorFlagged;
        private float angle, angleFlagged;
        private Color startColor, endColor;
        private MTexture arrowTexture, arrowTextureFlagged;
        private string flag;

        bool isFlagged;
    }

}