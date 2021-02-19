using Microsoft.Xna.Framework;
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

        public ClassicZoneController() {
            Tag = Tags.Global | Tags.PauseUpdate;
            _instance = this;
        }

        public override void Update() {
            base.Update();
            SkipFrame = !SkipFrame;
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

            bool playerInZone = self.CollideCheck<ClassicZone>();
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

            // camera update
            var from = ((Level) Engine.Scene).Camera.Position;
            var target = self.CameraTarget;
            ((Level) Engine.Scene).Camera.Position = from + (target - from) * (1f - (float)Math.Pow(0.01f, Engine.DeltaTime));

            if (_instance.SkipFrame) {
                return;
            }

            // I roughly estimated that the pico8 speed is 7.5 b/s, speed gets up to 1 and madeline's speed is 11.25 b/s
            // 
            self.MoveH(self.Speed.X * (11.25f / 7.5f));
            self.MoveV(self.Speed.Y * (11.25f / 7.5f));

            var input = Input.MoveX;

            bool onGround = self.OnGround();

            var jump = Input.Jump;
            if (jump)
                _instance.jbuffer = 4;
            else if (_instance.jbuffer > 0)
                _instance.jbuffer--;

            var dash = Input.Dash;

            if (onGround) {
                _instance.grace = 6;
                if (_instance.djump < self.MaxDashes) {
                    // G.psfx(54);
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
                        // G.psfx(1);
                        _instance.jbuffer = 0;
                        _instance.grace = 0;
                        self.Speed.Y = -2;
                    } else {
                        // wall jump
                        int wallDir = /* (is_solid(-3, 0) ? -1 : (is_solid(3, 0) ? 1 : 0)) */ 0;
                        if (wallDir != 0) {
                            // G.psfx(2);
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

                    // G.psfx(3);
                    // G.freeze = 2;
                    // G.shake = 6;
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
                    // G.psfx(9);
                }
            }

            // animation
            // spr_off += 0.25f;
            // if (!onGround) {
            //     if (is_solid(input, 0))
            //         spr = 5;
            //     else
            //         spr = 3;
            // } else if (E.btn(G.k_down))
            //     spr = 6;
            // else if (E.btn(G.k_up))
            //     spr = 7;
            // else if (spd.X == 0 || (!E.btn(G.k_left) && !E.btn(G.k_right)))
            //     spr = 1;
            // else
            //     spr = 1 + spr_off % 4;
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
            Draw.Rect(self.Position - new Vector2(4f, 8f), 8f, 8f, Color.Red);
        }
    }
}