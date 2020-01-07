using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingCamera : MonoBehaviour
{
    SceneManager manager = null;
    public bool isPicking = false;

    void Start()
    {
        manager = FindObjectOfType<SceneManager>();
    }
    void OnPreRender()
    {
        if (manager == null || manager.obj == null) return;
        //manager.obj.GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword(isPicking ? "WYSIWYP_ON" : "WYSIWYP_OFF");
        manager.obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_IsPicking", isPicking ? 1 : 0);
    }
}
