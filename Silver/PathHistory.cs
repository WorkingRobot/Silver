using System.Collections.Generic;

namespace Silver
{
    public class PathHistory
    {
        LinkedList<string> History = new LinkedList<string>();
        LinkedListNode<string> CurrentPoint;
        readonly int MaxEntries;

        public PathHistory(int maxEntries)
        {
            MaxEntries = maxEntries;
        }

        public void Clear()
        {
            History.Clear();
            CurrentPoint = null;
        }

        public string Navigate(string path)
        {
            if (CurrentPoint != null && History.Last != CurrentPoint)
            {
                while (CurrentPoint.Next != null)
                {
                    CurrentPoint = CurrentPoint.Next;
                    History.Remove(CurrentPoint.Previous);
                }
            }
            CurrentPoint = History.AddLast(path);
            if (History.Count > MaxEntries)
            {
                History.RemoveFirst();
            }
            return path;
        }

        public string MoveBack()
        {
            if (CurrentPoint != History.First)
            {
                CurrentPoint = CurrentPoint.Previous;
            }
            return CurrentPoint.Value;
        }

        public string MoveForward()
        {
            if (CurrentPoint != History.Last)
            {
                CurrentPoint = CurrentPoint.Next;
            }
            return CurrentPoint.Value;
        }

        public string Current => CurrentPoint?.Value;
    }
}
