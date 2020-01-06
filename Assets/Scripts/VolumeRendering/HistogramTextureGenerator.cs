using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistogramTextureGenerator
{
    public static Texture2D GenerateHistogramTexture(VolumeDataset dataset)
    {
        int numSamples = dataset.maxDataValue + 1;
        int[] values = new int[numSamples];
        Color[] colors = new Color[numSamples];
        Texture2D texture = new Texture2D(numSamples, 1, TextureFormat.RGBAFloat, false);

        int maxFreq = 0;
        for (int i = 0; i < dataset.data.Length; ++i)
        {
            values[dataset.data[i]] += 1;
            maxFreq = Math.Max(values[dataset.data[i]], maxFreq);
        }

        for (int i = 0; i < numSamples; ++i)
        {
            colors[i] = new Color(Mathf.Log10((float)values[i]) / Mathf.Log10((float)maxFreq), 0.0f, 0.0f, 1.0f);
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public static Texture2D Generate2DHistogramTexture(VolumeDataset dataset)
    {
        int sz = dataset.maxDataValue + 1;
        int szGrad = 256;
        var colors = new Color[sz * szGrad];
        var texture = new Texture2D(sz, szGrad);

        for (int i = 0; i < colors.Length; ++i)
        {
            colors[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }

        int maxRange = dataset.maxDataValue - dataset.minDataValue;
        const float maxNormalMag = 1.75f;

        for (int z = 1; z < dataset.sizeZ - 1; ++z)
        {
            for (int y = 1; y < dataset.sizeY - 1; ++y)
            {
                for (int x = 1; x < dataset.sizeX - 1; ++x)
                {
                    int idx = x + dataset.sizeX * (y + z * dataset.sizeY);
                    int density = dataset.data[idx];
                    
                    int x1 = dataset.data[(x + 1) + y * dataset.sizeX + z * (dataset.sizeX * dataset.sizeY)];
                    int x2 = dataset.data[(x - 1) + y * dataset.sizeX + z * (dataset.sizeX * dataset.sizeY)];
                    int y1 = dataset.data[x + (y + 1) * dataset.sizeX + z * (dataset.sizeX * dataset.sizeY)];
                    int y2 = dataset.data[x + (y - 1) * dataset.sizeX + z * (dataset.sizeX * dataset.sizeY)];
                    int z1 = dataset.data[x + y * dataset.sizeX + (z + 1) * (dataset.sizeX * dataset.sizeY)];
                    int z2 = dataset.data[x + y * dataset.sizeX + (z - 1) * (dataset.sizeX * dataset.sizeY)];

                    Vector3 grad = new Vector3((x2 - x1) / (float)maxRange, (y2 - y1) / (float)maxRange, (z2 - z1) / (float)maxRange);
                    var colIdx = density + (int) (grad.magnitude * szGrad / maxNormalMag) * sz;
                    colors[colIdx] = Color.white;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }
}
