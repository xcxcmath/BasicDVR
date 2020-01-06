using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TransferFunction
{
    public List<TFColorPoint> colorPoints = new List<TFColorPoint>();
    public List<TFAlphaPoint> alphaPoints = new List<TFAlphaPoint>();
    
    public Texture2D histogramTexture = null;

    private Texture2D texture = null;
    private Color[] tfColors;
    private const int TEXTURE_WIDTH = 256;
    private const int TEXTURE_HEIGHT = 1;

    public TransferFunction()
    {
        texture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT);
        tfColors = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT];
    }

    public void AddControlPoint(TFColorPoint ctrlPoint)
    {
        colorPoints.Add(ctrlPoint);
    }

    public void AddControlPoint(TFAlphaPoint ctrlPoint)
    {
        alphaPoints.Add(ctrlPoint);
    }
    
    public Texture2D GetTexture()
    {
        if(texture == null)
            GenerateTexture();

        return texture;
    }

    public void GenerateTexture()
    {
        var colors = new List<TFColorPoint>(colorPoints);
        var alphas = new List<TFAlphaPoint>(alphaPoints);
        
        colors.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));
        alphas.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));
        
        if (colors.Count == 0 || colors[colors.Count - 1].dataValue < 1.0f)
            colors.Add(new TFColorPoint(1.0f, Color.white));
        if(colors[0].dataValue > 0.0f)
            colors.Insert(0, new TFColorPoint(0.0f, Color.white));
        
        if(alphas.Count == 0 || alphas[alphas.Count - 1].dataValue < 1.0f)
            alphas.Add(new TFAlphaPoint(1.0f, 1.0f));
        if(alphas[0].dataValue > 0.0f)
            alphas.Insert(0, new TFAlphaPoint(0.0f, 0.0f));

        int numColors = colors.Count;
        int numAlphas = alphas.Count;
        int colorIdx = 0, alphaIdx = 0;
        
        for (int i = 0; i < TEXTURE_WIDTH; ++i)
        {
            float t = i / (float) (TEXTURE_WIDTH - 1);
            while (colorIdx < numColors - 2 && colors[colorIdx + 1].dataValue < t)
                ++colorIdx;
            while (alphaIdx < numAlphas - 2 && alphas[alphaIdx + 1].dataValue < t)
                ++alphaIdx;
            
            
            var leftColor = colors[colorIdx];
            var leftColorDV = leftColor.dataValue;
            var rightColor = colors[colorIdx + 1];
            var rightColorDV = rightColor.dataValue;
            var leftAlpha = alphas[alphaIdx];
            var leftAlphaDV = leftAlpha.dataValue;
            var rightAlpha = alphas[alphaIdx + 1];
            var rightAlphaDV = rightAlpha.dataValue;
            
            // get pixel color with linear interpolation
            float tColor = (Mathf.Clamp(t, leftColorDV, rightColorDV) - leftColorDV)
                           / (rightColorDV - leftColorDV);
            float tAlpha = (Mathf.Clamp(t, leftAlphaDV, rightAlphaDV) - leftAlphaDV)
                           / (rightAlphaDV - leftAlphaDV);
            Color pixColor = rightColor.colorValue * tColor + leftColor.colorValue * (1.0f - tColor);
            pixColor.a = rightAlpha.alphaValue * tAlpha + leftAlpha.alphaValue * (1.0f - tAlpha);
            for (int j = 0; j < TEXTURE_HEIGHT; ++j)
            {
                tfColors[i + j * TEXTURE_WIDTH] = pixColor;
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(tfColors);
        texture.Apply();
    }

    // file format
    // int32:szColor
    // int32:szAlpha
    // floats are stored as Single
    // colors are stored in order of (D)RGB floats
    // alphas are stored in order of (D)A
    
    public static TransferFunction ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File doesn't exist : " + filePath);
            return null;
        }
        
        var tf = new TransferFunction();

        using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            int szColor = reader.ReadInt32();
            int szAlpha = reader.ReadInt32();
            for (int i = 0; i < szColor; ++i)
            {
                var dataValue = reader.ReadSingle();
                var r = reader.ReadSingle();
                var g = reader.ReadSingle();
                var b = reader.ReadSingle();
                tf.AddControlPoint(new TFColorPoint(dataValue, new Color(r, g, b, 1.0f)));
            }

            for (int i = 0; i < szAlpha; ++i)
            {
                var dataValue = reader.ReadSingle();
                var alphaValue = reader.ReadSingle();
                tf.AddControlPoint(new TFAlphaPoint(dataValue, alphaValue));
            }
        }

        return tf;
    }

    public static void WriteToFile(TransferFunction tf, string filePath)
    {
        using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        {
            writer.Write(tf.colorPoints.Count);
            writer.Write(tf.alphaPoints.Count);
            foreach(var col in tf.colorPoints)
            {
                writer.Write(col.dataValue);
                writer.Write(col.colorValue.r);
                writer.Write(col.colorValue.g);
                writer.Write(col.colorValue.b);
            }

            foreach (var alp in tf.alphaPoints)
            {
                writer.Write(alp.dataValue);
                writer.Write(alp.alphaValue);
            }
        }
    }
}
