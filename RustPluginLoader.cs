﻿// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.RustPluginLoader
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.Rust
{
    public class RustPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new Type[1]
        {
      typeof (RustCore)
        };
    }
}
