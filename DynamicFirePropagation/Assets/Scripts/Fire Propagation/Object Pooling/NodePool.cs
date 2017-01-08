using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NodePool : MonoBehaviour
{
    [Header ("Settings")]
    public int nodesToCreateAtStart = 0;
    public GameObject nodePrefab;
    public GameObject fireGridPrefab;
    public GameObject nodeStorage;

    private Transform nodesInPoolParent;
    //private Transform nodesInUseParent;

    private float colliderSize = 0;
    private float minusSize = 0;

    private List<GameObject> nodes = new List<GameObject> ();

    void Awake()
    {
        //Check if all objects referenced in inspector
        if(!nodePrefab)
        {
            Debug.LogError ("No Node GameObject Prefab Referenced in Inspector!");
        }
        if (!fireGridPrefab)
        {
            Debug.LogError ("No FireGrid GameObject Prefab Referenced in Inspector!");
        }
        if (!nodeStorage)
        {
            Debug.LogError ("No Node Storage GameObject Prefab Referenced in Inspector!");
        }

        GameObject nodeStorageClone = Instantiate (nodeStorage, Vector3.zero, Quaternion.identity) as GameObject;
        nodesInPoolParent = nodeStorageClone.transform.FindChild ("Pooled Nodes");
        //nodesInUseParent = nodeStorageClone.transform.FindChild ("In Use Nodes");

        //Get collider size of nodes, and reduce by a small amount to not overlap with neighbour nodes
        colliderSize = fireGridPrefab.GetComponent<FirePropagation> ().nodeDiameter;
        minusSize = colliderSize / 10;
        colliderSize -= minusSize;

        //Create a defined amount of nodes to be ready for use at start of the game
        for (int i = 0; i < nodesToCreateAtStart; i++)
        {
            GameObject newNode = Instantiate (nodePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            nodes.Add (newNode);
            newNode.transform.localScale = new Vector3 (colliderSize, colliderSize, colliderSize);
            newNode.transform.parent = nodesInPoolParent;
        }
    }

    /// <summary>
    /// Get a node GameObject from a nodes pool
    /// </summary>
    /// <returns></returns>
    public GameObject GetNodeGameObjectFromPool(Vector3 position)
    {
        GameObject node;

        //If no node gameobjects in current nodes list, create new node gameobjects
        if(nodes.Count <= 0)
        {
            GameObject newNode = Instantiate (nodePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            nodes.Add (newNode);
            newNode.transform.localScale = new Vector3 (colliderSize, colliderSize, colliderSize);
            newNode.transform.parent = nodesInPoolParent;
            //Debug.Log ("Created new node object for pool");
        }

        //Get last node from the list
        node = nodes.Last (); //nodes[nodes.Count - 1];

        //Set the world position of the node object
        node.transform.position = position;

        //Remove this node from the list while is being used in a fire propagation simulation
        nodes.RemoveAt(nodes.Count - 1);
        //node.transform.parent = nodesInUseParent;

        return node;
    }

    /// <summary>
    /// Return a node GameObject to the node pool
    /// </summary>
    public void ReturnNodeGameObjectToPool(GameObject node)
    {
        nodes.Add (node);
        node.SetActive (false);
        //node.transform.parent = nodesInPoolParent;
    }
	
}
