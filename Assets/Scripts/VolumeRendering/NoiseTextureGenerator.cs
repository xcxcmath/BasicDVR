using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTextureGenerator
{
    public static Texture2D GenerateNoiseTexture(int sizeX, int sizeY)
    {
        var noiseTexture = new Texture2D(sizeX, sizeY);
        var noiseColors = new Color[sizeX * sizeY];
        for (int j = 0; j < sizeY; ++j)
        {
            for (int i = 0; i < sizeX; ++i)
            {
                float pixVal = Random.Range(0.0f, 1.0f);
                noiseColors[i + j * sizeX] = new Color(pixVal, pixVal, pixVal);
            }
        }
        
        noiseTexture.SetPixels(noiseColors);
        noiseTexture.Apply();
        return noiseTexture;
    }
}
