using UnityEngine;

public class WaitingSlotCell : MonoBehaviour
{ 
    public PassengerController CurrentPassanger;

    public bool IsOccupied => CurrentPassanger != null;
}
 