using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    struct PickPoint
    {
        float time;
        Color point;
        string message;
        public PickPoint(float time, Color point, string message)
        {
            this.time = time;
            this.point = point;
            this.message = message;
        }
        override public string ToString()
        {
            return $"{time.ToString()} {point.r} {point.g} {point.b} {message}";
        }
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
    private Object prevRawData = null;

    // for experiments
    [SerializeField] private Object targetData = null;
    [SerializeField] private float targetEpsilon = 0.02f;
    private List<Color> targetPositions = null;
    private int targetIndex = -1;
    private string targetIndexMessage = "Ready";
    [SerializeField] private GameObject targetMessageObject = null;
    [Tooltip("Visualization flag of blinking target")]
    [SerializeField] private bool blinkTarget = false;
    [SerializeField] private float targetSize = 0.04f;
    [SerializeField] private float pickSize = 0.06f;
    [SerializeField] private float raySize = 0.005f;
    [SerializeField] private Color targetColor = new Color(0.5f, 0.75f, 1.0f, 1.0f);
    [SerializeField] private Color pickColor = new Color(1.0f, 0.9f, 0.7f, 1.0f);
    [SerializeField] private Color rayColor = new Color(0.5f, 0.75f, 1.0f, 1.0f);
    [SerializeField] private bool showRay = false;
    [SerializeField] private string resultPath = "Assets/Results/result.txt";
    private List<PickPoint> pickPoints = null;
    private float startTime = 0.0f;
    private bool isStartMoving = false;
    

    // for picking
    [SerializeField] private RenderTexture pickRenderTexture = null;
    private RenderTexture pickRenderTexture2D = null;
    private Texture2D pickTex = null, pickTex2D = null;
    private Rect pickRect, pickRect2D;
    private LookingGlass.Cursor3D cursor3D = null;
    private Vector4 cur_origin = new Vector4(0, 0, 0, 1);
    private Vector4 cur_dir = new Vector4(0, 0, 1, 0);
    [SerializeField] private GameObject cursor2D = null;
    [SerializeField] private bool is2DPicking = false;

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
        if(cursor2D != null)
        {
            pickRenderTexture2D = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 16, RenderTextureFormat.ARGB32);
            cursor2D.GetComponent<Camera>().targetTexture = pickRenderTexture2D;
            pickTex2D = new Texture2D(Camera.main.pixelWidth, Camera.main.pixelHeight);
            pickRect2D = new Rect(0, 0, Camera.main.pixelWidth, Camera.main.pixelHeight);
        }
        pickTex = new Texture2D(1, 1);
        pickRect = new Rect(0, 0, 1, 1);

        cursor3D = FindObjectOfType<LookingGlass.Cursor3D>();

        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickingPos", Vector3.zero);

        pickPoints = new List<PickPoint>();

        if (is2DPicking)
        {
            cursor3D.enabled = false;
            cursor3D.transform.position = new Vector3(-3, -3, 0);
        }

        // experiment settings (targets)
        if(targetData != null)
        {
            targetIndex = -1;
            targetPositions = new List<Color>();
            var targetDataFilePath = AssetDatabase.GetAssetPath(targetData);
            using(var sr = new StreamReader(File.Open(targetDataFilePath, System.IO.FileMode.Open)))
            {
                int targetCount = int.Parse(sr.ReadLine());
                for(var i = 0; i < targetCount; ++i)
                {
                    var lineList = sr.ReadLine().Trim().Split(' ').ToList().ConvertAll(float.Parse);
                    if(lineList.Count == 3)
                    {
                        targetPositions.Add(new Color(lineList[0], lineList[1], lineList[2], 1.0f));
                    }
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!isStartMoving)
            {
                isStartMoving = true;
                startTime = Time.time;
                targetIndex = 0;
                targetIndexMessage = "1";
            }
            else
            {
                Color picked = new Color(0, 0, 0);
                if (is2DPicking)
                {
                    RenderTexture.active = pickRenderTexture2D;
                    pickTex2D.ReadPixels(pickRect2D, 0, 0);
                    pickTex2D.Apply();
                    RenderTexture.active = null;
                    var mousePos = Input.mousePosition;
                    picked = pickTex2D.GetPixel((int)mousePos.x, (int)mousePos.y);
                }
                else
                {
                    RenderTexture.active = pickRenderTexture;
                    pickTex.ReadPixels(pickRect, 0, 0);
                    pickTex.Apply();
                    RenderTexture.active = null;
                    picked = pickTex.GetPixel(0, 0);
                }
                obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickingPos", picked);

                // do experiments
                string message = "";
                if (targetPositions != null && targetIndex >= 0 && targetIndex < targetPositions.Count)
                {
                    var ct = targetPositions[targetIndex];
                    var vp = new Vector3(picked.r, picked.g, picked.b);
                    var vt = new Vector3(ct.r, ct.g, ct.b);
                    if ((vp - vt).magnitude < targetEpsilon)
                    {
                        message = $"{targetIndex}";
                        targetIndex += 1;
                        targetIndexMessage = (targetIndex + 1).ToString();
                        if (targetIndex == targetPositions.Count)
                        {
                            targetIndexMessage = "Finished";
                        }
                    }
                }

                var pp = new PickPoint(Time.time, picked, message);
                pickPoints.Add(pp);
                Debug.Log(pp.ToString());
            }
        }
        targetMessageObject.GetComponent<TextMesh>().text = targetIndexMessage;

        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.localScale;
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_BlinkTarget", blinkTarget ? 1 : 0);
        if (targetPositions != null && targetIndex >= 0 && targetIndex < targetPositions.Count)
        {
            obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_TargetPos", targetPositions[targetIndex]);
        }
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_TargetSize", targetSize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_PickSize", pickSize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_RaySize", raySize);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_TargetColor", targetColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickColor", pickColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_RayColor", rayColor);
        obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_ShowRay", showRay ? 1 : 0);
        if (cursor3D != null)
        {
            var pickToObj = transform.worldToLocalMatrix * cursor3D.transform.localToWorldMatrix;
            obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickRayOrigin", pickToObj * cur_origin);
            obj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_PickRayDir", pickToObj * cur_dir);
        }
    }
    private void OnApplicationQuit()
    {
        using(var sw = new StreamWriter(File.Open(resultPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        {
            sw.WriteLine(pickPoints.Count.ToString());
            sw.WriteLine(startTime);
            foreach(var pp in pickPoints)
            {
                sw.WriteLine(pp.ToString());
            }
            sw.Flush();
            Debug.Log($"Results are stored in {resultPath}");
        }
    }
}
