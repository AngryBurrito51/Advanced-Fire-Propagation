using UnityEngine;
using System.Collections;

public class DynamicNode : MonoBehaviour
{
    private bool isOnFire = false;
    private bool isFlammable = true;
    private bool isBurnedOut = false;

    private Vector3 nodeCoord;
    private Vector3 nodeLocalPos;

    private Collider triggerCollider;
    DynamicObjectFire dynamicObjectFireScript;



    void Awake()
    {
        triggerCollider = this.gameObject.GetComponent<BoxCollider> ();
    }
	
    /// <summary>
    /// Default dynamic node
    /// </summary>
    public void NodeSetUp(Vector3 coord, Vector3 localPos, DynamicObjectFire script)
    {
        this.nodeCoord = coord;
        this.nodeLocalPos = localPos;
        this.dynamicObjectFireScript = script;
    }

    /// <summary>
    /// Initial fire starting dynamic node
    /// </summary>
    public void NodeSetUp(bool isOnFire, bool isFlammable, Vector3 coord, Vector3 localPos, DynamicObjectFire script)
    {
        this.isOnFire = isOnFire;
        this.isFlammable = isFlammable;
        this.nodeCoord = coord;
        this.nodeLocalPos = localPos;
        this.dynamicObjectFireScript = script;
    }

    public void SpreadFireAroundObject()
    {
        //Find neighbour nodes to start burning, within 1 node distance from current node
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    //Get neighbour node coordinates in grid space
                    int xCoord = (int) nodeCoord.x + x;
                    int yCoord = (int) nodeCoord.y + y;
                    int zCoord = (int) nodeCoord.z + z;
                    Vector3 currentCoordToCheck = new Vector3 (xCoord, yCoord, zCoord);

                    DynamicNode neighbour;

                    //Get dynamic node at calculated coordinates
                    neighbour = dynamicObjectFireScript.GetNodeAtCoordinates (currentCoordToCheck);

                    //If a node is returned, and it is flammable
                    if (neighbour != null && neighbour.CanIgnite())
                    {
                        neighbour.SetOnFire ();
                    }
                }
            }
        }
    }

    public void SetOnFire()
    {
        isFlammable = false;
        isOnFire = true;

        dynamicObjectFireScript.AddNextNodesOnFire (this);
    }

    public void BurnOut()
    {
        isBurnedOut = true;
        isOnFire = false;
    }

    public void ResetNode()
    {
        isFlammable = true;
        isBurnedOut = false;
        isOnFire = false;
    }

    public Vector3 GetCoord()
    {
        return nodeCoord;
    }

    public bool CanIgnite()
    {
        return isFlammable;
    }

    public bool IsOnFire()
    {
        return isOnFire;
    }

    public Vector3 GetGlobalPos()
    {
        return transform.position;
    }
}
