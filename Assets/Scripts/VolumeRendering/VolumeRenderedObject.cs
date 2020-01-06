using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class VolumeRenderedObject : MonoBehaviour
{
    [HideInInspector] public TransferFunction transferFunction;
    [HideInInspector] public TransferFunction2D transferFunction2D;
    [HideInInspector] public VolumeDataset dataset;
    [HideInInspector] [NotNull] public string tfFilePath = "Assets/Resources/TF.raw";
    [HideInInspector] [NotNull] public string tf2DFilePath = "Assets/Resources/TF2D.raw";

    void Start()
    {
        GetComponent<MeshFilter>().sharedMesh = Build();
    }

    Mesh Build()
    {
        var mesh = new Mesh
        {
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
            },
            triangles = new[]
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
            }
        };


        mesh.RecalculateNormals();
        mesh.hideFlags = HideFlags.HideAndDontSave;

        return mesh;
    }
}
