using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeRendering : MonoBehaviour
{
    [SerializeField] protected Shader shader;
    protected Material material;
    public Texture volume;
    [HideInInspector] public TransferFunction transferFunction;

    void OnEnable()
    {
        transferFunction = new TransferFunction();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        material = new Material(shader);
        GetComponent<MeshFilter>().sharedMesh = Build();
        GetComponent<MeshRenderer>().sharedMaterial = material;
        
        material.SetTexture("_Volume", volume);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    Mesh Build()
    {
        var mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
        };
        mesh.triangles = new int[]
        {
            0, 2, 1,
            0, 3, 2,
            2, 3, 4,
            2, 4, 5,
            1, 2, 5,
            1, 5, 6,
            0, 7, 4,
            0, 4, 3,
            5, 4, 7,
            5, 7, 6,
            0, 6, 7,
            0, 1, 6
        };

        mesh.RecalculateNormals();
        mesh.hideFlags = HideFlags.HideAndDontSave;

        return mesh;
    }

    void OnDestroy()
    {
        Destroy(material);
    }
}
