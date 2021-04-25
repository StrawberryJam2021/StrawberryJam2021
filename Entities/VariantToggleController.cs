using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using ExtendedVariants.Module;

namespace Celeste.Mod.StrawberryJam2021.Entities {
    [CustomEntity("SJ2021/VariantToggleController")]
    class VariantToggleController : Entity {
        private string flag; //the flag that controls the variants
        private bool isFlagged; //last flag state
        private bool defaultValue;
        private bool forceUpdate;
        private Dictionary<ExtendedVariantsModule.Variant, int> variantValues;

        public VariantToggleController(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Attr("flag"), data.Attr("variantList"), data.Bool("defaultValue", true)) {
        }

        public VariantToggleController(Vector2 position, string flagName, string variantList, bool defValue) 
            : base(position) {
            flag = flagName;
            defaultValue = defValue;
            variantValues = ParseParameterList(variantList);
        }

        public override void Update() {
            base.Update();
            UpdateFlag();
        }

        public override void Awake(Scene scene) {
            isFlagged = false;
            if (!string.IsNullOrEmpty(flag)) {
                SceneAs<Level>().Session.SetFlag(flag, defaultValue);
                forceUpdate = true;
            }
        }

        private void UpdateVariants() {
            if (isFlagged) {
                foreach (KeyValuePair<ExtendedVariantsModule.Variant, int> variant in variantValues) {
                    ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(variant.Key, variant.Value, false);
                }
            } 
            else {
                foreach (KeyValuePair<ExtendedVariantsModule.Variant, int> variant in variantValues) {
                    int defaultValue = ExtendedVariants.ExtendedVariantTrigger.GetDefaultValueForVariant(variant.Key);
                    ExtendedVariantsModule.Instance.TriggerManager.OnEnteredInTrigger(variant.Key, defaultValue, false);
                }
            }
        }

        private void UpdateFlag() {
            if (string.IsNullOrEmpty(flag) || (isFlagged == SceneAs<Level>().Session.GetFlag(flag) && !forceUpdate))
                return; //if we have no flag or it hasn't changed, skip updating the variant
            //we only want to clear forceUpdate if an update wasn't already ganna happen
            if (forceUpdate && isFlagged == SceneAs<Level>().Session.GetFlag(flag))
                forceUpdate = false;
            isFlagged = SceneAs<Level>().Session.GetFlag(flag);
            UpdateVariants();
        }

        static private Dictionary<ExtendedVariantsModule.Variant, int> ParseParameterList(string list) {
            Dictionary<ExtendedVariantsModule.Variant, int> variantList = new Dictionary<ExtendedVariantsModule.Variant, int>();
            if (string.IsNullOrEmpty(list))
                return variantList;
            string[] keyValueList = list.Split(',');
            //comma separated list of Variant:Value pairs
            foreach(string keyValue in keyValueList) {
                string[] variantKeyValue = keyValue.Split(':');
                if(variantKeyValue.Length >= 2) {
                    ExtendedVariantsModule.Variant variant = (ExtendedVariantsModule.Variant) Enum.Parse(typeof(ExtendedVariantsModule.Variant), variantKeyValue[0]);
                    int value = int.Parse(variantKeyValue[1]);
                    variantList[variant] = value;
                }
            }

            return variantList;
        }
    }
}
