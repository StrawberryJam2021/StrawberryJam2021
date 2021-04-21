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
        private string flag;
        private bool isFlagged;
        private List<KeyValuePair<ExtendedVariants.Module.ExtendedVariantsModule.Variant, int>> variantValues;

        public VariantToggleController(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Attr("flag"), data.Attr("variantList")) {
        }

        public VariantToggleController(Vector2 position, string flagName, string variantList) 
            : base(position) {
            flag = flagName;
            variantValues = ParseParameterList(variantList);
        }

        public override void Update() {
            base.Update();
            UpdateFlag();
        }

        public override void Awake(Scene scene) {
            isFlagged = false;
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
            if (string.IsNullOrEmpty(flag) || isFlagged == SceneAs<Level>().Session.GetFlag(flag))
                return; //if we have no flag or it hasn't changed, skip updating the variant
            isFlagged = SceneAs<Level>().Session.GetFlag(flag);
            UpdateVariants();
        }

        static private List<KeyValuePair<ExtendedVariantsModule.Variant, int>> ParseParameterList(string list) {
            List<KeyValuePair<ExtendedVariantsModule.Variant, int>> variantList = new List<KeyValuePair<ExtendedVariantsModule.Variant, int>>();
            if (String.IsNullOrEmpty(list))
                return variantList;
            string[] keyValueList = list.Split(',');
            foreach(string keyValue in keyValueList) {
                string[] variantKeyValue = keyValue.Split(':');
                if(variantKeyValue.Length >= 2) {
                    ExtendedVariantsModule.Variant variant = (ExtendedVariantsModule.Variant) Enum.Parse(typeof(ExtendedVariantsModule.Variant), variantKeyValue[0]);
                    int value = int.Parse(variantKeyValue[1]);
                    variantList.Add(new KeyValuePair<ExtendedVariantsModule.Variant, int>(variant, value));
                }
            }

            return variantList;
        }
    }
}
