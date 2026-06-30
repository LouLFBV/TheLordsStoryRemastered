using UnityEngine;

public class Harvestable : WorldDisappearOnCollected
{
    public Ressource[] harvestableItems;

    [Header("Options")]
    public Tool tool;
    public bool disableKinematicOnHarvest;
    public float destroyDelay;
}


[System.Serializable]
public class Ressource
{
    public ItemData itemData;

    [Range(0, 100)]
    public int dropChance;
}

public enum Tool
{   
    Pickaxe,
    SuperPickaxe,
    Axe
}