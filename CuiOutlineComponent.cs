﻿// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiOutlineComponent
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Newtonsoft.Json;

namespace Oxide.Game.Rust.Cui
{
    public class CuiOutlineComponent : ICuiComponent, ICuiColor
    {
        public string Type => "UnityEngine.UI.Outline";

        public string Color { get; set; }

        [JsonProperty("distance")]
        public string Distance { get; set; }

        [JsonProperty("useGraphicAlpha")]
        public bool UseGraphicAlpha { get; set; }
    }
}
