using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class VolumeObjectFactory
{
    public static VolumeRenderedObject CreateObject(VolumeDataset dataset, string tfFilePath, string tf2DFilePath, bool enable2D)
    {
        var obj = Object.Instantiate(Resources.Load("VolumeRenderedObject", typeof(GameObject))) as GameObject;
        
        var volObj = obj.GetComponent<VolumeRenderedObject>();
        var meshRenderer = obj.GetComponent<MeshRenderer>();

        volObj.dataset = dataset;

        var sizeX = dataset.sizeX;
        var sizeY = dataset.sizeY;
        var sizeZ = dataset.sizeZ;

        int maxRange = dataset.maxDataValue - dataset.minDataValue;

        var colors = new Color[dataset.data.Length];
        for (int z = 0; z < sizeZ; ++z)
        {
            for (int y = 0; y < sizeY; ++y)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    int idx = x + sizeX * (y + z * sizeY);
                    
                    // grad
                    int x_up = Math.Min(x + 1, sizeX - 1);
                    int x_down = Math.Max(x - 1, 0);
                    int y_up = Math.Min(y + 1, sizeY - 1);
                    int y_down = Math.Max(y - 1, 0);
                    int z_up = Math.Min(z + 1, sizeZ - 1);
                    int z_down = Math.Max(z - 1, 0);

                    var hi_x = dataset.data[x_up + sizeX * (y + z * sizeY)];
                    var lo_x = dataset.data[x_down + sizeX * (y + z * sizeY)];
                    var hi_y = dataset.data[x + sizeX * (y_up + z * sizeY)];
                    var lo_y = dataset.data[x + sizeX * (y_down + z * sizeY)];
                    var hi_z = dataset.data[x + sizeX * (y + z_up * sizeY)];
                    var lo_z = dataset.data[x + sizeX * (y + z_down * sizeY)];
                    
                    var grad = new Vector3((hi_x - lo_x) / (float)maxRange, (hi_y - lo_y) / (float)maxRange, (hi_z - lo_z) / (float)maxRange);
                    colors[idx] = new Color(grad.x, grad.y, grad.z, (float)dataset.data[idx] / (float)dataset.maxDataValue);
                }
            }
        }
        
        dataset.texture.SetPixels(colors);
        dataset.texture.Apply();

        var tex = dataset.texture;

        const int noiseSizeX = 512;
        const int noiseSizeY = 512;
        var noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(noiseSizeX, noiseSizeY);

        var tf = tfFilePath == null ? null : TransferFunction.ReadFromFile(tfFilePath);
        if (tf == null)
        {
            tf = new TransferFunction();
            tf.AddControlPoint(new TFColorPoint(0.0f, new Color(0.11f, 0.14f, 0.13f, 1.0f)));
            tf.AddControlPoint(new TFColorPoint(0.2415f, new Color(0.469f, 0.354f, 0.223f, 1.0f)));
            tf.AddControlPoint(new TFColorPoint(0.3253f, new Color(1.0f, 1.0f, 1.0f, 1.0f)));
            tf.AddControlPoint(new TFAlphaPoint(0.0f, 0.0f));
            tf.AddControlPoint(new TFAlphaPoint(0.1787f, 0.0f));
            tf.AddControlPoint(new TFAlphaPoint(0.2f, 0.024f));
            tf.AddControlPoint(new TFAlphaPoint(0.28f, 0.03f));
            tf.AddControlPoint(new TFAlphaPoint(0.4f, 0.546f));
            tf.AddControlPoint(new TFAlphaPoint(0.547f, 0.5266f));
        }
        else
        {
            volObj.tfFilePath = tfFilePath;
        }

        tf.GenerateTexture();
        var tfTexture = tf.GetTexture();
        volObj.transferFunction = tf;

        tf.histogramTexture = HistogramTextureGenerator.GenerateHistogramTexture(dataset);
        
        var tf2D = tf2DFilePath == null ? null : TransferFunction2D.ReadFromFile(tf2DFilePath);
        if (tf2D == null)
        {
            tf2D = new TransferFunction2D();
            tf2D.AddBox(0.05f, 0.1f, 0.8f, 0.7f, Color.white, 0.0f, 0.4f);
        }
        else
        {
            volObj.tf2DFilePath = tf2DFilePath;
        }
        volObj.transferFunction2D = tf2D;
        if (enable2D)
        {
            tf2D.GenerateTexture();
            tfTexture = tf2D.GetTexture();
        }
        
        meshRenderer.sharedMaterial.SetTexture("_DataTex", tex);
        meshRenderer.sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
        meshRenderer.sharedMaterial.SetTexture("_TFTex", tfTexture);

        return volObj;
    }
}
