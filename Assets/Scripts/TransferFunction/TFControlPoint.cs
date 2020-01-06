using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TFColorPoint
{
    public float dataValue;
    public Color colorValue;

    public TFColorPoint(float dataValue, Color colorValue)
    {
        this.dataValue = dataValue;
        this.colorValue = colorValue;
    }
}

public class TFAlphaPoint
{
    public float dataValue;
    public float alphaValue;

    public TFAlphaPoint(float dataValue, float alphaValue)
    {
        this.dataValue = dataValue;
        this.alphaValue = alphaValue;
    }
}