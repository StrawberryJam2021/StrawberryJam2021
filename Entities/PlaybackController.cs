using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;


namespace Celeste.Mod.StrawberryJam2021.Entities {
    internal class PlaybackController : Entity {
        public PlaybackController() {
            Tag = Tags.Global | Tags.Persistent;
        }
    }
}
