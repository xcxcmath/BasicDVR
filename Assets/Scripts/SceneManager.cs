using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using System.IO;
public class SceneManager : MonoBehaviour
{
    enum DVRType
    {
        TF1D,
        TF2D
    }

    [SerializeField] private int sizeX = 256;
    [SerializeField] private int sizeY = 256;
    [SerializeField] private int sizeZ = 256;
    [SerializeField] private DataContentFormat dataFormat = DataContentFormat.Uint8;
    [SerializeField] private int bytesToSkip = 0;
    [SerializeField] private Object rawData = null;
    [SerializeField] private Object transferFunctionFile = null;
    [SerializeField] private Object transferFunction2DFile = null;
    [SerializeField] private DVRType _DVRType = DVRType.TF1D;
    [SerializeField] private bool renderAsScale = false;
    [HideInInspector] public VolumeRenderedObject obj = null;
    [SerializeField] public RenderTexture pickRenderTexture = null;
    private Object prevRawData = null;

    // for experiments
    [Tooltip("Visualization flag of blinking target")]
    [SerializeField] private bool blinkTarget = false;
    [Tooltip("UV-space coordinate that user should pick")]
    [SerializeField] private Vector3 targetPos = Vector3.zero;
    [SerializeField] private float targetSize = 0.04f;
    [SerializeField] private float pickSize = 0.06f;
    [SerializeField] private float raySize = 0.005f;
    [SerializeField] private Color targetColor = new Color(0.5f, 0.75f, 1.0f, 1.0f);
    [SerializeField] private Color pickColor = new Color(1.0f, 0.9f, 0.7f, 1.0f);
    [SerializeField] private Color rayColor = new Color(0.5f, 0.75f, 1.0f, 1.0f);
    [SerializeField] private bool showRay = false;

    // for picking
    private Texture2D pickTex;
    private Rect pickRect;
    private LookingGlass.Cursor3D cursor3D = null;
    private Vector4 cur_origin = new Vector4(0, 0, 0, 1);
    private Vector4 cur_dir = new Vector4(0, 0, 1, 0);

    private void OnEnable()
    {
        prevRawData = rawData;
    }

    private void OnValidate()
    {
        if ((prevRawData != null && prevRawData.Equals(rawData)) || rawData == null) return;
        Debug.Log("raw data change detected");
        prevRawData = rawData;

        // parse obj file name
        var filePath = AssetDatabase.GetAssetPath(rawData);
        var fileName = Path.GetFileName(filePath);
        var splitted = fileName.Split(new char[] { '.' });
        for(int i = 1; i < splitted.Length; ++i)
        {
            var here = splitted[i];
            if (here.StartsWith("skip"))
            {
                bytesToSkip = int.Parse(here.Substring(4));
                continue;
            }
            var xSplitted = here.Split(new char[] { 'x' });
            if(xSplitted.Length == 3)
            {
                sizeX = int.Parse(xSplitted[0]);
                sizeY = int.Parse(xSplitted[1]);
                sizeZ = int.Parse(xSplitted[2]);
                continue;
            }

            switch (here)
            {
                case "i8":
                    dataFormat = DataContentFormat.Int8;
                    break;
                case "i16":
                    dataFormat = DataContentFormat.Int16;
                    break;
                case "i32":
                    dataFormat = DataContentFormat.Int32;
                    break;
                case "u8":
                    dataFormat = DataContentFormat.Uint8;
                    break;
                case "u16":
                    dataFormat = DataContentFormat.Uint16;
                    break;
                case "u32":
                    dataFormat = DataContentFormat.Uint32;
                    break;
                default:
                    break;
            }
        }
    }

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
            obj = VolumeObjectFactory.CreateObject(dataset, tfFilePath, tf2DFilePath, _DVRType == DVRType.TF2D);
            switch (_DVRType)
            {
                case DVRType.TF1D:
                    obj.GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("TF2D_ON");
                    Debug.Log("TF2D is off");
                    break;
                case DVRType.TF2D:
                    obj.GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("TF2D_ON");
                    Debug.Log("TF2D is on");
                    break;
            }

            if (renderAsScale)
            {
                var biggestAxisLen = (float)Mathf.Max(sizeX, Mathf.Max(sizeY, sizeZ));
                transform.localScale = (new Vector3(sizeX, sizeY, sizeZ)) / biggestAxisLen;
                Debug.Log(transform.localScale);
            }
        }
        else
        {
            Debug.LogError("Failed to import dataset");
        }
        pickTex = new Texture2D(1, 1);
        pickRect = new Rect(0, 0,1,1);
        cursor3D = FindObjectOfType<LookingGlass.Cursor3D>();

        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickingPos", Vector3.zero);
    }

    void Update()
    {
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.localScale;
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_BlinkTarget", blinkTarget ? 1 : 0);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_TargetPos", targetPos);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_TargetSize", targetSize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_PickSize", pickSize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_RaySize", raySize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_TargetColor", targetColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickColor", pickColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_RayColor", rayColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_ShowRay", showRay ? 1 : 0);
        var pickToObj = transform.worldToLocalMatrix * cursor3D.transform.localToWorldMatrix;
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickRayOrigin", pickToObj * cur_origin);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickRayDir", pickToObj * cur_dir);

        if(pickRenderTexture != null && Input.GetMouseButtonDown(0))
        {
            RenderTexture.active = pickRenderTexture;
            pickTex.ReadPixels(pickRect, 0, 0);
            pickTex.Apply();
            RenderTexture.active = null;
            var picked = pickTex.GetPixel(0, 0);
            obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickingPos", picked);
            Debug.Log(Time.time.ToString() + " " + picked.ToString());
        }
    }
}
