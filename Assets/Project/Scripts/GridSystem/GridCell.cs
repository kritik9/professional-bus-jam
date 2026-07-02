using UnityEngine;

public class GridCell : MonoBehaviour
{ 
    public int x;
    public int z;

    public PassengerController CurrentPassanger;  
    
    public bool IsOccupied => CurrentPassanger != null;
}
 
