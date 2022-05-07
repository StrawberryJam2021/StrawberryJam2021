﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.StrawberryJam2021.StylegroundMasks {
    public static class MaskHooks {
        public static void Load() {
            BloomMask.Load();
            StylegroundMask.Load();
            LightingMask.Load();
            ColorGradeMask.Load();
        }

        public static void Unload() {
            BloomMask.UnLoad();
            StylegroundMask.UnLoad();
            LightingMask.UnLoad();
            ColorGradeMask.UnLoad();
        }
    }
}
