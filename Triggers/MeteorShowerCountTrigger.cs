using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.StrawberryJam2021.Effects;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.StrawberryJam2021.Triggers {
    [CustomEntity("SJ2021/MeteorShowerCountTrigger")]
    public class MeteorShowerCountTrigger : Trigger {
        private int setAmnt;

        public MeteorShowerCountTrigger(EntityData data, Vector2 offset) : base (data, offset) {
            setAmnt = data.Int("NumberOfMeteors", 1);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            Level level = Scene as Level;
            foreach (MeteorShower meteorShower in level.Background.GetEach<MeteorShower>()) {
                meteorShower.MeteorCount = setAmnt;
            }
        }
    }
}
