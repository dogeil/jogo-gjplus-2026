using UnityEngine;

public enum Recurso { Madeira, Prego, Metal, Arame }

public class InventoryHandler : MonoBehaviour
{
    static readonly int[] quantidades = new int[System.Enum.GetValues(typeof(Recurso)).Length];

    public static void Adicionar(Recurso recurso, int qtd = 1)
    {
        quantidades[(int)recurso] += qtd;
        Debug.Log(recurso + ": " + quantidades[(int)recurso]);
    }

    public static int Quantidade(Recurso recurso) => quantidades[(int)recurso];

    // ponytail: contador debug via OnGUI, trocar por UI/TextMeshPro quando tiver HUD
    void OnGUI()
    {
        GUILayout.Label($"Madeira: {Quantidade(Recurso.Madeira)}  Prego: {Quantidade(Recurso.Prego)}  Metal: {Quantidade(Recurso.Metal)}  Arame: {Quantidade(Recurso.Arame)}");
    }
}
