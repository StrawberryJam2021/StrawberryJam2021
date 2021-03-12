using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {

[CustomEntity("SJ2021/MomentumBlock")]
    public class MomentumBlock : Solid {
        const float MAX_SPEED = 282; //internally the player has a max lift boost in each direction
        const float MAX_SPEED_X = 250; 
        const float MAX_SPEED_Y = -130;
        private Vector2 targetSpeed, targetSpeedFlagged;
        private Color speedColor, speedColorFlagged;
        private float angle, angleFlagged;
        private Color startColor, endColor;
        private MTexture arrowTexture, arrowTextureFlagged;
        private string flag;
        private bool isFlagged;

        public MomentumBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Float("speed"), data.Float("direction"), data.Float("speedFlagged"), data.Float("directionFlagged"), data.Attr("startColor"), data.Attr("endColor"), data.Attr("flag")) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            UpdateFlag();
        }

        public MomentumBlock(Vector2 position, int width, int height, float spd, float dir, float spdFlagged, float dirFlagged, string startC, string endC, string flg) : base(position, width, height, safe: false) {
            flag = flg;
            isFlagged = false;
            dir = Calc.ToRad(dir);  //convert to radians
            dirFlagged = Calc.ToRad(dirFlagged);

            targetSpeed = Calc.AngleToVector(dir, spd);
            targetSpeedFlagged = Calc.AngleToVector(dirFlagged, spdFlagged);

            //bound the components to their respective max for accurate angles
            ClampLiftBoost(targetSpeed);
            ClampLiftBoost(targetSpeedFlagged);
            
            angle = dir;
            angleFlagged = dirFlagged;

            //calculate the color gradient
            startColor = Calc.HexToColor(startC);
            endColor = Calc.HexToColor(endC);
            speedColor = CalculateGradient(spd);
            speedColorFlagged = CalculateGradient(spdFlagged);
            
            arrowTexture = GetArrowTexture(angle);
            arrowTextureFlagged = GetArrowTexture(angleFlagged);
        }

        public static MTexture GetArrowTexture(float angle) {
            int value = (int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
            return GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow")[Calc.Clamp(value, 0, 7)];
        }

        public static void ClampLiftBoost(Vector2 liftBoost) {
            liftBoost.X = Calc.Clamp(liftBoost.X, -MAX_SPEED_X, MAX_SPEED_X);
            liftBoost.Y = Calc.Clamp(liftBoost.Y, MAX_SPEED_Y, 0);
        }

        public Color CalculateGradient(float spd) {
            float g = (float) (1 - Math.Abs((1.0 - spd / MAX_SPEED) % 2f - 1)); //smooth the linear gradient
            g = -g + 1;
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
            Draw.HollowRect(Position, Width, Height, isFlagged ? speedColorFlagged : speedColor);
            Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, isFlagged ? speedColorFlagged : speedColor);
            if (isFlagged)
                arrowTextureFlagged.DrawCentered(Center);
            else
                arrowTexture.DrawCentered(Center);            
        }

        private void UpdateFlag() {
            if (!string.IsNullOrEmpty(flag))

                isFlagged = SceneAs<Level>().Session.GetFlag(flag);
        }
    }
}
