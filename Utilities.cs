using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using Monocle;
using MonoMod.Utils;
using Mono.Cecil.Cil;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.StrawberryJam2021 {
    public static class Utilities {

        public static void LoadContent() {
            //Loads in XNA Colors, fastest option is actually a manual dictionary but in the grand scheme of things it makes little difference when called once.
            foreach(PropertyInfo prop in typeof(Color).GetProperties()) {
                if(prop.PropertyType == typeof(Color) && !ColorHelper.ContainsKey(prop.Name)) {
                    ColorHelper[prop.Name] = (Color)prop.GetValue(null);
                }
            }
        }

        public static Dictionary<string, Color> ColorHelper = new Dictionary<string, Color>(); //Optimized for Caching
        private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);
        public static int AddState(this StateMachine machine, Func<Player, int> onUpdate, Func<Player, IEnumerator> coroutine = null, Action<Player> begin = null, Action<Player> end = null) {
            int nextIndex = Expand(machine);
            // And now we add the new functions
            machine.SetCallbacks(nextIndex, () => onUpdate(machine.Entity as Player), coroutine is null ? null : () => coroutine(machine.Entity as Player), () => begin(machine.Entity as Player), () => end(machine.Entity as Player));
            return nextIndex;
        }

        public static int Expand(this StateMachine machine) {
            Action[] begins = (Action[]) StateMachine_begins.GetValue(machine);
            Func<int>[] updates = (Func<int>[]) StateMachine_updates.GetValue(machine);
            Action[] ends = (Action[]) StateMachine_ends.GetValue(machine);
            Func<IEnumerator>[] coroutines = (Func<IEnumerator>[]) StateMachine_coroutines.GetValue(machine);
            int nextIndex = begins.Length;
            // Now let's expand the arrays
            Array.Resize(ref begins, begins.Length + 1);
            Array.Resize(ref updates, begins.Length + 1);
            Array.Resize(ref ends, begins.Length + 1);
            Array.Resize(ref coroutines, coroutines.Length + 1);
            // Store the resized arrays back into the machine
            StateMachine_begins.SetValue(machine, begins);
            StateMachine_updates.SetValue(machine, updates);
            StateMachine_ends.SetValue(machine, ends);
            StateMachine_coroutines.SetValue(machine, coroutines);

            return nextIndex;
        }

        //Code borrowed from VivHelper, permanent
        public static Color HexOrNameToColor(string hex) {
            if (ColorHelper.ContainsKey(hex))
                return ColorHelper[hex];
            string hexplus = hex.Trim('#');
            if (hexplus.StartsWith("0x"))
                hexplus = hexplus.Substring(2);
            int result;
            if (hexplus.Length == 6 && int.TryParse(hexplus, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result)) {
                return Calc.HexToColor(result);
            } else if (hexplus.Length == 8 && hexplus.Substring(0, 2) == "00" && int.TryParse(hexplus.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int _)) {
                return Color.Transparent;
            } else if (int.TryParse(hexplus, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result)) {
                return AdvHexToColor(result);
            }
            return Color.Transparent;
        }

        public static Color AdvHexToColor(int hex) {
            Color result = default(Color);
            result.A = (byte) (hex >> 24);
            result.R = (byte) (hex >> 16);
            result.G = (byte) (hex >> 8);
            result.B = (byte) hex;
            return result;
        }


    }
}
