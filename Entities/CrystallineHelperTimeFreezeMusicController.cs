using Monocle;
using Microsoft.Xna.Framework;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.StrawberryJam2021.Entities {

    [CustomEntity("SJ2021/TimeFreezeMusicController")]
    public class CrystallineHelperTimeFreezeMusicController : Entity {
        
        public static FieldInfo crystallineHelper_TimeCrystal_stopStage;

        public float paramOff, paramOn;
        public string paramName;
        public bool prevValue;

        public CrystallineHelperTimeFreezeMusicController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            paramName = data.Attr("musicParam");
            paramOff = data.Float("paramOff");
            paramOn = data.Float("paramOn");
        }

        public override void Update() {
            base.Update();
            bool value = (int)crystallineHelper_TimeCrystal_stopStage.GetValue(null) == 1;
            if(value != prevValue) {
                AudioState audio = SceneAs<Level>().Session.Audio;
                audio.Music.Param(paramName, value ? paramOn : paramOff);
                audio.Apply();
            }
            prevValue = value;
        }
    }
}
