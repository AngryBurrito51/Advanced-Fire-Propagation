using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DynamicObjectFire : MonoBehaviour
{
    public GameObject dynamicNode;
    public GameObject fireGridObject;

    [Header("Dynamic Object Settings")]
    public bool isFireRetardant = false;
    public bool canSetOnFire = false;
    private bool canSpreadFire = true;
    public LayerMask flammableLayers;
    public LayerMask nonFlammableLayers;

    [Header ("Fire Spread On Object Settings")]
    public float nodeRadius = 0.5f;
    public float nodeBurnSpreadRate = 0.3f;
    public float nodeBurnLifeTime = 4f;
    public float timeBeforeFlammableAgain = 10f;
    public int setOnFireChance = 0;

    [Header ("Fire Spread To Objects Settings")]
    public int fireSpreadChance = 60;
    [Tooltip("Initial delay until can spread fire")]
    public float initialFireSpreadDelay = 2f;
    [Tooltip("After spreading fire, how long to wait to allow fire spreading again")]
    public float repeatedFireSpreadDelay = 1f;
    public bool enableDebug = true;

    private bool isFlammable = false;
    private bool fireSpreadAllowed = false;

    private float nodeDiameter;

    private int xNodes;
    private int yNodes;
    private int zNodes;
    private int nodesOnFire = 0;

    private Mesh mesh;

    private Vector3 center;
    private Vector3 gridSize;
    private Vector3 corner;
    private Vector3 currentNodePos;

    private Collider triggerCollider;

    private Dictionary<Vector3, DynamicNode> gridNodes = new Dictionary<Vector3, DynamicNode> ();
    private List<GameObject> nodeObjs = new List<GameObject> ();
    private List<DynamicNode> nodesOnFireScripts = new List<DynamicNode> ();
    private List<DynamicNode> nextNodesOnFireScripts = new List<DynamicNode> ();
    private List<DynamicNode> allNodesScripts = new List<DynamicNode> ();

    void Awake()
    {
        triggerCollider = dynamicNode.GetComponent<BoxCollider> ();

        if (isFireRetardant)
        {
            if(canSetOnFire)
            {
                //Debug.LogWarning ("Dynamic object set as Fire Retardant and canSetOnFire - Disabling canSetOnFire feature!");
                canSetOnFire = false;
            }
        }

        isFlammable = canSetOnFire;

        mesh = gameObject.GetComponent<MeshFilter> ().mesh;

        //Use bounding box as grid size
        gridSize = mesh.bounds.size;

        float tempNodeRadiusX = nodeRadius / transform.localScale.x;
        float tempNodeRadiusY = nodeRadius / transform.localScale.y;
        float tempNodeRadiusZ = nodeRadius / transform.localScale.z;

        corner = (gridSize / -2);
        corner = corner + new Vector3 (tempNodeRadiusX , tempNodeRadiusY, tempNodeRadiusZ);

        //Debug.Log (gridSize );

        nodeDiameter = nodeRadius * 2;

        gridSize = Vector3.Scale (mesh.bounds.size, transform.localScale);

        xNodes = Mathf.RoundToInt (gridSize.x / nodeDiameter);
        yNodes = Mathf.RoundToInt (gridSize.y / nodeDiameter);
        zNodes = Mathf.RoundToInt (gridSize.z / nodeDiameter);

        dynamicNode.transform.localScale = new Vector3 (nodeDiameter, nodeDiameter, nodeDiameter);

        //Debug.Log (xNodes);

        //StartCoroutine ("StartFire");

    }

    public void StartFireCoroutine()
    {
        StartCoroutine ("StartFire");
        isFlammable = false;
    }

    public IEnumerator StartFire()
    {
        bool initialNode = false;

        for(int y = 0; y < yNodes; y++)
        {
            for (int x = 0; x < xNodes; x++)
            {
                for (int z = 0; z < zNodes; z++)
                {
                    currentNodePos.x = corner.x + ((x * nodeDiameter) / transform.localScale.x);
                    currentNodePos.y = corner.y + ((y * nodeDiameter) / transform.localScale.y);
                    currentNodePos.z = corner.z + ((z * nodeDiameter) / transform.localScale.z);

                    //Debug.Log (corner.x + ((0 * nodeDiameter) / transform.localScale.x));

                    if (CheckForObjects (gameObject.transform.TransformPoint(currentNodePos)))
                    {
                        Vector3 nodeCoord = new Vector3 (x, y, z);

                        GameObject newDynamicNode = Instantiate (dynamicNode, Vector3.zero, Quaternion.identity) as GameObject;
                        newDynamicNode.transform.parent = this.transform;
                        newDynamicNode.transform.localPosition = currentNodePos;

                        nodeObjs.Add (newDynamicNode);

                        DynamicNode script = newDynamicNode.GetComponent<DynamicNode> ();
                        allNodesScripts.Add (script);

                        gridNodes.Add (nodeCoord, script);

                        if (!initialNode)
                        {
                            script.NodeSetUp (true, false, nodeCoord, currentNodePos, this);
                            nodesOnFire++;
                            nodesOnFireScripts.Add (script);

                            initialNode = true;
                        }
                        else
                        {
                            script.NodeSetUp (nodeCoord, currentNodePos, this);
                        }
                    }
                }
            }
            yield return new WaitForEndOfFrame ();
            yield return new WaitForEndOfFrame ();
        }

        StartCoroutine ("UpdateNodes");

        if(canSpreadFire)
        {
            Invoke ("EnableFireSpread", initialFireSpreadDelay);
        }

    }

    IEnumerator UpdateNodes()
    {
        yield return new WaitForSeconds (nodeBurnSpreadRate);

        int iteration = 0;

        if (nodesOnFireScripts.Count > 0)
        {
            for(int i = 0; i < nodesOnFireScripts.Count; i ++)
            {
                iteration++;

                nodesOnFireScripts[i].SpreadFireAroundObject ();

                if(iteration >= 2)
                {
                    iteration = 0;
                    yield return new WaitForEndOfFrame ();
                }
            }

            nodesOnFireScripts.Clear ();
            nodesOnFireScripts = nextNodesOnFireScripts.ToList();
            nextNodesOnFireScripts.Clear ();

            //Debug.Log (nodesOnFireScripts.Count);
        }
        else
        {
            //Destroy grid
        }

        if(nodeObjs.Count <= nodesOnFire)
        {
            Invoke ("SetOutOfFire", nodeBurnLifeTime);
        }
        else
        {
            StartCoroutine ("UpdateNodes");
        }

        //Invoke ("UpdateNodes", nodeBurnSpreadRate);
    }

    public void AddNextNodesOnFire(DynamicNode node)
    {
        nextNodesOnFireScripts.Add (node);
        nodesOnFire++;
    }

    bool CheckForObjects(Vector3 position)
    {
        if(Physics.CheckSphere (position, nodeRadius))
        {
            //Debug.Log ("Reached" + position);
            Collider[] cols = Physics.OverlapSphere (position, nodeRadius);

            if(cols.Length > 0)
            {
                for(int i = 0; i < cols.Length; i++)
                {
                    if (cols[i].gameObject == this.gameObject)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
        else 
        {
            return false;

            //Debug.Log (position);
        } 
    }

    void EnableFireSpread()
    {
        fireSpreadAllowed = true;
    }

    /// <summary>
    /// On collision enter try spread fire from this dynamic object onto surrounding area
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionEnter(Collision col)
    {
        if(fireSpreadAllowed)
        {
            //Debug.Log ("Fire Spread Is Allowed");
            //Debug.Log ("contact " + col.contacts[0].point);

            if(IsNodeFlammable(col.contacts[0].point))
            {
                //Debug.Log ("Fire Spread Is Allowed");

                int i = Random.Range (0, 100);

                if(i <= fireSpreadChance)
                {
                    GameObject fire = Instantiate (fireGridObject, col.contacts[0].point, Quaternion.identity) as GameObject;
                    fire.GetComponent<FirePropagation> ().StartFirePropagation ();

                    fireSpreadAllowed = false;
                    Invoke ("EnableFireSpread", repeatedFireSpreadDelay);
                }
            }
        }
    }

    public DynamicNode GetNodeAtCoordinates(Vector3 coords)
    {
        try { return gridNodes[coords]; }
        catch { return null; }
    }

    public bool IsFlammable()
    {
        return isFlammable;
    }

    /// <summary>
    /// Check wheather current node overlaps any flammable or nonflammable objects
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    bool IsNodeFlammable(Vector3 position)
    {
        //Check if there are colliders within the node that are marked with flammable layers
        if (Physics.CheckSphere (position, 0.4f, flammableLayers))
        {
            //Check if there are colliders within the node that are marked with nonflammable layers
            if (Physics.CheckSphere (position, 0.4f, nonFlammableLayers))
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
    /// Set object off fire and not flammable anymore
    /// </summary>
    void SetOutOfFire()
    {

        canSetOnFire = false;
        canSpreadFire = false;
        isFlammable = false;
        fireSpreadAllowed = false;

        for (int i = 0; i < nodeObjs.Count; i++)
        {
            Destroy (nodeObjs[i].gameObject);
        }
        nodeObjs.Clear ();

        Invoke ("ResetComponent", timeBeforeFlammableAgain);
    }

    /// <summary>
    /// Resets component to be able to set on fire again
    /// </summary>
    void ResetComponent()
    {

        canSetOnFire = true;
        canSpreadFire = true;
        fireSpreadAllowed = false;
        nodesOnFire = 0;
        isFlammable = true;

        allNodesScripts.Clear ();
        nodesOnFireScripts.Clear ();
        gridNodes.Clear ();
    }

    void OnDrawGizmos()
    {
        if(enableDebug)
        {
            //Gizmos.DrawWireCube (transform.position, gridSize);

            if (allNodesScripts.Count > 0)
            {
                foreach (DynamicNode node in allNodesScripts)
                {
                    if (node.CanIgnite ())
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
                        else if (!node.IsOnFire ())
                        {
                            Gizmos.color = Color.green;
                        }
                    }

                    Gizmos.DrawWireCube (node.GetGlobalPos (), new Vector3 (nodeDiameter - 0f, nodeDiameter - 0f, nodeDiameter - 0f));
                }
            }
        }
    }

}
