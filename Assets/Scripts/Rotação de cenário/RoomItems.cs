using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomItems : MonoBehaviour
{
    [Serializable]
    public class ItemSave
    {
        public string prefabId;
        public int surface;     // 0..3 paredes, 4 chão, 5 teto
        public Vector3 localPos;
    }

    [SerializeField] RoomView room;
    [SerializeField] List<GameObject> prefabs;           // prefabId = nome do prefab
    [SerializeField] List<ItemSave> savedItems = new();

    void Start() => Load(savedItems);

    public List<ItemSave> Save()
    {
        var list = new List<ItemSave>();
        foreach (var item in FindObjectsByType<DraggableObject>(FindObjectsSortMode.None))
        {
            int surface = room.IndexOfSurface(item.transform.parent);
            if (surface < 0) continue; // sendo arrastado

            list.Add(new ItemSave
            {
                prefabId = item.name.Replace("(Clone)", "").Trim(),
                surface = surface,
                localPos = item.transform.localPosition
            });
        }
        return list;
    }

    public void Load(List<ItemSave> data)
    {
        foreach (var d in data)
        {
            GameObject prefab = prefabs.Find(p => p.name == d.prefabId);
            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, room.GetSurface(d.surface));
            go.transform.localPosition = d.localPos;
        }
    }
}