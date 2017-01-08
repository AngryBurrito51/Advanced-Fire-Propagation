using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Node:MonoBehaviour
{
    public GameObject particles;

    public LayerMask nonFlammableLayer;

    private GameObject burnMaterialObject;
    private BurnMaterial burnMaterial;

    private bool isFlammable = false;
    private bool isOnFire = false;
    private bool isPaused = false;
    private bool isBurnedOut = false;
    private bool initialUpdate = true;
    private bool resetNodeHpOnNoBurn = false;

    private int nodeHP = 100;
    private int nodeFuel = 100;
    private int hpLoss = 1;
    private int fuelLoss = 1;
    private int slopeUpHPLossDiff = 0;
    private int slopeDownHPLossDiff = 0;
    private float nodeBurnChance = 0;
    private int nodeExtraFuelLoss = 0;
    private int nodeExtraHPLoss = 0;
    private int resetHPAfterNoBurn = 0;

    private float nodeRadius = 0;

    private bool needsReset = false;

    private Vector3 nodeCoords;
    private Vector3 nodePos;
    private Vector3 firePos;

    private Collider triggerCollider;

    private FirePropagation firePropagationController;

    private List<Node> nodesToBurn = new List<Node> ();

    void Awake()
    {
        triggerCollider = this.gameObject.GetComponent<BoxCollider> ();
    }

    /// <summary>
    /// Normal node constructor
    /// </summary>
    public void SetUpNode (bool isFlammable, int nodeHP, int nodeFuel, int hpLoss, int fuelLoss,float nodeRadius, int slopeUpHPLossDiff, int slopeDownHPLossDiff, float nodeBurnChance,
        bool resetHpOnNoBurnChance, int resetHPAfterNoBurn, Vector3 nodeCoords, Vector3 nodePos, FirePropagation firePropagationController,GameObject burnMaterialObject)
    {
        //If node is reused from a previous fire grid, it requires a reset on most of its variables
        if(needsReset)
        {                       
            isOnFire = false;
            isPaused = false;
            isBurnedOut = false;
            initialUpdate = true;
            DeActivateTriggerCollider ();
            nodesToBurn.Clear ();
            particles.SetActive (false);
        }

        this.isFlammable = isFlammable;
        this.nodeHP = nodeHP;
        this.nodeFuel = nodeFuel;
        this.hpLoss = hpLoss;
        this.fuelLoss = fuelLoss;
        this.nodeCoords = nodeCoords;
        this.nodePos = nodePos;
        this.firePropagationController = firePropagationController;
        this.nodeRadius = nodeRadius;
        this.slopeUpHPLossDiff = slopeUpHPLossDiff;
        this.slopeDownHPLossDiff = slopeDownHPLossDiff;
        this.nodeBurnChance = nodeBurnChance;
        this.resetNodeHpOnNoBurn = resetHpOnNoBurnChance;
        this.burnMaterialObject = burnMaterialObject;
        this.resetHPAfterNoBurn = resetHPAfterNoBurn;
    }

    /// <summary>
    /// Fire starting node constructor 
    /// </summary>
    public void SetUpNode (int hpLoss, int fuelLoss, float nodeRadius, int slopeUpHPLossDiff, int slopeDownHPLossDiff, Vector3 nodeCoords, Vector3 nodePos, FirePropagation firePropagationController)
    {
        isFlammable = false;
        isOnFire = true;

        if (needsReset)
        {
            nodeHP = 100;
            nodeFuel = 100;
            isPaused = false;
            isBurnedOut = false;
            initialUpdate = true;
            nodesToBurn.Clear ();
            particles.SetActive (false);
        }

        this.hpLoss = hpLoss;
        this.fuelLoss = fuelLoss;
        this.nodeCoords = nodeCoords;
        this.nodePos = nodePos;
        this.firePropagationController = firePropagationController;
        this.nodeRadius = nodeRadius;
        this.slopeUpHPLossDiff = slopeUpHPLossDiff;
        this.slopeDownHPLossDiff = slopeDownHPLossDiff;
    }

    void OnDisable()
    {
        triggerCollider.enabled = false;
    }

    public void UpdateThisNodePart1()
    {
        if(!isBurnedOut)
        {
            if(initialUpdate)
            {
                initialUpdate = false;

                StartCoroutine (FindNeighbours ());
            }
            else
            {
                StartCoroutine (UpdateThisNodePart2 ());
            }   
        }
    }

    IEnumerator UpdateThisNodePart2()
    {
        if (nodesToBurn.Count > 0)
        {
            int xHPLossPos = 0;
            int xHPLossNeg = 0;
            int zHPLossNeg = 0;
            int zHPLossPos = 0;

            //If wind is active, set the directional forces of the wind
            if (WindSettings.IsWindActive)
            {
                xHPLossPos = WindSettings.GetXHPLossPos ();
                xHPLossNeg = WindSettings.GetXHPLossNeg ();
                zHPLossNeg = WindSettings.GetZHPLossNeg ();
                zHPLossPos = WindSettings.GetZHPLossPos ();
            }
            else
            {
                xHPLossPos = 0;
                xHPLossNeg = 0;
                zHPLossNeg = 0;
                zHPLossPos = 0;
            }

            for (int i = 0; i < nodesToBurn.Count; i++)
            {
                Node currentNode = nodesToBurn[i];

                if (!currentNode.IsOnFire ())
                {
                    //Slope now affects the fire spread speed by comparing the neighbour node coordinates against this node
                    if(currentNode.GetY () == nodeCoords.y)
                    {
                        if(currentNode.GetX() > nodeCoords.x)
                        {
                            currentNode.ReduceNodeHP (xHPLossPos);
                        }

                        if (currentNode.GetX () < nodeCoords.x)
                        {
                            currentNode.ReduceNodeHP (xHPLossNeg);
                        }

                        if (currentNode.GetZ () > nodeCoords.z)
                        {
                            currentNode.ReduceNodeHP (zHPLossPos);
                        }

                        if (currentNode.GetZ () < nodeCoords.z)
                        {
                            currentNode.ReduceNodeHP (zHPLossNeg);
                        }
                    }
                    else if(currentNode.GetY() > nodeCoords.y)
                    {
                        //Debug.Log ("Using");
                        currentNode.ReduceNodeHP (slopeUpHPLossDiff);
                    }
                    else if(currentNode.GetY () < nodeCoords.y)
                    {
                        currentNode.ReduceNodeHP (-slopeDownHPLossDiff);
                    }                       

                    //currentNode.UpdateThisNodePart1 ();
                }
                yield return new WaitForEndOfFrame ();
            }
        }

        ReduceNodeFuel (nodeExtraFuelLoss);
    }

    /// <summary>
    /// Check if neighbour node is still flammable
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    bool NeighbourNodeIsFlammable(Vector3 pos)
    {
        if (Physics.CheckSphere (pos, nodeRadius, nonFlammableLayer))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Node needs reset if being reused from another fire grid (node pool)
    /// </summary>
    public void NeedsReset()
    {
        needsReset = true;
    }

    /// <summary>
    /// Returns (int) x node coordinate
    /// </summary>
    /// <returns></returns>
    public int GetX()
    {
        return (int)nodeCoords.x;
    }

    /// <summary>
    /// Returns (int) y node coordinate
    /// </summary>
    /// <returns></returns>
    public int GetY()
    {
        return (int)nodeCoords.y;
    }

    /// <summary>
    /// Returns (int) z node coordinate
    /// </summary>
    /// <returns></returns>
    public int GetZ()
    {
        return (int)nodeCoords.z;
    }

    /// <summary>
    /// Returns node world position
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPos()
    {
        return nodePos;
    }

    /// <summary>
    /// Returns isFlammable bool (wheather the node can be set alight)
    /// </summary>
    /// <returns></returns>
    public bool IsFlammable()
    {
        return isFlammable;
    }

    /// <summary>
    /// Returns isOnFire bool (Wheather the node is already on fire)
    /// </summary>
    /// <returns></returns>
    public bool IsOnFire()
    {
        return isOnFire;
    }

    /// <summary>
    /// Return isPaused bool (Wheather the node is not flammable for few moments from random chance)
    /// </summary>
    /// <returns></returns>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Return isBurnedOut bool (Wheather the node is finished burning)
    /// </summary>
    /// <returns></returns>
    public bool IsBurnedOut()
    {
        return isBurnedOut;
    }

    void SetOnFire()
    {
        if (NeighbourNodeIsFlammable (nodePos))
        {
            isOnFire = true;
            isFlammable = false;

            particles.SetActive (true);

            firePropagationController.SetNodesOnFire (1);
            firePropagationController.NodesToAddToFireList (this);

            ActivateTriggerCollider ();
        }
    }

    void SetOutOfFuel()
    {
        if(!isBurnedOut)
        {
            isOnFire = false;
            isBurnedOut = true;

            particles.SetActive (false);

            firePropagationController.SetNodesOnFire (-1);
            firePropagationController.NodesToRemoveFromFireList (this);
        }
    }

    public void ReduceNodeHP(int extraHPLoss)
    {
        CheckMaterial ();

        int i = Random.Range (0, 100);

        //If by chance burning is allowed, reduce this node HP
        if(i <= nodeBurnChance)
        {
            nodeHP -= (hpLoss + extraHPLoss + nodeExtraHPLoss);

            if(nodeHP <= 0)
            {
                SetOnFire ();
            }
        }
        else
        {

            //Resets node HP if node didnt burn
            if(resetNodeHpOnNoBurn)
            {
                nodeHP = resetHPAfterNoBurn;
            }
        }
    }

    void ReduceNodeFuel(int extraFuelLoss)
    {
        nodeFuel -= (fuelLoss + extraFuelLoss);

        if(nodeFuel <= 0)
        {
            SetOutOfFuel ();
        }
    }

    void ActivateTriggerCollider()
    {
        triggerCollider.enabled = true;
    }

    void DeActivateTriggerCollider()
    {
        triggerCollider.enabled = false;
    }

    void CheckMaterial()
    {
        if(burnMaterial == null)
        {
            //Get burn material script from burning object the node is on
            if (burnMaterialObject != null)
            {
                //Debug.Log (burnMaterialObject.name);

                if (burnMaterial = burnMaterialObject.GetComponent<BurnMaterial> ())
                {
                    nodeExtraFuelLoss = burnMaterial.extraNodeFuelLoss;
                    nodeExtraHPLoss = burnMaterial.extraNodeHPLoss;
                }
            }
        }
    }

    IEnumerator FindNeighbours()
    {
        //Find neighbour nodes to start burning, within 1 node distance from current node
        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    //Get neighbour node coordinates in grid space
                    int xCoord = (int) nodeCoords.x + x;
                    int yCoord = (int) nodeCoords.y + y;
                    int zCoord = (int) nodeCoords.z + z;
                    Vector3 currentCoordToCheck = new Vector3 (xCoord, yCoord, zCoord);

                    Node neighbour;

                    //Get node at calculated coordinates
                    neighbour = firePropagationController.GetNodeAtCoordinates (currentCoordToCheck);

                    //If a node is returned, and it is flammable, add it to the nodes to burn next
                    if (neighbour != null && neighbour.IsFlammable())
                    {
                        nodesToBurn.Add (neighbour);
                        //Debug.Log (neighbour.GetPos () + " " + this.GetPos ());
                    }
                    //Debug.Break ();
                }
                yield return new WaitForEndOfFrame ();
            }

            //spread search of nodes over few frames rather than 1 frame
            yield return new WaitForEndOfFrame ();
        }

        UpdateThisNodePart2 ();
    }

    void OnTriggerEnter(Collider col)
    {
        DynamicObjectFire objectScript;
        DynamicNode dynamicNodeScript;

        if (dynamicNodeScript = col.gameObject.GetComponent<DynamicNode> ())
        {
            if (dynamicNodeScript.CanIgnite ())
            {
                dynamicNodeScript.SetOnFire ();
            }
        }
        else if (objectScript = col.gameObject.GetComponent<DynamicObjectFire>())
        {
            if(objectScript.isFireRetardant)
            {
                SetOutOfFuel ();
            }
            else if(objectScript.IsFlammable())
            {
                objectScript.StartFireCoroutine ();
            }
        }
    }

}
