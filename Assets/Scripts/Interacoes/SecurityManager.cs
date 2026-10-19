using System;
using System.Collections.Generic;

/// Nível de segurança por entrada (id string). Como InventoryHandler: estático, sem objeto na cena.
public static class SecurityManager
{
    static readonly Dictionary<string, int> levels = new Dictionary<string, int>();

    public static event Action<string, int> OnLevelChanged; // entrada, novo nível

    public static void Reset() => levels.Clear();

    public static int Level(string entrance) => levels.TryGetValue(entrance, out var l) ? l : 0;

    public static int Total()
    {
        int sum = 0;
        foreach (var l in levels.Values) sum += l;
        return sum;
    }

    public static void Add(string entrance, int amount = 1)
    {
        levels[entrance] = Level(entrance) + amount;
        OnLevelChanged?.Invoke(entrance, levels[entrance]);
    }
}
