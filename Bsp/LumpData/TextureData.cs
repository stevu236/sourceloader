﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Csgo.Bsp.LumpData
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TextureData
    {
        public Vector3 Reflectivity;
        public int NameStringTableId;
        public int Width;
        public int Height;
        public int ViewWidth;
        public int viewHeight;
    }
}
