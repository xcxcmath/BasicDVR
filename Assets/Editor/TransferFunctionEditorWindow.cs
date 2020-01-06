using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TransferFunctionEditorWindow : EditorWindow
{
    private TransferFunction tf = null;
    private int movColorIdx = -1, selColorIdx = -1;
    private int movAlphaIdx = -1;
    private Material tfGUIMat = null;
    private Material tfPaletteGUIMat = null;

    [MenuItem("Volume Rendering/1D Transfer Function")]
    static void ShowWindow()
    {
        var wnd2D = (TransferFunction2DEditorWindow) EditorWindow.GetWindow(typeof(TransferFunction2DEditorWindow));
        if (wnd2D != null)
        {
            wnd2D.Close();
        }
        var wnd = (TransferFunctionEditorWindow)EditorWindow.GetWindow(typeof(TransferFunctionEditorWindow));
        wnd.Show();
    }

    private void OnEnable()
    {
        tfGUIMat = Resources.Load<Material>("TransferFunctionGUIMat");
        tfPaletteGUIMat = Resources.Load<Material>("TransferFunctionPaletteGUIMat");
    }

    private void OnGUI()
    {
        var vol = FindObjectOfType<VolumeRenderedObject>();
        if (vol == null) return;
        tf = vol.transferFunction;
        if (tf == null) return;

        Color oldColor = GUI.color;
        float bgWidth = Mathf.Min(this.position.width - 20.0f, (this.position.height - 50.0f) * 2.0f);
        Rect bgRect = new Rect(0.0f, 0.0f, bgWidth, bgWidth * 0.5f);
        
        tf.GenerateTexture();
        
        tfGUIMat.SetTexture("_TFTex", tf.GetTexture());
        tfGUIMat.SetTexture("_HistTex", tf.histogramTexture);
        Graphics.DrawTexture(bgRect, tf.GetTexture(), tfGUIMat);

        Texture2D tfTexture = tf.GetTexture();
        
        tfPaletteGUIMat.SetTexture("_TFTex", tf.GetTexture());
        Graphics.DrawTexture(new Rect(bgRect.x, bgRect.y + bgRect.height+20, bgRect.width, 20.0f), tfTexture, tfPaletteGUIMat);

        if (GUI.Button(new Rect(bgRect.x + 100, bgRect.y + bgRect.height + 50, 150, 40), "Build into file"))
        {
            TransferFunction.WriteToFile(tf, vol.tfFilePath);
        }
        
        for (int i = 0; i < tf.colorPoints.Count; ++i)
        {
            var it = tf.colorPoints[i];
            Rect ctrlBox = new Rect(bgRect.x + bgRect.width * it.dataValue, bgRect.y + bgRect.height + 20, 10, 20);
            GUI.color = Color.red;
            GUI.skin.box.fontSize = 8;
            GUI.Box(ctrlBox, "|");
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                ctrlBox.Contains(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y)))
            {
                movColorIdx = i;
                selColorIdx = i;
            } else if (movColorIdx == i)
            {
                it.dataValue = Mathf.Clamp((Event.current.mousePosition.x - bgRect.x) / bgRect.width, 0.0f, 1.0f);
            }

            tf.colorPoints[i] = it;
        }

        for (int i = 0; i < tf.alphaPoints.Count; ++i)
        {
            var it = tf.alphaPoints[i];
            Rect ctrlBox = new Rect(bgRect.x + bgRect.width * it.dataValue, bgRect.y + (1.0f - it.alphaValue) * bgRect.height, 10, 10);
            GUI.color = oldColor;
            GUI.skin.box.fontSize = 6;
            GUI.Box(ctrlBox, "a");
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && ctrlBox.Contains(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y)))
            {
                movAlphaIdx = i;
            }
            else if (movAlphaIdx == i)
            {
                it.dataValue = Mathf.Clamp((Event.current.mousePosition.x - bgRect.x) / bgRect.width, 0.0f, 1.0f);
                it.alphaValue = Mathf.Clamp(1.0f - (Event.current.mousePosition.y - bgRect.y) / bgRect.height, 0.0f, 1.0f);
            }
            tf.alphaPoints[i] = it;
        }

        if (Event.current.type == EventType.MouseUp)
        {
            movColorIdx = movAlphaIdx = -1;
        }

        if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            if (bgRect.Contains(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y)))
                tf.alphaPoints.Add(new TFAlphaPoint(Mathf.Clamp((Event.current.mousePosition.x - bgRect.x) / bgRect.width, 0.0f, 1.0f), Mathf.Clamp(1.0f - (Event.current.mousePosition.y - bgRect.y) / bgRect.height, 0.0f, 1.0f)));
            else
                tf.colorPoints.Add(new TFColorPoint(Mathf.Clamp((Event.current.mousePosition.x - bgRect.x) / bgRect.width, 0.0f, 1.0f), Random.ColorHSV()));
            selColorIdx = -1;
        }

        if (selColorIdx != -1)
        {
            var it = tf.colorPoints[selColorIdx];
            it.colorValue = EditorGUI.ColorField(new Rect(bgRect.x, bgRect.y + bgRect.height + 50, 100.0f, 40.0f), it.colorValue);
            tf.colorPoints[selColorIdx] = it;
        }
        
        vol.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_TFTex", tfTexture);
        vol.GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("TF2D_ON");

        GUI.color = oldColor;
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }
}
