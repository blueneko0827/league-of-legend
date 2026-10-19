using UnityEngine;
namespace MidLane
{
    public static class LeagueItemCatalog
    {
        static ItemCatalogData data;
        public static ItemDefinition[] Items
        {
            get
            {
                if (data == null) data = JsonUtility.FromJson<ItemCatalogData>(Resources.Load<TextAsset>("LeagueData/Items").text);
                return data.items;
            }
        }
    }
}
