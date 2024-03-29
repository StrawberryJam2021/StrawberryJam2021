﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Batteries;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [TrackedAs(typeof(BatteryGate))]
    [CustomEntity("SJ2021/SwitchDoor")]
    public class SwitchDoor : BatteryGate {
        private DynamicData gateData;
        private Sprite sprite;

        public SwitchDoor(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id) {
            gateData = new DynamicData(typeof(BatteryGate), this);
            Remove(gateData.Get<Sprite>("sprite"));
            Add(sprite = StrawberryJam2021Module.SpriteBank.Create("switchDoor"));
            gateData.Set("sprite", sprite);

            if (gateData.Get<bool>("vertical")) {
                sprite.X = (Collider.Width - 1f) / 2f;
            } else {
                sprite.Rotation = 1.5f * MathHelper.Pi;
                sprite.Y = (Collider.Height + 1f) / 2f;
            }

            sprite.Play("idle", false, false);
        }
    }
}
