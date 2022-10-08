using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class CustomBlurProcess : VolumeComponent //, IPostProcessComponent
{
    public IntParameter blurTimes = new ClampedIntParameter(1, 0, 5);
    public FloatParameter blurRange = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
    public IntParameter donwSample = new ClampedIntParameter(2, 1, 16);

    //public FloatParameter intensity = new ClampedFloatParameter(1, 0, 10);
    public FloatParameter centerX = new ClampedFloatParameter(0.5f, 0, 1);
    public FloatParameter centerY = new ClampedFloatParameter(0.5f, 0, 1);
}
