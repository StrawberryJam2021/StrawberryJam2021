using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/RequireDashlessTrigger")]
    public class RequireDashlessTrigger : Trigger {
        private static char[] separators = { ',' };
        private List<Entity> trackedEntities = new List<Entity>();
        private bool removed;

        public string[] EntityNames;

        public RequireDashlessTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            EntityNames = data.Attr("entityNames", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();

            Add(new DashListener {
                OnDash = OnDash
            });
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            foreach (Entity entity in scene.Entities) {
                if (CollideCheck(entity)) {
                    if (EntityNames.Contains(entity.GetType().FullName) || EntityNames.Contains(entity.GetType().Name)) {
                        trackedEntities.Add(entity);
                    }
                }
            }

            Level level = SceneAs<Level>();

            if (level.Session.Dashes != 0 || !level.Session.StartedFromBeginning) {
                scene.Remove(trackedEntities);
                removed = true;
            }
        }

        private void OnDash(Vector2 dir) {
            if (!removed) {
                foreach (Entity entity in trackedEntities) {
                    entity.Add(new Coroutine(RemoveRoutine(entity), true));
                }
                removed = true;
            }
        }

        private IEnumerator RemoveRoutine(Entity entity) {
            entity.Collidable = false;
            foreach (Component com in entity.Components) {
                if (com is Sprite sprite) {
                    Tween shrinkTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.15f, true);
                    shrinkTween.OnUpdate = delegate (Tween t) {
                        sprite.Scale.X = MathHelper.Lerp(1f, 0f, t.Eased);
                        sprite.Scale.Y = MathHelper.Lerp(1f, 0f, t.Eased);
                    };
                    Add(shrinkTween);
                    break;
                }
            }
            yield return 0.15f;
            Audio.Play("event:/game/general/seed_poof", entity.Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            for (int i = 0; i < 6; i++) {
                float num = Calc.Random.NextFloat(6.2831855f);
                SceneAs<Level>().ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, entity.Position + Calc.AngleToVector(num, 4f), Vector2.Zero, num);
            }
            entity.RemoveSelf();
            yield break;
        }
    }
}