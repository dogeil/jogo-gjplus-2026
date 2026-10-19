using UnityEngine;

public enum Recurso { Madeira, Prego, Arame, Parafuso }

public class InventoryHandler : MonoBehaviour
{
    static readonly int[] quantidades = new int[System.Enum.GetValues(typeof(Recurso)).Length];

    // Ordem = enum Recurso (Madeira, Prego, Arame, Parafuso)
    [SerializeField] int[] limites = { 99, 99, 99, 99 };
    static int[] limitesAtivos = { 99, 99, 99, 99 };

    void Awake() => limitesAtivos = limites;
    void OnValidate() => limitesAtivos = limites;

    public static int Limite(Recurso recurso)
    {
        int i = (int)recurso;
        return i < limitesAtivos.Length ? limitesAtivos[i] : 99;
    }

    public static void Adicionar(Recurso recurso, int qtd = 1)
    {
        quantidades[(int)recurso] = Mathf.Min(quantidades[(int)recurso] + qtd, Limite(recurso));
        Debug.Log(recurso + ": " + quantidades[(int)recurso]);
    }

    public static void Reset() => System.Array.Clear(quantidades, 0, quantidades.Length);

    public static int Quantidade(Recurso recurso) => quantidades[(int)recurso];

    public static void Remover(Recurso recurso, int qtd) => quantidades[(int)recurso] -= qtd;
}
