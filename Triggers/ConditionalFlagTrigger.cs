using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.StrawberryJam2021.Triggers {

    [CustomEntity("SJ2021/ConditionalFlagTrigger")]
    [Tracked]
    public class ConditionalFlagTrigger : Trigger {
        private string flag; //the flag to set when the trigger is set
        private string controllerFlag; //the flag that enables these triggers
        private bool priorState; //the prior state
        private bool flagValue; //value to set the flag to on entry
        private bool revertOnLeave; //whether or not to reset on leaving the trigger

        public ConditionalFlagTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            flag = data.Attr("flag");
            flagValue = data.Bool("flagValue", true);
            revertOnLeave = data.Bool("revertOnLeave", true);
            controllerFlag = data.Attr("controllerFlag");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            //if the trigger is enabled, change the flag state to set
            if (!string.IsNullOrEmpty(flag)) {
                ConditionalFlagTrigger active = GetActiveTrigger();
                if (active != null)
                    priorState = active.priorState;
                else
                    priorState = SceneAs<Level>().Session.GetFlag(flag);
                SceneAs<Level>().Session.SetFlag(flag, flagValue);
            }
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            //if the trigger is enabled, change the flag state to it's prior state
            if (!string.IsNullOrEmpty(flag) && revertOnLeave) {

                if(!FoundActiveTrigger())
                    SceneAs<Level>().Session.SetFlag(flag, priorState);
            }
        }

        private bool FoundActiveTrigger() {
            bool found = false;
            List<Entity> tracked = Scene.Tracker.GetEntities<ConditionalFlagTrigger>();
            foreach (ConditionalFlagTrigger current in tracked) {
                if (current != this && current.Triggered && current.flag == flag)
                    found = true;
            }
            return found;
        }

        private ConditionalFlagTrigger GetActiveTrigger() {
            List<Entity> tracked = Scene.Tracker.GetEntities<ConditionalFlagTrigger>();
            foreach (ConditionalFlagTrigger current in tracked) {
                if (current != this && current.Triggered && current.flag == flag)
                    return current;
            }
            return null;
        }

        public override void Update() {
            base.Update();
            if (!string.IsNullOrEmpty(controllerFlag))
                Collidable = SceneAs<Level>().Session.GetFlag(controllerFlag);
        }
    }
}