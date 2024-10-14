using UnityEngine;

public abstract class Item : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;

    public abstract void Use(Unit user);
}