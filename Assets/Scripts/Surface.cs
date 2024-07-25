using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SurfaceType
{
    Default,
    Wood,
    Metal
}

public class Surface : MonoBehaviour
{
    [SerializeField]
    private SurfaceType _surfaceType;
    public SurfaceType SurfaceType => _surfaceType;   
}
