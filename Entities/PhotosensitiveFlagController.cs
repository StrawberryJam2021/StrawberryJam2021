using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/PhotosensitiveFlagController")]
    public class PhotosensitiveFlagController : Entity {

        public readonly string FlagName;

        public PhotosensitiveFlagController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            FlagName = data.Attr("flag");
        }

        public void UpdateFlag() {
            var session = SceneAs<Level>().Session;

            if (FlagName.Length > 0 && session.GetFlag(FlagName) != Settings.Instance.DisableFlashes) {
                session.SetFlag(FlagName, Settings.Instance.DisableFlashes);
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            UpdateFlag();
        }

        public override void Update() {
            base.Update();

            UpdateFlag();
        }
    }
}
