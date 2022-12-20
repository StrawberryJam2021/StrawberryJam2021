using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Monocle;
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

        public static Dictionary<string, Color> ColorHelper = new Dictionary<string, Color>(); //Optimized with Caching
        public static int AddState(this StateMachine machine, Func<Player, int> onUpdate, Func<Player, IEnumerator> coroutine = null, Action<Player> begin = null, Action<Player> end = null) {
            int nextIndex = Expand(machine);
            // And now we add the new functions
            machine.SetCallbacks(nextIndex, () => onUpdate(machine.Entity as Player), coroutine is null ? null : () => coroutine(machine.Entity as Player), () => begin(machine.Entity as Player), () => end(machine.Entity as Player));
            return nextIndex;
        }

        public static int Expand(this StateMachine machine) {
            int nextIndex = machine.begins.Length;
            // Now let's expand the arrays
            Array.Resize(ref machine.begins, nextIndex + 1);
            Array.Resize(ref machine.updates, nextIndex + 1);
            Array.Resize(ref machine.ends, nextIndex + 1);
            Array.Resize(ref machine.coroutines, machine.coroutines.Length + 1);

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

        public static bool IsInBounds(this Camera self, Entity entity, float extend = 32f) =>
            entity.Right >= self.Left - extend &&
            entity.Left <= self.Right + extend &&
            entity.Bottom >= self.Top - extend &&
            entity.Top <= self.Bottom + extend;

        public static bool Contains(this Camera self, Vector2 point, float extend = 32f) =>
            point.X >= self.Left - extend &&
            point.X <= self.Right + extend &&
            point.Y >= self.Top - extend &&
            point.Y <= self.Bottom + extend;

        public static bool IsRectangleVisible(float x, float y, float w, float h) {
            const float lenience = 4f;
            Camera camera = (Engine.Scene as Level)?.Camera;
            if (camera is null) {
                return true;
            }

            return x + w >= camera.Left - lenience
                && x <= camera.Right + lenience
                && y + h >= camera.Top - lenience
                && y <= camera.Bottom + 180f + lenience;
        }
    }
}
