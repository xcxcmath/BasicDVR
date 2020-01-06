using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private int sizeX = 256;
    [SerializeField] private int sizeY = 256;
    [SerializeField] private int sizeZ = 256;
    [SerializeField] private DataContentFormat dataFormat = DataContentFormat.Uint8;
    [SerializeField] private int bytesToSkip = 0;
    [SerializeField] private Object rawData = null;
    [SerializeField] private Object transferFunctionFile = null;
    [SerializeField] private Object transferFunction2DFile = null;
    private VolumeRenderedObject obj = null;
    private Vector3 rotateAxis = new Vector3(0, 0, 1);

    // Start is called before the first frame update
    void Start()
    {
        if (rawData == null)
        {
            Debug.Log("No raw data is set");
            return;
        }
        var filePath = AssetDatabase.GetAssetPath(rawData);
        DatasetImporterBase importer = new RawDatasetImporter(filePath, sizeX, sizeY, sizeZ, dataFormat, bytesToSkip);
        var dataset = importer.Import();
        if (dataset != null)
        {
            string tfFilePath = null;
            string tf2DFilePath = null;
            if (transferFunctionFile != null)
            {
                tfFilePath = AssetDatabase.GetAssetPath(transferFunctionFile);
            }

            if (transferFunction2DFile != null)
            {
                tf2DFilePath = AssetDatabase.GetAssetPath(transferFunction2DFile);
            }
            obj = VolumeObjectFactory.CreateObject(dataset, tfFilePath, tf2DFilePath);
        }
        else
        {
            Debug.LogError("Failed to import dataset");
        }
    }

    void Update()
    {
        transform.Rotate(rotateAxis, Time.deltaTime * 20.0f);
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.localScale;
    }
}
