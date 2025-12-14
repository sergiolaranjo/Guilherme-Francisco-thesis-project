using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleManager : MonoBehaviour
{   
    public static ScaleManager Instance { get; private set; }

    [SerializeField] private float originalScale = 0.001f;
    [SerializeField] private GameObject model3D;

    public float previousScale;


    public float currentScale;

    private void Awake() {
        Instance = this;
        currentScale = model3D.transform.localScale.y;
    }

    private void Start() {
        GameManager.Instance.OnScaleChange += GameManager_OnScaleChange;
    }

    private void GameManager_OnScaleChange(object sender, GameManager.OnScaleChangeEventArgs e)
    {
        currentScale = model3D.transform.localScale.y;
    }


    public float GetRealMeasurement(float value, bool isVolume = false) {
        int metersToMilimetes = 1000;

        if (isVolume) {
            float scaleCorrection = originalScale / currentScale;
            return (float)((float) value * Math.Pow(scaleCorrection * metersToMilimetes, 3));
        }
        
        return value / (originalScale / currentScale) * metersToMilimetes;
    }    

    public float GetOriginalScale() {
        return originalScale;
    }
}
