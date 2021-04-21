using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

[CustomEntity("SJ2021/VariantToggleController")] 
[Tracked]
class ConditionalFlagTrigger : Trigger {
    string flag;
    bool resetOnLeave;
    bool value;

    public ConditionalFlagTrigger(EntityData data, Vector2 offset) 
        : base(data, data.Position + offset) {
        flag = data.Attr("flag");
        resetOnLeave = data.Bool("resetOnLeave");
        value = data.Bool("value");
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
    }

    public override void OnLeave(Player player) {
        base.OnLeave(player);
    }
}