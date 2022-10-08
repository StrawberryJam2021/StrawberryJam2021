using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/SolarElevator")]
    public class SolarElevator : Solid {
        private class Background : Entity {
            private readonly SolarElevator elevator;
            private readonly MTexture rail = GFX.Game["objects/StrawberryJam2021/solarElevator/rails"];

            public Background(SolarElevator elevator) {
                Depth = Depths.BGDecals;
                this.elevator = elevator;
            }

            public override void Render() {
                for (int y = 0; y < elevator.Distance + 60; y += rail.Height)
                    rail.DrawJustified(new(elevator.X, elevator.StartY - y), new(0.5f, 1.0f));

                GFX.Game["objects/StrawberryJam2021/solarElevator/elevatorback"]
                    .DrawJustified(elevator.Position + Vector2.UnitY * 10, new(0.5f, 1.0f));
            }
        }

        private readonly ColliderList OpenCollider = new(
            new Hitbox(3, 16, -24, -54),
            new Hitbox(3, 16, 21, -54),
            new Hitbox(48, 8, -24, -62),
            new Hitbox(48, 5, -24, 0)
        );
        private readonly ColliderList ClosedCollider = new(
            new Hitbox(3, 54, -24, -54),
            new Hitbox(3, 54, 21, -54),
            new Hitbox(48, 8, -24, -62),
            new Hitbox(48, 5, -24, 0)
        );

        private readonly TalkComponent interaction;
        private readonly SoundSource moveSfx;

        public readonly float StartY;
        public readonly float Distance;
        private readonly float time;

        private bool enabled = false;
        private bool atGroundFloor = true;

        private Background bg;

        private readonly DynamicData data;

        public SolarElevator(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int("distance", 128), data.Float("time", 3.0f)) { }

        public SolarElevator(Vector2 position, int distance, float time)
            : base(position, 56, 80, safe: true) {
            Depth = Depths.FGDecals;
            SurfaceSoundIndex = SurfaceIndex.MoonCafe;

            StartY = Y;
            Distance = distance;
            this.time = time;

            UpdateCollider(open: true);

            Add(moveSfx = new());

            Add(interaction = new TalkComponent(new Rectangle(-12, -8, 24, 8), Vector2.UnitY * -16, Activate));

            Image img = new(GFX.Game["objects/StrawberryJam2021/solarElevator/elevator"]);
            img.JustifyOrigin(0.5f, 1.0f);
            img.Position.Y = 10;
            Add(img);

            data = new DynamicData(typeof(Solid), this);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            Player player = scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            float distanceFromStart = Vector2.DistanceSquared(player.Center, Position);
            float distanceFromEnd = Vector2.DistanceSquared(player.Center, Position - Vector2.UnitY * Distance);
            if (distanceFromStart > distanceFromEnd) {
                Y -= Distance;
                atGroundFloor = false;
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(bg = new Background(this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Remove(bg);
        }

        private void UpdateCollider(bool open) {
            Collider = open ? OpenCollider : ClosedCollider;
        }

        private void Activate(Player player) {
            if (!enabled)
                Add(new Coroutine(Sequence()));
        }

        private IEnumerator Sequence() {
            enabled = true;
            interaction.Enabled = false;
            Audio.Play(SFX.game_10_ppt_mouseclick, Position);
            UpdateCollider(open: false);

            yield return 1f;

            moveSfx.Play(CustomSoundEffects.game_solar_elevator_elevate);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.15f);

            float start = Y;
            float end = atGroundFloor ? (start - Distance) : (start + Distance);
            float t = 0.0f;
            while (t < time) {
                float percent = t / time;
                float at = start + (end - start) * percent;
                MoveToY(at);

                t = Calc.Approach(t, time, Engine.DeltaTime);
                yield return null;
            }

            MoveToY(end);
            moveSfx.Play(CustomSoundEffects.game_solar_elevator_halt);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.2f);

            enabled = false;
            interaction.Enabled = true;
            atGroundFloor = !atGroundFloor;
            UpdateCollider(open: true);
        }

        // Fix wrong collision resolution against collider lists.
        // In this case, the entity only moves vertically, so let's just change MoveVExact only.
        // Copied from https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/ConnectedStuff/ConnectedSolid.cs#L396.
        // Might not behave well with Gravity Helper (inverted actors).
        public override void MoveVExact(int move) {
            if (Collider is not ColliderList) {
                base.MoveVExact(move);
                return;
            }

            Collider[] colliders = (Collider as ColliderList).colliders;

            GetRiders();
            HashSet<Actor> riders = data.Get<HashSet<Actor>>("riders");

            Y += move;
            MoveStaticMovers(Vector2.UnitY * move);

            if (Collidable) {
                foreach (Actor entity in Scene.Tracker.GetEntities<Actor>()) {
                    if (entity.AllowPushing) {
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;
                        if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                            foreach (Hitbox hitbox in colliders) {
                                if (!hitbox.Collide(entity))
                                    continue;

                                float top = Y + hitbox.Top;
                                float bottom = Y + hitbox.Bottom;
                                int moveV = (move <= 0) ? (int) (top - entity.Bottom) : (int) (bottom - entity.Top);

                                Collidable = false;
                                entity.MoveVExact(moveV, entity.SquishCallback, this);
                                entity.LiftSpeed = LiftSpeed;
                                Collidable = true;
                            }
                        } else if (riders.Contains(entity)) {
                            Collidable = false;
                            if (entity.TreatNaive)
                                entity.NaiveMove(Vector2.UnitY * move);
                            else
                                entity.MoveVExact(move);
                            entity.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        }
                        entity.Collidable = collidable;
                    }
                }
            }
            riders.Clear();
        }
    }
}
