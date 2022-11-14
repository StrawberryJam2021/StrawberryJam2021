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
        private bool onlyOnce;

        private bool triggered = false;

        public MeteorShowerCountTrigger(EntityData data, Vector2 offset) : base (data, offset) {
            setAmnt = data.Int("NumberOfMeteors", 1);
            onlyOnce = data.Bool("only_once", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            if (triggered && onlyOnce)
                return;

            Level level = Scene as Level;
            foreach (MeteorShower meteorShower in level.Background.GetEach<MeteorShower>()) {
                meteorShower.MeteorCount = setAmnt;
                triggered = true;
            }
        }
    }
}
