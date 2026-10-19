using UnityEngine;

/// Fonte de todo texto criado em código (Text legado). O relógio e o menu usam TMP com fonte própria.
/// O .ttf fica em Assets/Resources pra carregar por nome; sem ele cai na fonte padrão.
public static class UIFont
{
    static Font font;

    public static Font Get() =>
        font != null ? font : font = Resources.Load<Font>("OldNewspaperTypes") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
