using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TransferFunction2D
{
    public struct TF2DBox
    {
        public Color color;
        public float maxAlpha;
        public float minAlpha;
        public Rect rect;
    }

    public List<TF2DBox> boxes = new List<TF2DBox>();

    private Texture2D texture = null;

    private const int TEXTURE_WIDTH = 512;
    private const int TEXTURE_HEIGHT = 512;

    public TransferFunction2D()
    {
        texture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT);
    }

    public void AddBox(float x, float y, float width, float height, Color color, float minAlpha, float maxAlpha)
    {
        var box = new TF2DBox();
        box.rect.x = x;
        box.rect.y = y;
        box.rect.width = width;
        box.rect.height = height;
        box.color = color;
        box.minAlpha = minAlpha;
        box.maxAlpha = maxAlpha;
        boxes.Add(box);
    }

    public Texture2D GetTexture()
    {
        return texture;
    }

    public void GenerateTexture()
    {
        var colors = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT];
        for (int i = 0; i < TEXTURE_WIDTH; ++i)
        {
            for (int j = 0; j < TEXTURE_HEIGHT; ++j)
            {
                colors[i + j * TEXTURE_WIDTH] = Color.clear;
                foreach (var box in boxes)
                {
                    if (box.rect.Contains(new Vector2(i / (float) TEXTURE_WIDTH, j / (float) TEXTURE_HEIGHT)))
                    {
                        float x = i / (float) TEXTURE_WIDTH;
                        float alpha = Mathf.Lerp(box.maxAlpha, box.minAlpha, Mathf.Abs(box.rect.x + box.rect.width * 0.5f - x) * 2.0f);
                        colors[i + j * TEXTURE_WIDTH] = new Color(box.color.r, box.color.g, box.color.b, alpha);
                    }
                }
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();
    }

    // TransferFunction2D file format
    // color : RGB Singles
    // minAlpha : Single
    // maxAlpha : Single
    // rect : XYWH Singles

    public static TransferFunction2D ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File doesn't exist : " + filePath);
            return null;
        }
        
        var tf2D = new TransferFunction2D();

        using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            int szBox = reader.ReadInt32();
            for (int i = 0; i < szBox; ++i)
            {
                var r = reader.ReadSingle();
                var g = reader.ReadSingle();
                var b = reader.ReadSingle();
                var minAlpha = reader.ReadSingle();
                var maxAlpha = reader.ReadSingle();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var w = reader.ReadSingle();
                var h = reader.ReadSingle();
                tf2D.AddBox(x, y, w, h, new Color(r, g, b), minAlpha, maxAlpha);
            }
        }

        return tf2D;
    }
    
    public static void WriteToFile(TransferFunction2D tf2D, string filePath)
    {
        using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        {
            writer.Write(tf2D.boxes.Count);
            foreach(var box in tf2D.boxes)
            {
                writer.Write(box.color.r);
                writer.Write(box.color.g);
                writer.Write(box.color.b);
                writer.Write(box.minAlpha);
                writer.Write(box.maxAlpha);
                writer.Write(box.rect.x);
                writer.Write(box.rect.y);
                writer.Write(box.rect.width);
                writer.Write(box.rect.height);
            }
        }
    }
}
