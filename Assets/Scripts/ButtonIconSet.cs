using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ButtonIconSet", menuName = "Input/Button Icon Set")]
public class ButtonIconSet : ScriptableObject
{
    [System.Serializable]
    public class IconEntry
    {
        public string controlPath;  // exemple "<Gamepad>/buttonSouth"
        public Sprite icon;         // icône associée
    }

    public List<IconEntry> icons;

    public Sprite GetIcon(string path)
    {
        //Debug.Log($"Recherche de l'icône pour le path : {path}");
        foreach (var entry in icons)
        {
            if (path.Contains(entry.controlPath))
                return entry.icon;
        }
        return null;
    }
}
