using System.Collections.Generic;
using UnityEngine;

namespace PebblesReadsPearls;

public class OracleRMModule
{
    public DataPearl? inspectPearl = null;

    public DataPearl? floatPearl = null;
    public Vector2? hoverPos = null;

    public readonly List<DataPearl.AbstractDataPearl> readPearls = new();
    public readonly List<AbstractPhysicalObject> wasGrabbedByPlayer = new();
}