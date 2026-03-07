using UnityEngine;
using System.Collections.Generic;

public class Layers : MonoBehaviour
{
    // Warstwy, o które trafia pocisk
    public static List<int> ProjectileHitLayers { get; private set; }

    void Start()
    {
        ProjectileHitLayers = new List<int>() { LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Wall") };
        DontDestroyOnLoad(gameObject);
    }

    
}
