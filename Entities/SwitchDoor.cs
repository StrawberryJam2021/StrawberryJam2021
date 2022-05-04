using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Linq;
using System.Reflection;


namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/SwitchDoor")]
    public class SwitchDoor : Batteries.BatteryGate {
        DynData<Batteries.BatteryGate> GateData;
        public SwitchDoor(Vector2 position, int size, bool vertical, int? openWith, bool closes, EntityID id) : base(position, size, vertical, openWith, closes, id) {
            GateData = new DynData<Batteries.BatteryGate>(this);
            Remove(GateData.Get<Sprite>("sprite"));
            Sprite sprite = StrawberryJam2021Module.SpriteBank.Create("switchDoor");
            Add(sprite);
            GateData["sprite"] = sprite;
        }
        public SwitchDoor(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, data.Height, data.Bool("vertical"), data.Int("switchId", -1), data.Bool("closes"), id) {
        }

        public override void Render() {
            base.Render();
        }

        public override void Update() {
            base.Update();
        }
        public override void Awake(Scene scene) {
            base.Awake(scene);
        }
    }
}
