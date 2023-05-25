using System.Collections.Generic;
using UnityEngine;

namespace PebblesReadsPearls;

public class OracleRMModule
{
    public DataPearl? inspectPearl = null;

    public DataPearl? floatPearl = null;
    public Vector2? hoverPos = null;

    public readonly Dictionary<DataPearl.AbstractDataPearl, int> readPearls = new();
    public readonly List<AbstractPhysicalObject> wasGrabbedByPlayer = new();

    public bool wasAlreadyRead = false;
    public int rand = 0;
}