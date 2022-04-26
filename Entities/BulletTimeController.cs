using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

[CustomEntity("SJ2021/BTController")]
public class BTController : Entity {
    float timerate = 1;
    string flag = "";
    bool active = false;

    public BTController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Tag = Tags.PauseUpdate;
        timerate = data.Float("speed");
        flag = data.Attr("flag");
    }

    public override void Update() {
        base.Update();

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player != null && !player.IsIntroState && !player.JustRespawned && player.Dashes > 0 && (string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag))) {
            if (Scene.Paused) {
                Engine.TimeRate = 1.0f;
            } else {
                Engine.TimeRate = timerate;
            }
            active = true;
        } else if (active) {
            Engine.TimeRate = 1.0f;
            active = false;
        }
    }
}
