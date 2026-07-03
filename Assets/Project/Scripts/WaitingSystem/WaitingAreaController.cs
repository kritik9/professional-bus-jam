using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameEnums;

public class WaitingAreaController : MonoBehaviour
{
    

    private List<WaitingSlotCell> cells = new();
    private Queue<PassengerController> addQueue = new();

    private bool isProcessing = false;
     

    private void Awake()
    {
        cells.Clear();
        cells.AddRange(GetComponentsInChildren<WaitingSlotCell>());
    }
     
    public bool HasSpace()
    {
        foreach (var cell in cells)
        {
            if (!cell.IsOccupied)
                return true;
        }

        return false;
    }

    public int OccupiedCount => cells.Count(cell => cell.IsOccupied);

    public void AddPassenger(PassengerController passenger)
    {
        if (passenger == null)
            return;

        Debug.Log(
            $"Adding passenger {passenger.name} to waiting area. " +
            $"Already in waiting? {IsPassengerInWaiting(passenger)}"
        );

        if (IsPassengerInWaiting(passenger))
            return;

        addQueue.Enqueue(passenger);

        if (!isProcessing)
            StartCoroutine(ProcessQueue());
    }

    public void RemovePassenger(PassengerController passenger)
    {
        if (passenger == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].CurrentPassanger == passenger)
            {
                cells[i].CurrentPassanger = null;
                passenger.isInWaiting = false;
                break;
            }
        }
    }

    public List<PassengerController> GetMatchingPassengers(PassengerColor color)
    {
        List<PassengerController> matches = new();
        int count = 0;

        foreach (var cell in cells)
        {
            if (!cell.IsOccupied)
                continue;

            if (cell.CurrentPassanger.color == color)
            {
                matches.Add(cell.CurrentPassanger);

                if (count >= 2)
                    break;

                count++;
            }
        }

        return matches;
    }

    public bool HasMatchingPassenger(PassengerColor color)
    {
        foreach (var cell in cells)
        {
            if (cell.IsOccupied &&
                cell.CurrentPassanger.color == color)
                return true;
        }

        return false;
    }

    public bool IsPassengerInWaiting(PassengerController passenger)
    {
        foreach (var cell in cells)
        {
            if (cell.CurrentPassanger == passenger)
                return true;
        }

        return false;
    }
 

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (addQueue.Count > 0)
        {
            PassengerController passenger = addQueue.Dequeue();

            foreach (var cell in cells)
            {
                if (cell.IsOccupied)
                    continue;

                cell.CurrentPassanger = passenger;
                PlacePassenger(passenger, cell);

                yield return null;
                break;
            }
        }

        isProcessing = false;
    }

    private void PlacePassenger(
        PassengerController passenger,
        WaitingSlotCell cell)
    {
        Vector3 targetPosition =
            cell.transform.position + Vector3.up * 0f;

        passenger.MoveToPosition(targetPosition, () =>
        {
            passenger.isInWaiting = true;

            if (passenger.arrowInstance != null)
                passenger.arrowInstance.SetActive(false);

            Debug.Log(
                $"Passenger placed at waiting position: {cell.transform.position}"
            );

            GameManager.Instance.AutoBoardWaitingPassengers();
        });

        Debug.Log($"Waiting position {cell.transform.position}");
    }
     
}

//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using static GameEnums;

//public class WaitingAreaSlot : MonoBehaviour
//{
//    private List<WaitingSlotCell> cells = new List<WaitingSlotCell>();
//    private Queue<PassengerController> addQueue = new Queue<PassengerController>();  
//    private bool isProcessing = false;   

//    private void Awake()
//    {
//        cells.Clear();
//        cells.AddRange(GetComponentsInChildren<WaitingSlotCell>());
//    }

//    public bool HasSpace()
//    { 
//        foreach (var cell in cells)
//        {
//            if (!cell.IsOccupied)
//                return true;
//        }
//        return false;
//    }

//    public int OccupiedCount => cells.Count(cell => cell.IsOccupied);

//    public void AddPassenger(PassengerController p)
//    {
//        Debug.Log($"Adding passenger {p.name} to waiting area. Already in waiting? {IsPassengerInWaiting(p)}");
//        if (IsPassengerInWaiting(p)) return;

//        addQueue.Enqueue(p);  
//        if (!isProcessing)
//        {
//            StartCoroutine(ProcessQueue());   
//        }
//    }
//    public PassengerController GetFirstMatchingPassenger(PassengerColor color)
//    {
//        foreach (var cell in cells)
//        {
//            if (cell.IsOccupied && cell.CurrentPassanger.color == color)
//            {
//                return cell.CurrentPassanger;
//            }
//        }
//        return null;
//    }

//    private IEnumerator ProcessQueue()
//    {
//        isProcessing = true;
//        while (addQueue.Count > 0)
//        {
//            PassengerController p = addQueue.Dequeue();
//            foreach (var cell in cells)
//            {
//                if (!cell.IsOccupied)
//                {
//                    cell.CurrentPassanger = p;   
//                    PlacePassenger(p, cell);
//                    //yield return new WaitForSeconds(0.2f);

//                    yield return null;
//                    break;
//                }
//            } 
//        }
//        isProcessing = false;
//    }

//    void PlacePassenger(PassengerController p, WaitingSlotCell cell)
//    {
//        Vector3 targetPos = cell.transform.position + Vector3.up * 0.5f;

//        p.MoveToPosition(targetPos, () =>
//        {
//            p.isInWaiting = true;
//            p.arrowInstance.SetActive(false);
//            if (GameManager.Instance.currentBus != null)
//            {
//                GameManager.Instance.AutoBoardWaitingPassengers();
//            }
//            Debug.Log($"Passenger placed at waiting position: {cell.transform.position}");
//        });
//        Debug.Log($"Waiting position {cell.transform.position}");
//    }

//    public List<PassengerController> GetMatchingPassengers(PassengerColor color)
//    {
//        List<PassengerController> matches = new();
//        int count = 0;
//        foreach (var cell in cells)
//        {

//            if (cell.IsOccupied && cell.CurrentPassanger.color == color)
//            {
//                matches.Add(cell.CurrentPassanger);
//                if(count >= 2) break; 
//                count++;
//            }


//        }

//        return matches;
//    }

//    public void RemovePassenger(PassengerController p)
//    {
//        for (int i = 0; i < cells.Count; i++)
//        {
//            if (cells[i].CurrentPassanger == p)
//            {
//                cells[i].CurrentPassanger = null;
//                p.isInWaiting = false;
//                return;
//            }
//        }
//    }


//    public bool IsPassengerInWaiting(PassengerController p)
//    {
//        foreach (var cell in cells)
//        {
//            if (cell.CurrentPassanger == p) return true;
//        }
//        return false;
//    }

//    public bool HasMatchingPassenger(PassengerColor color)
//    {
//        foreach (var cell in cells)
//        {
//            if (cell.IsOccupied && cell.CurrentPassanger.color == color)
//                return true;
//        }
//        return false;
//    }
//}