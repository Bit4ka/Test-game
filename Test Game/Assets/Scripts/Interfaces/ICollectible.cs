using UnityEngine;

public interface ICollectible
{
    /// <summary>
    /// Wywo³ywana przez podnosz¹cego przedmiot
    /// </summary>
    /// <param name="collector">Podnosz¹cy przedmiot</param>
    public void Collect(Collector collector);
}
