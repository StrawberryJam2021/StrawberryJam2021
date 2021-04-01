using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [Tracked]
    [CustomEntity("SJ2021/UFO")]
    public class UFO : FlingBird {
        public UFO(Vector2[] nodes, bool skippable) : base(nodes, skippable) {

        }
        public UFO(EntityData data, Vector2 levelOffset) : this(data.NodesWithPosition(levelOffset), data.Bool("waiting")) {

        }
    }
}
