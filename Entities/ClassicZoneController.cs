using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked(false)]
    public class ClassicZoneController : Entity {
        private static ClassicZoneController _instance;
        private bool PlayerInZone { get; set; }
        public bool SkipFrame { get; private set; }

        // TODO: Remove most of these and use DynData to use existing private player variables for better interactions
        private int jbuffer;
        private int grace;
        private int djump;
        private int dashTime;
        private int dashEffectTime;
        private Vector2 dashTarget = new Vector2(0, 0);
        private Vector2 dashAccel = new Vector2(0, 0);
        private int sprite = 1;
        private float spriteOffset;

        public ClassicZoneController() {
            Tag = Tags.Global | Tags.PauseUpdate;
            _instance = this;
        }

        public override void Update() {
            base.Update();
            if (!Scene.Paused) {
                SkipFrame = !SkipFrame;
            }
        }

        public static void Load() {
            On.Celeste.Player.Update += OnPlayerUpdate;
            On.Celeste.Player.Render += OnPlayerRender;
        }

        public static void Unload() {
            On.Celeste.Player.Update -= OnPlayerUpdate;
            On.Celeste.Player.Render -= OnPlayerRender;
        }

        // Most code in the hooks is taken from https://github.com/NoelFB/Celeste/blob/master/Source/PICO-8/Classic.cs
        // and heavily modified. Note that it doesn't aim to keep 100% accuracy and this whole approach is a bit hacky
        private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
            if (_instance == null) {
                return;
            }

            ClassicZone classicZone = self.CollideFirst<ClassicZone>();
            bool playerInZone = classicZone != null && classicZone.PlayerHasDreamDash;
            if (playerInZone && !_instance.PlayerInZone) {
                self.Speed /= 90f;
            } else if (!playerInZone && _instance.PlayerInZone) {
                self.Speed *= 90f;
            }

            _instance.PlayerInZone = playerInZone;

            if (!_instance.PlayerInZone) {
                orig(self);
                return;
            }

            if (_instance.SkipFrame) {
                return;
            }

            // camera update
            var from = ((Level) Engine.Scene).Camera.Position;
            var target = self.CameraTarget;
            ((Level) Engine.Scene).Camera.Position =
                from + (target - from) * (1f - (float) Math.Pow(0.01f, Engine.DeltaTime));

            // I roughly estimated that the pico8 speed is 7.5 b/s, speed gets up to 1 and madeline's speed is 11.25 b/s
            // 
            self.MoveH(self.Speed.X * (11.25f / 7.5f));
            self.MoveV(self.Speed.Y * (11.25f / 7.5f));

            var input = Input.MoveX;

            bool onGround = self.OnGround();

            // TODO: Remove buffering from Input.*.Pressed when options becomes available

            var jump = Input.Jump.Pressed;
            if (jump)
                _instance.jbuffer = 4;
            else if (_instance.jbuffer > 0)
                _instance.jbuffer--;

            var dash = Input.Dash.Pressed;

            if (onGround) {
                _instance.grace = 6;
                if (_instance.djump < self.MaxDashes) {
                    Audio.Play("event:/classic/sfx54");
                    _instance.djump = self.MaxDashes;
                }
            } else if (_instance.grace > 0)
                _instance.grace--;

            _instance.dashEffectTime--;
            if (_instance.dashTime > 0) {
                _instance.dashTime--;
                self.Speed.X = Approach(self.Speed.X, _instance.dashTarget.X, _instance.dashAccel.X);
                self.Speed.Y = Approach(self.Speed.Y, _instance.dashTarget.Y, _instance.dashAccel.Y);
            } else {
                // move
                float accel = 0.6f;
                float deccel = 0.15f;

                if (!onGround) {
                    accel = 0.4f;
                }

                if (Math.Abs(self.Speed.X) > 1)
                    self.Speed.X = Approach(self.Speed.X, Math.Sign(self.Speed.X), deccel);
                else
                    self.Speed.X = Approach(self.Speed.X, input, accel);

                // facing
                if (self.Speed.X != 0)
                    self.Facing = (self.Speed.X < 0) ? Facings.Right : Facings.Left;

                // gravity
                float maxfall = 2f;
                float gravity = 0.21f;

                if (Math.Abs(self.Speed.Y) <= 0.15f)
                    gravity *= 0.5f;

                // wall slide
                if (input != 0 &&/* is_solid(input, 0) */ false) {
                    maxfall = 0.4f;
                }

                if (!onGround)
                    self.Speed.Y = Approach(self.Speed.Y, maxfall, gravity);

                // jump
                if (_instance.jbuffer > 0) {
                    if (_instance.grace > 0) {
                        // normal jump
                        Audio.Play("event:/classic/sfx1");
                        _instance.jbuffer = 0;
                        _instance.grace = 0;
                        self.Speed.Y = -2;
                    } else {
                        // wall jump
                        int wallDir = /* (is_solid(-3, 0) ? -1 : (is_solid(3, 0) ? 1 : 0)) */ 0;
                        if (wallDir != 0) {
                            Audio.Play("event:/classic/sfx2");
                            _instance.jbuffer = 0;
                            self.Speed.Y = -2;
                            self.Speed.X = -wallDir * 2;
                        }
                    }
                }

                // dash
                int dFull = 5;
                float dHalf = dFull * 0.70710678118f;

                if (_instance.djump > 0 && dash) {
                    _instance.djump--;
                    _instance.dashTime = 4;
                    _instance.dashEffectTime = 10;

                    int dashXInput = Math.Sign(Input.GetAimVector(self.Facing).X);
                    int dashYInput = Math.Sign(Input.GetAimVector(self.Facing).Y);

                    if (dashXInput != 0 && dashYInput != 0) {
                        self.Speed.X = dashXInput * dHalf;
                        self.Speed.Y = dashYInput * dHalf;
                    } else if (dashXInput != 0) {
                        self.Speed.X = dashXInput * dFull;
                        self.Speed.Y = 0;
                    } else {
                        self.Speed.X = 0;
                        self.Speed.Y = dashYInput * dFull;
                    }

                    Audio.Play("event:/classic/sfx3");
                    Celeste.Freeze(time: 0.06666667f);
                    (_instance.Scene as Level)?.Shake(0.2f);
                    _instance.dashTarget.X = 2 * Math.Sign(self.Speed.X);
                    _instance.dashTarget.Y = 2 * Math.Sign(self.Speed.Y);
                    _instance.dashAccel.X = 1.5f;
                    _instance.dashAccel.Y = 1.5f;

                    if (self.Speed.Y < 0)
                        _instance.dashTarget.Y *= 0.75f;
                    if (self.Speed.Y != 0)
                        _instance.dashAccel.X *= 0.70710678118f;
                    if (self.Speed.X != 0)
                        _instance.dashAccel.Y *= 0.70710678118f;
                } else if (dash && _instance.djump <= 0) {
                    Audio.Play("event:/classic/sfx9");
                }
            }

            // animation
            _instance.spriteOffset += 0.25f;
            if (!onGround) {
                if (/*is_solid(input, 0)*/ false)
                    _instance.sprite = 5;
                else
                    _instance.sprite = 3;
            } else if (Input.MoveY == 1)
                _instance.sprite = 6;
            else if (Input.MoveY == -1)
                _instance.sprite = 7;
            else if (self.Speed.X == 0 || (Input.MoveX == 0))
                _instance.sprite = 1;
            else
                _instance.sprite = 1 + (int) (_instance.spriteOffset % 4);
        }

        private static float Approach(float val, float target, float amount) {
            return (val > target ? Math.Max(val - amount, target) : Math.Min(val + amount, target));
        }

        private static void OnPlayerRender(On.Celeste.Player.orig_Render orig, Player self) {
            if (_instance == null) {
                return;
            }

            if (!_instance.PlayerInZone) {
                orig(self);
                return;
            }

            // Draw.Rect(self.Position - new Vector2(4f, 8f), 8f, 8f, Color.Red);
            GFX.Game[$"objects/StrawberryJam2021/classicZoneController/player0{_instance.sprite - 1}"]
                .Draw(self.Position, new Vector2(4f, 7f), Color.White,
                    self.Facing == Facings.Left ? Vector2.One : new Vector2(-1f, 1f));
        }
    }
}