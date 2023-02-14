﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/GrabTempleGate")]
    class GrabTempleGate : Solid {

        private const float switchTimeDelay = 0.2f;
        private int closedHeight;

        private Sprite sprite;
        private Shaker shaker;
        private float drawHeight;
        private float drawHeightMoveSpeed;

        private bool startClosed;
        private bool open;

        private float canSwitchTimer;

        private SoundSource sfx;

        public GrabTempleGate(Vector2 position, bool startClosed)
            : base(position, 8f, 48, safe: true) {
            closedHeight = 48;
            this.startClosed = startClosed;

            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("grabTempleGate"));
            sprite.X = Collider.Width / 2f;
            sprite.Play("idle");

            Add(sfx = new SoundSource());
            Add(shaker = new Shaker(on: false));
            Depth = Depths.Solids;
        }

        public GrabTempleGate(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("closed")) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (!startClosed) {
                StartOpen();
            }
            drawHeight = Math.Max(4f, Height);
        }

        public bool CloseBehindPlayerCheck() {
            Player entity = Scene.Tracker.GetEntity<Player>();
            if (entity != null) {
                return entity.X < X;
            }
            return false;
        }

        public void SwitchOpen() {
            sprite.Play("open");
            Alarm.Set(this, 0.2f, () => {
                shaker.ShakeFor(0.2f, removeOnFinish: false);
                Alarm.Set(this, 0.2f, Open);
            });
        }

        public void Open() {
            sfx.Play(SFX.game_05_gate_main_open);
            drawHeightMoveSpeed = 200f;
            drawHeight = Height;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(0);
            sprite.Play("open");
            open = true;
        }

        public void StartOpen() {
            SetHeight(0);
            drawHeight = 4f;
            open = true;
        }

        public void Close() {
            sfx.Play(SFX.game_05_gate_main_close);
            drawHeightMoveSpeed = 300f;
            drawHeight = Math.Max(4f, Height);
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(closedHeight);
            sprite.Play("hit");
            open = false;
        }

        private void SetHeight(int height) {
            if (height < Collider.Height) {
                Collider.Height = height;
                return;
            }
            float y = Y;
            int num = (int) Collider.Height;
            if (Collider.Height < 64f) {
                Y -= 64f - Collider.Height;
                Collider.Height = 64f;
            }

            // check if the player is above the gate's top y level, and if so,
            // perform a naive move down so as to not snap the player down, possibly killing it.
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null && player.Bottom <= Bottom) // note: Bottom was Top before the collider was changed in the code above.
                MoveVNaive(height - num);
            else
                MoveV(height - num);
            
            Y = y;
            Collider.Height = height;
        }

        public override void Update() {
            base.Update();
            canSwitchTimer = Calc.Approach(canSwitchTimer, 0f, Engine.DeltaTime);
            if (Input.Grab.Pressed && canSwitchTimer == 0f) {
                sfx.Stop();
                if (open) {
                    Close();
                } else {
                    Open();
                }
                canSwitchTimer = switchTimeDelay;
            }

            float num = Math.Max(4f, Height);
            if (drawHeight != num) {
                drawHeight = Calc.Approach(drawHeight, num, drawHeightMoveSpeed * Engine.DeltaTime);
            }
        }

        public override void Render() {
            Vector2 value = new Vector2(Math.Sign(shaker.Value.X), 0f);
            Draw.Rect(X - 2f, Y - 8f, 13f, 10f, Color.Black);
            sprite.DrawSubrect(Vector2.Zero + value, new Rectangle(0, (int) (sprite.Height - drawHeight), (int) sprite.Width, (int) drawHeight));
        }

    }
}
