﻿// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.Cui.CuiButton
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

namespace Oxide.Game.Rust.Cui
{
    public class CuiButton
    {
        public CuiButtonComponent Button { get; } = new CuiButtonComponent();

        public CuiRectTransformComponent RectTransform { get; } = new CuiRectTransformComponent();

        public CuiTextComponent Text { get; } = new CuiTextComponent();

        public float FadeOut { get; set; }
    }
}
