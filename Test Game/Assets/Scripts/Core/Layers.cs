using UnityEngine;
using System.Collections.Generic;

public static class Layers 
{
    // Warstwy, o które trafia pocisk
    public static List<int> ProjectileHitLayers { get; private set; } = new List<int>() { LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Wall") };

}
