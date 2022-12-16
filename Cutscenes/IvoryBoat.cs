using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.StrawberryJam2021.Cutscenes {
    [CustomEntity("SJ2021/IvoryBoat")]
    public class IvoryBoat : JumpThru {
        private const float StartSpeed = 60f;
        private const float EndSpeed = 16f;

        private readonly SineWave sine;
        private readonly Image image;
        private readonly Vector2 start;
        private readonly Vector2 end;
        private readonly string flag;
        private Coroutine boatRoutine;
        private Coroutine playerRoutine;
        private bool activated;

        public IvoryBoat(EntityData data, Vector2 offset)
            : base(data.Position + offset, width: 48, safe: true) {
            start = Position;
            end = data.NodesWithPosition(offset)[1];
            SurfaceSoundIndex = SurfaceIndex.Wood;
            flag = data.Attr("flag");
            Add(sine = new SineWave(0.16f));
            Add(image = new Image(GFX.Game["objects/StrawberryJam2021/ivoryBoat/boat"]));
            image.Position = new Vector2(-8f, -9f);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (activated = SceneAs<Level>().Session.GetFlag(flag)) {
                Position.X = end.X;
            }
        }

        public override void Update() {
            base.Update();
            MoveToY(start.Y + sine.Value);
            if (!activated && GetPlayerRider() is Player player) {
                activated = true;
                (Scene as Level).StartCutscene((level) => SkipCutscene(level, player), fadeInOnSkip: false);
                Add(boatRoutine = new Coroutine(BoatRoutine()));
                Add(playerRoutine = new Coroutine(PlayerRoutine(player)));
            }
        }

        private IEnumerator BoatRoutine() {
            float distance = end.X - start.X;
            while (end.X - Position.X > 0.5f * distance) {
                MoveH(StartSpeed * Engine.DeltaTime);
                yield return null;
            }

            float speed = StartSpeed;
            while (Position.X != end.X) {
                speed = Calc.Approach(speed, EndSpeed, 7f * Engine.DeltaTime);
                MoveH(speed * Engine.DeltaTime);
                yield return null;
            }
        }

        private IEnumerator PlayerRoutine(Player player) {
            Level level = player.SceneAs<Level>();

            player.StateMachine.State = Player.StDummy;
            player.ForceCameraUpdate = true;

            while (!boatRoutine?.Finished ?? false) {
                if (player.CollideCheck<JumpThru>(player.Position + Vector2.UnitX * 8f)) {
                    player.Jump();
                    player.AutoJump = true;
                    player.AutoJumpTimer = 1.8f;
                    yield return player.DummyRunTo(end.X + Width / 2f);
                    break;
                }

                yield return null;
            }

            player.StateMachine.State = Player.StNormal;
            player.ForceCameraUpdate = false;
            level.Session.SetFlag(flag, true);
            level.EndCutscene();
        }

        public void SkipCutscene(Level level, Player player) {
            boatRoutine?.Cancel();
            playerRoutine?.Cancel();
            player.StateMachine.State = Player.StNormal;
            player.ForceCameraUpdate = false;
            player.AutoJump = false;
            level.Session.SetFlag(flag, true);
            Position.X = end.X;
            player.Position = new Vector2(CenterX, Top);
            level.Camera.Position = level.GetFullCameraTargetAt(player, player.Position);
        }
    }
}
