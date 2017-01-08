using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FirePropagation : MonoBehaviour
{

    //Testing variables
    List<float> results = new List<float> ();
    List<float> resultsAverage = new List<float> ();

    float gridCreationTimePassed = 0;
    int nodesCreated = 0;
    float average = 0;

    float gridFireSpreadTimePassed = 0;
    int timeStep = 0;
    //--------------

    public bool enableVisualDebug = false;

    [Header ("Fire Center")]
    public Vector3 fireStart;

    private Vector3 gridNodeStart;
    private Vector3 currentNodePos;

    [Header ("Node Settings")]
    public GameObject nodePrefab;
    public float nodeDiameter = 0;
    public int defaultNodeHP = 100;
    public int defaultNodeFuel = 100;

    private float nodeRadius = 0;

    [Header ("Cube Grid Settings")]
    public Vector3 gridSize;
    [Tooltip ("Slower to create the fire grid with sphericalGrid enabled")]
    public bool useSphericalGrid = false;
    [Tooltip("Should be less than x or y value than cube grid size")]
    public float sphereDiameter = 10;


    private int gridX = 0;
    private int gridY = 0;
    private int gridZ = 0;

    private int centerX;
    private int centerY;
    private int centerZ;

    //private bool isGridComplete = false;

    [Header ("Flammability Layers")]
    public LayerMask flammableLayers;
    public LayerMask nonFlammableLayers;

    [Header ("Fire Randomness Settings")]
    public float initialNodeFireStartChance = 100f;
    public float nodeBurnChance = 100f;
    public bool resetNodeHpIfNoBurn = true;
    public int resetNodeHPTo = 20;

    [Header ("Fire Update Settings")]
    public float nodeUpdateRate = 0;
    [Range(0,100)]
    public int nodeHPLoss = 0;
    [Range(0,100)]
    public int nodeFuelLoss = 0;
    [Range (0, 100)]
    public int slopeUpHPLossDiff = 0;
    [Range (0, 100)]
    public int slopeDownHPLossDiff = 0;
    /*[Range(-100,0)]
    public int wetNodeHPLossChange = 0;
    [Range (0, 100)]
    public int wetNodeFuelLossChange = 0;*/

    [Header ("Post Fire Settings")]
    public float delayToGridDestruction = 5f;

    private int totalNodesOnFire = 0;

    NodePool nodePool;

    private Dictionary<Vector3, Node> gridNodes = new Dictionary<Vector3, Node> ();
    private List<Node> gridNodesList = new List<Node> ();
    private List<Node> nodesOnFire = new List<Node> ();

    private List<Node> nodesToRemoveFromFire = new List<Node> ();
    private List<Node> nodesToAddToFire = new List<Node> ();

    //float frameTimes = 0;

    void Awake()
    {
        //Check if prefab is referenced in inspector, otherwise give an error in console
        if(!nodePrefab)
        {
            Debug.LogError ("Fire Propagation: No Node GameObject Prefab Found!");
        }

        if(slopeDownHPLossDiff > nodeHPLoss)
        {
            Debug.LogError ("Cannot Have A Greator Loss in HP On Slope Down Than Actual HP Loss Of The Node! (Change slopeDownHPLossDiff Amount)");
        }

        nodePool = GameObject.Find ("Node Pool").GetComponent<NodePool> ();

        //Debug.Log ("Awake Runs");
    }

    void Update()
    {
        // Check ms per frame till grid is complete
        /*if(!isGridComplete)
        {
            frameTimes = Time.deltaTime;
            Debug.Log (frameTimes * 1000);
        }*/

        //Debug.Log (totalNodesOnFire);

        //Debug.Log ("NodesToAddToFire List Count: " + nodesToAddToFire.Count);
        //Debug.Log ("NodesToRemoveFromFire List Count: " + nodesToRemoveFromFire.Count);


        //Debug.Log ("nodes on fire: " + totalNodesOnFire);
    }

    void Start()
    {
        fireStart = transform.position;

        nodeRadius = nodeDiameter / 2;

        //Calculate grid xyz (coordinate) size
        gridX = Mathf.RoundToInt (gridSize.x / nodeDiameter);
        gridY = Mathf.RoundToInt (gridSize.y / nodeDiameter);
        gridZ = Mathf.RoundToInt (gridSize.z / nodeDiameter);

        centerX = gridX / 2;
        centerY = gridY / 2;
        centerZ = gridZ / 2;

        //Debug.Log ("Grid Node Capacity: " + "xyz = " + gridX + "," + gridY + "," + gridZ);

        if(WillFireStart ())
        {
            //if(!enableVisualDebug)
            StartFirePropagation ();
        }
        else
        {
            //destroy fire grid if fire has not started
            Destroy (this.gameObject);
        }

        //Debug.Log ("Start Runs");
    }

    /// <summary>
    /// Check if initial fire grid node will start the fire
    /// </summary>
    /// <returns></returns>
    bool WillFireStart()
    {
        if(initialNodeFireStartChance == 100)
        {
            return true;
        }
        else
        {
            int i = UnityEngine.Random.Range (0, 100);

            if(i <= initialNodeFireStartChance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public void StartFirePropagation()
    {
        //IF starting position is flammable, can start fire propagation
        if (IsNodeFlammable (fireStart))
        {
            //Test variable
            nodesCreated = gridY * gridX * gridZ;

            //Need to create a grid of xyz over the next couple of frames rather than in 1 frame to reduce workload per frame
            gridNodeStart = fireStart - (gridSize / 2);
            gridNodeStart.x += nodeRadius;
            gridNodeStart.y += nodeRadius;
            gridNodeStart.z += nodeRadius;

            StartCoroutine (CreateGrid ());
        }
    }

    IEnumerator CreateGrid()
    {
        gridCreationTimePassed = Time.realtimeSinceStartup;
        //Debug.Log ("-----------------------------" + gridY + " " + gridX + " " + gridZ);
        //Create a grid of x,y,z size
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                for (int z = 0; z < gridZ; z++)
                {
                    currentNodePos.x = gridNodeStart.x + x * nodeDiameter;
                    currentNodePos.y = gridNodeStart.y + y * nodeDiameter;
                    currentNodePos.z = gridNodeStart.z + z * nodeDiameter;

                    if(useSphericalGrid)
                    {
                        if(Vector3.Distance(fireStart,currentNodePos) <= (sphereDiameter/2))
                        {

                        }
                        else
                        {
                            continue;
                        }
                    }

                    Vector3 nodeCoord = new Vector3 (x, y, z);
                    if (CheckForObjects (currentNodePos))
                    {
                        //Debug.Log ("---------------------- Object Found");

                        GameObject obj = CheckObject (currentNodePos);

                        if(obj.isStatic)
                        {
                            //Debug.Log ("----------------------- Object static");

                            if (x == centerX && y == centerY && z == centerZ)
                            {
                                //Debug.Log ("First Fire Node Created");
                                //GameObject newNodeGameObject = Instantiate (nodePrefab, currentNodePos, Quaternion.identity) as GameObject;
                                GameObject newNodeGameObject = nodePool.GetNodeGameObjectFromPool (currentNodePos);
                                newNodeGameObject.SetActive (true);

                                Node newNode = newNodeGameObject.GetComponent<Node> ();
                                newNode.SetUpNode (nodeHPLoss, nodeFuelLoss, nodeRadius, slopeUpHPLossDiff, slopeDownHPLossDiff, nodeCoord, currentNodePos, this);
                                //Node newNode = new Node (nodeHPLoss, nodeFuelLoss, nodeCoord, currentNodePos,this);

                                totalNodesOnFire++;

                                gridNodes.Add (nodeCoord, newNode);
                                nodesOnFire.Add (newNode);

                                gridNodesList.Add (newNode);
                            }
                            else
                            {
                                //Debug.Log ("other node");
                                //GameObject newNodeGameObject = Instantiate (nodePrefab, currentNodePos, Quaternion.identity) as GameObject;
                                GameObject newNodeGameObject = nodePool.GetNodeGameObjectFromPool (currentNodePos);
                                newNodeGameObject.SetActive (true);

                                Node newNode = newNodeGameObject.GetComponent<Node> ();
                                newNode.SetUpNode (IsNodeFlammable (currentNodePos), defaultNodeHP, defaultNodeFuel, nodeHPLoss, nodeFuelLoss, nodeRadius, slopeUpHPLossDiff, slopeDownHPLossDiff, nodeBurnChance,
                                    resetNodeHpIfNoBurn, resetNodeHPTo, nodeCoord, currentNodePos, this, obj );
                                //Node newNode = new Node (IsNodeFlammable (currentNodePos), nodeHPLoss, nodeFuelLoss, nodeCoord, currentNodePos,this);

                                gridNodes.Add (nodeCoord, newNode);
                        
                                gridNodesList.Add (newNode);
                            }
                        }
                    }
                }
            }

            gridCreationTimePassed = Time.realtimeSinceStartup - gridCreationTimePassed;
            average += gridCreationTimePassed;
            //Debug.Log ("Time Passed: " + gridCreationTimePassed);
            gridCreationTimePassed = Time.realtimeSinceStartup;
            //Create 1 layer every frame (layer means a set of x,z nodes on y(layer))
            yield return new WaitForEndOfFrame ();
          
        }

        Debug.Log (" Nodes Created: " + nodesCreated);
        //isGridComplete = true;
        //Start updating all nodes of the grid for fire propagation
        Invoke ("UpdateGridNodes", nodeUpdateRate);
    }

    /// <summary>
    /// Check wheather current node overlaps any flammable or nonflammable objects
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    bool IsNodeFlammable(Vector3 position)
    {
        //Check if there are colliders within the node that are marked with flammable layers
        if(Physics.CheckSphere (position, nodeRadius,flammableLayers))
        {
            //Check if there are colliders within the node that are marked with nonflammable layers
            if (Physics.CheckSphere(position,nodeRadius,nonFlammableLayers))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Check for material of object that node is currently on
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    GameObject CheckObject(Vector3 position)
    {
        QueryTriggerInteraction trigger = QueryTriggerInteraction.UseGlobal;

        Collider[] hitCols = Physics.OverlapSphere (position, nodeRadius, flammableLayers, trigger);

        if (hitCols.Length > 0)
        {
            //Debug.Log ("--------------------- Found an sphere overlaped object " + hitCols[0].gameObject.name);

            return hitCols[0].gameObject;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Check wheather current node overlaps any flammable objects
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    bool CheckForObjects(Vector3 position)
    {
        return Physics.CheckSphere (position,nodeRadius,flammableLayers);
    }

    void UpdateGridNodes()
    {
        //Debug.Log ("Update");
        //Use for loop instead of foreach, which gives increased performance when using lists
        for (int node = 0; node < nodesOnFire.Count; node++)
        {
            nodesOnFire[node].UpdateThisNodePart1 ();
        }

        RemoveAddNodesForFire ();

        Invoke ("UpdateGridNodes", nodeUpdateRate);
    }

    void RemoveAddNodesForFire()
    {
        if(nodesToRemoveFromFire.Count > 0)
        {
            foreach(Node node in nodesToRemoveFromFire)
            {
                nodesOnFire.Remove (node);
            }

            nodesToRemoveFromFire.Clear ();
        }

        if(nodesToAddToFire.Count > 0)
        {
            foreach (Node node in nodesToAddToFire)
            {
                nodesOnFire.Add (node);
            }

            nodesToAddToFire.Clear ();
        }
    }

    public Node GetNodeAtCoordinates(Vector3 coords)
    {
        try { return gridNodes[coords]; }
        catch { return null; }
    }

    public void NodesToAddToFireList(Node nodeOnFire)
    {
        nodesToAddToFire.Add (nodeOnFire);
    }

    public void NodesToRemoveFromFireList(Node nodeOnFire)
    {
        nodesToRemoveFromFire.Add (nodeOnFire);
    }

    /// <summary>
    /// Change total nodes that or on fire
    /// </summary>
    /// <param name="changeInNodesOnFire"></param>
    public void SetNodesOnFire(int changeInNodesOnFire)
    {
        totalNodesOnFire += changeInNodesOnFire;

        if(totalNodesOnFire <= 0)
        {
            Invoke("RemoveFireGrid", delayToGridDestruction);
        }
    }

    void RemoveFireGrid()
    {
        for(int i = 0; i < gridNodesList.Count; i++)
        {
            gridNodesList[i].NeedsReset ();
            nodePool.ReturnNodeGameObjectToPool (gridNodesList[i].gameObject);
        }

        gridNodesList.Clear ();
        Destroy (this.gameObject); //RE WRITE USING POOL SYSTEM FOR FIRE GRIDS
    }

    void OnDrawGizmos()
    {
        if(enableVisualDebug)
        {
            Gizmos.DrawWireCube (fireStart, gridSize);

            if(gridNodesList.Capacity > 0)
            {
                foreach (Node node in gridNodesList)
                {
                    if(node.IsFlammable())
                    {
                        Gizmos.color = Color.white;
                    }
                    else
                    {
                        Gizmos.color = Color.grey;

                        if (node.IsOnFire ())
                        {
                            Gizmos.color = Color.red;
                        }
                        else if(node.IsBurnedOut())
                        {
                            Gizmos.color = Color.green;
                        }
                    }

                    Gizmos.DrawWireCube (node.GetPos (), new Vector3 (nodeDiameter -0f, nodeDiameter - 0f, nodeDiameter - 0f));
                }
            }
        }
    }

    void OnGUI()
    {
        if (GUI.Button (new Rect (10, 10, 150, 100), "Start Fire"))
            StartFirePropagation ();

    }
}
