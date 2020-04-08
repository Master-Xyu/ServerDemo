using System;
using System.Collections.Generic;

public class PlayerTempData
{
    public PlayerTempData()
    {
        status = Status.None;
    }
    public enum Status
    {
        None,
        Room,
        Fight,
    }
    public Status status;
    public Room room;
    public int team = 1;
    public bool isOwner = false;
}