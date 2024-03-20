using System.Collections.Generic;
using UnityEngine;

namespace PebblesReadsPearls;

public class OracleRMModule
{
    public DataPearl? InspectPearl { get; set; } = null;

    public DataPearl? FloatPearl { get; set; } = null;
    public Vector2? HoverPos { get; set; } = null;

    public Dictionary<DataPearl.AbstractDataPearl, int> ReadPearls { get; } = new();
    public List<AbstractPhysicalObject> WasGrabbedByPlayer { get; } = new();

    public bool WasAlreadyRead { get; set; } = false;
    public int Rand { get; set; } = 0;

    public int UniquePearlsBrought { get; set; } = 0;
}