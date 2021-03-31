using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/TheoRespawn")]
    [Tracked]
    class TheoRespawn : Entity {

        public TheoRespawn(EntityData data, Vector2 offset) : base(data.Position + offset) {

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Level level = SceneAs<Level>();
            float thisDist = Vector2.Distance(Position, level.Session.RespawnPoint.Value);

            foreach(TheoRespawn respawn in scene.Tracker.GetEntities<TheoRespawn>()) {
                float dist = Vector2.Distance(respawn.Position, level.Session.RespawnPoint.Value);
                // If another TheoRespawn is closer than this one, remove this
                if(dist < thisDist) {
                    RemoveSelf();
                    return;
                }
            }

            scene.Add(new TheoCrystal(Position));
        }

    }
}
