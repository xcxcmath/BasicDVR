using System;
using System.IO;
using UnityEngine;

public enum DataContentFormat
{
    Int8,
    Uint8,
    Int16,
    Uint16,
    Int32,
    Uint32
}

public class RawDatasetImporter : DatasetImporterBase
{
    private string filePath;
    private int sizeX, sizeY, sizeZ;
    private DataContentFormat contentFormat;
    private int skipBytes;

    public RawDatasetImporter(string filePath, int sizeX, int sizeY, int sizeZ, DataContentFormat contentFormat,
        int skipBytes)
    {
        this.filePath = filePath;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.contentFormat = contentFormat;
        this.skipBytes = skipBytes;
    }

    public override VolumeDataset Import()
    {
        var dataset = new VolumeDataset();

        dataset.sizeX = sizeX;
        dataset.sizeY = sizeY;
        dataset.sizeZ = sizeZ;
        
        var fs = new FileStream(filePath, FileMode.Open);
        var reader = new BinaryReader(fs);

        if (skipBytes > 0)
            reader.ReadBytes(skipBytes);

        int sz = sizeX * sizeY * sizeZ;
        dataset.texture = new Texture3D(sizeX, sizeY, sizeZ, TextureFormat.RGBAFloat, false);
        dataset.texture.wrapMode = TextureWrapMode.Clamp;
        dataset.data = new int[sz];

        int minVal = int.MaxValue;
        int maxVal = int.MinValue;
        int val = 0;
        for (int i = 0; i < sz; ++i)
        {
            switch (contentFormat)
            {
                case DataContentFormat.Int8:
                    val = (int) reader.ReadSByte();
                    break;
                case DataContentFormat.Int16:
                    val = (int) reader.ReadInt16();
                    break;
                case DataContentFormat.Int32:
                    val = (int) reader.ReadInt32();
                    break;
                case DataContentFormat.Uint8:
                    val = (int) reader.ReadByte();
                    break;
                case DataContentFormat.Uint16:
                    val = (int) reader.ReadUInt16();
                    break;
                case DataContentFormat.Uint32:
                    val = (int) reader.ReadUInt32();
                    break;
            }

            minVal = Math.Min(minVal, val);
            maxVal = Math.Max(maxVal, val);
            dataset.data[i] = val;
        }
        
        Debug.Log("Loaded dataset in range: " + minVal + " ~ " + maxVal);

        dataset.minDataValue = minVal;
        dataset.maxDataValue = maxVal;
        
        return dataset;
    }
}
