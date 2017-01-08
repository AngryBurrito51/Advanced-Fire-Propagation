using UnityEngine;
using System.Collections;

public class WindSettings : MonoBehaviour
{

    [Header ("Settings")]
    public bool isWindActive = false;
    public static bool IsWindActive = false;
    [Tooltip("Directions: 0 = +Z | 90 = +X | 180 = -Z | 270 = -X")]
    [Range(0,360)]
    public int windDirection = 0;
    [Range (0f, 1f)]
    public float windStrength = 1;
    private int lastWindDir = 0;

    [Header ("Node/Wind Setting")]
    [Tooltip("This is the max that a node can be affected by the wind, extra amount that the nodes will lose HP when next in line to burn")]
    public int maxHPLossChangeDueToWind = 10;

    static int xHPLossPos = 0;
    static int zHPLossPos = 0;
    static int xHPLossNeg = 0;
    static int zHPLossNeg = 0;

    void Start()
    {
        lastWindDir = windDirection;
        CalculateXAndZHPLossChange ();
        InvokeRepeating ("UpdateWind", 0f, 1f);
    }

    void UpdateWind()
    {
        IsWindActive = isWindActive;

        if(IsWindActive)
        {
            if(windDirection != lastWindDir)
            {
                CalculateXAndZHPLossChange ();
            }
        }
    }

    void CalculateXAndZHPLossChange()
    {
        int angle = 0;

        float x = maxHPLossChangeDueToWind;
        float z = maxHPLossChangeDueToWind;

        if (windDirection <= 90)
        {
            angle = 90 - windDirection;
        }
        else if (180 >= windDirection && windDirection > 90)
        {
            angle = windDirection - 90;
            z *= (-1);
        }
        else if (270 >= windDirection && windDirection > 180)
        {
            angle = windDirection - 270;
            z *= (-1);
            x *= (-1);
        }
        else
        {
            angle = 270 + windDirection;
            x *= (-1);
        }

        float xComponent = Mathf.Cos (Mathf.Deg2Rad * angle) * windStrength;
        float zComponent = Mathf.Sqrt((windStrength * windStrength) - (xComponent * xComponent));

        z *= zComponent;
        x *= xComponent;

        int xHPLoss = Mathf.RoundToInt (x);
        int zHPLoss = Mathf.RoundToInt (z);

        if(xHPLoss >= 0)
        {
            xHPLossPos = xHPLoss;
            xHPLossNeg = -1 * xHPLoss;
        }
        else
        {
            xHPLossPos = xHPLoss;
            xHPLossNeg = -1 * xHPLoss;
        }

        if (zHPLoss >= 0)
        {
            zHPLossPos = zHPLoss;
            zHPLossNeg = -1 * zHPLoss;
        }
        else
        {
            zHPLossPos = zHPLoss;
            zHPLossNeg = -1 * zHPLoss;
        }

        lastWindDir = windDirection;

        //Debug.Log (xComponent);
        //Debug.Log ("Z HP Loss: " + z + "  X HP Loss: " + x);
    }

    public bool IsWndActive()
    {
        return IsWindActive;
    }

    public static int GetXHPLossPos()
    {
        return xHPLossPos;
    }

    public static int GetZHPLossPos()
    {
        return zHPLossPos;
    }

    public static int GetXHPLossNeg()
    {
        return xHPLossNeg;
    }

    public static int GetZHPLossNeg()
    {
        return zHPLossNeg;
    }

}
