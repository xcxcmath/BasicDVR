using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TransferFunction2DEditorWindow : EditorWindow
{
    private Texture2D hist2DTex = null;
    private bool needsRegenTexture = true;
    private Material tfGUIMat = null;
    private int selectedBoxIndex = -1;

    [MenuItem("Volume Rendering/2D Transfer Function")]
    static void ShowWindow()
    {
        var wnd1D = (TransferFunctionEditorWindow) EditorWindow.GetWindow(typeof(TransferFunctionEditorWindow));
        if (wnd1D != null)
        {
            wnd1D.Close();
        }

        var wnd = (TransferFunction2DEditorWindow) EditorWindow.GetWindow(typeof(TransferFunction2DEditorWindow));
        wnd.Show();
    }

    private void OnEnable()
    {
        tfGUIMat = Resources.Load<Material>("TransferFunction2DGUIMat");
    }

    private void OnGUI()
    {
        var vol = FindObjectOfType<VolumeRenderedObject>();
        if (vol == null) return;
        if (hist2DTex == null)
        {
            hist2DTex = HistogramTextureGenerator.Generate2DHistogramTexture(vol.dataset);
        }

        var tf2d = vol.transferFunction2D;

        float bgWidth = Mathf.Min(this.position.width - 20.0f, (this.position.height - 250.0f) * 2.0f);
        var histRect = new Rect(0.0f, 0.0f, bgWidth, bgWidth * 0.5f);
        Graphics.DrawTexture(histRect, hist2DTex);
        tfGUIMat.SetTexture("_TFTex", tf2d.GetTexture());
        Graphics.DrawTexture(histRect, tf2d.GetTexture(), tfGUIMat);

        for (int i = 0; i < tf2d.boxes.Count; ++i)
        {
            TransferFunction2D.TF2DBox box = tf2d.boxes[i];
            Rect boxRect = new Rect(histRect.x + box.rect.x * histRect.width, histRect.y + (1.0f - box.rect.height - box.rect.y) * histRect.height, box.rect.width * histRect.width, box.rect.height * histRect.height);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && boxRect.Contains(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y)))
            {
                selectedBoxIndex = i;
                needsRegenTexture = true;
            }
        }

        float startX = histRect.x;
        float startY = histRect.y + histRect.height + 10;
        if (selectedBoxIndex != -1)
        {
            EditorGUI.BeginChangeCheck();
            var box = tf2d.boxes[selectedBoxIndex];
            float oldX = box.rect.x;
            float oldY = box.rect.y;
            box.rect.x = EditorGUI.Slider(new Rect(startX, startY, 200.0f, 20.0f), "x", box.rect.x, 0.0f, 0.99f);
            box.rect.width = EditorGUI.Slider(new Rect(startX + 220.0f, startY, 200.0f, 20.0f), "width", oldX + box.rect.width, box.rect.x + 0.01f, 1.0f) - box.rect.x;
            box.rect.y = EditorGUI.Slider(new Rect(startX, startY + 50, 200.0f, 20.0f), "y", box.rect.y, 0.0f, 1.0f);
            box.rect.height = EditorGUI.Slider(new Rect(startX + 220.0f, startY + 50, 200.0f, 20.0f), "height", oldY + box.rect.height, box.rect.y + 0.01f, 1.0f) - box.rect.y;
            box.color = EditorGUI.ColorField(new Rect(startX + 450.0f, startY + 10, 100.0f, 20.0f), box.color);
            box.minAlpha = EditorGUI.Slider(new Rect(startX + 450.0f, startY + 30, 200.0f, 20.0f), "min alpha", box.minAlpha, 0.0f, 1.0f);
            box.maxAlpha = EditorGUI.Slider(new Rect(startX + 450.0f, startY + 60, 200.0f, 20.0f), "max alpha", box.maxAlpha, 0.0f, 1.0f);
            tf2d.boxes[selectedBoxIndex] = box;
            needsRegenTexture |= EditorGUI.EndChangeCheck();
        }
        else
        {
            EditorGUI.LabelField(new Rect(startX, startY, this.position.width - startX, 50.0f), "Select a rectangle in the above view, or add a new one.");
        }
        
        // Add new rectangle
        if (GUI.Button(new Rect(startX, startY + 100, 150.0f, 40.0f), "Add rectangle"))
        {
            tf2d.AddBox(0.1f, 0.1f, 0.8f, 0.8f, Color.white, 0.5f, 0.5f);
            selectedBoxIndex = tf2d.boxes.Count - 1;
            needsRegenTexture = true;
        }
        // Remove selected shape
        if (selectedBoxIndex != -1)
        {
            if (GUI.Button(new Rect(startX, startY + 150, 150.0f, 40.0f), "Remove selected shape"))
            {
                tf2d.boxes.RemoveAt(selectedBoxIndex);
                selectedBoxIndex = -1;
                needsRegenTexture = true;
            }
        }

        // TODO: regenerate on add/remove/modify (and do it async)
        if (needsRegenTexture)
        {
            tf2d.GenerateTexture();
            needsRegenTexture = false;
        }

        if (GUI.Button(new Rect(startX + 170, startY + 100, 150.0f, 40.0f), "Build into file"))
        {
            TransferFunction2D.WriteToFile(tf2d, vol.tf2DFilePath);
        }
        vol.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_TFTex", tf2d.GetTexture());
        vol.GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("TF2D_ON");
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}
