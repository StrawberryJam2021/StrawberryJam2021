using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/RequireDashlessTrigger")]
    class RequireDashlessTrigger : Trigger {
        private static char[] separators = { ',' };

        public string[] EntityNames;

        public RequireDashlessTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            EntityNames = data.Attr("entityNames", "").Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            Level level = SceneAs<Level>();

            if (level.Session.Dashes != 0 || !level.Session.StartedFromBeginning) {
                foreach (Entity entity in scene.Entities) {
                    if (CollideCheck(entity)) {
                        if (EntityNames.Contains(entity.GetType().FullName) || EntityNames.Contains(entity.GetType().Name)) {
                            scene.Remove(entity);
                        }
                    }
                }
            }
        }
    }
}