using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/ConditionalFlagTrigger")]
    class ConditionalFlagTrigger : Trigger {
        private string flag; //the flag to set when the trigger is set
        private string controllerFlag; //the flag that enables these triggers
        private bool priorState; //the prior state
        private bool flagValue; //value to set the flag to on entry
        private bool resetOnLeave; //whether or not to reset on leaving the trigger
        private bool isEnabled;

        public ConditionalFlagTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            flag = data.Attr("flag");
            flagValue = data.Bool("flagValue", true);
            resetOnLeave = data.Bool("resetOnLeave", true);
            controllerFlag = data.Attr("controllerFlag");
            isEnabled = true;
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if(!string.IsNullOrEmpty(controllerFlag))
                isEnabled = SceneAs<Level>().Session.GetFlag(controllerFlag);
            //if the trigger is enabled, change the flag state to set
            if (!string.IsNullOrEmpty(flag) && isEnabled) {
                priorState = SceneAs<Level>().Session.GetFlag(flag);
                SceneAs<Level>().Session.SetFlag(flag, flagValue);
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (!string.IsNullOrEmpty(controllerFlag))
                isEnabled = SceneAs<Level>().Session.GetFlag(controllerFlag);
            //if the trigger is enabled, change the flag state to it's prior state
            if (!string.IsNullOrEmpty(flag) && isEnabled && resetOnLeave) {
                SceneAs<Level>().Session.SetFlag(flag, priorState);
            }
        }

        public override void OnStay(Player player) {
            base.OnStay(player);
            if (!string.IsNullOrEmpty(controllerFlag) && SceneAs<Level>().Session.GetFlag(controllerFlag) != isEnabled) {
                isEnabled = SceneAs<Level>().Session.GetFlag(controllerFlag);
                //if the player is already inside the trigger, we need to update it
                if (isEnabled)
                    Enable();
                else
                    Disable();
            }
        }

        public void Disable() {
            isEnabled = false;
            if (!string.IsNullOrEmpty(flag) && resetOnLeave)
                SceneAs<Level>().Session.SetFlag(flag, priorState);
        }

        public void Enable() {
            isEnabled = true;
            if (!string.IsNullOrEmpty(flag)) {
                priorState = SceneAs<Level>().Session.GetFlag(flag);
                SceneAs<Level>().Session.SetFlag(flag, flagValue);
            }
        }
    }
}