using UnityEngine;
using System;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DicomImageUI : MonoBehaviour
{
    [SerializeField] private string folder;
    [SerializeField] private Transform axialImage;
    [SerializeField] private Transform coronalImage;
    [SerializeField] private Transform sagittalImage;
    [SerializeField] private TextMeshProUGUI axialValueText;
    [SerializeField] private TextMeshProUGUI coronalValueText;
    [SerializeField] private TextMeshProUGUI sagittalValueText;

    [SerializeField] private GameObject model3D;

    [SerializeField] private GameObject xrOrigin;

    private int[] imageShape;

    private int axialSlice = 0;

    private int coronalSlice = 0;
    private int sagittalSlice = 0;

    private DicomUtils dicomUtils;
    private void Awake() {
        dicomUtils = new DicomUtils(folder);
        imageShape = dicomUtils.GetImageShape();
        Debug.Log("Image Shape: " + '(' + imageShape[0] + ',' + imageShape[1] + ',' +imageShape[2] + ')');
    }

    private void Start() {
        ChangeAxialSlice(0.5f);
        ChangeCoronalSlice(0.5f);
        ChangeSagittalSlice(0.5f);
    }
    
    public void ChangeAxialSlice(float slice) {
        axialImage.GetComponent<UnityEngine.UI.Image>().sprite = dicomUtils.GetSpriteFromTexture(dicomUtils.GetAxialSliceTexture((int)(slice * imageShape[2])));
        axialValueText.text = Convert.ToInt32(imageShape[2] *slice + 1).ToString() + "/" + imageShape[2].ToString();
    }

    public void ChangeCoronalSlice(float slice) {
        coronalImage.GetComponent<UnityEngine.UI.Image>().sprite = dicomUtils.GetSpriteFromTexture(dicomUtils.GetCoronalSliceTexture((int)(imageShape[0] * slice)));
        coronalValueText.text = Convert.ToInt32(imageShape[0] * slice + 1).ToString() + "/" + imageShape[0].ToString();
    }

    public void ChangeSagittalSlice(float slice) {
        sagittalImage.GetComponent<UnityEngine.UI.Image>().sprite = dicomUtils.GetSpriteFromTexture(dicomUtils.GetSagittalSliceTexture((int)(imageShape[1] * slice)));
        sagittalValueText.text = Convert.ToInt32(imageShape[1] * slice + 1).ToString() + "/" + imageShape[1].ToString();
    }

    private void Update() {
        if (ToolsPanelUI.Instance.GetNavigation() == ToolsPanelUI.Navigation.Inside) {
            Vector3 distance = xrOrigin.transform.position - model3D.transform.position;    
            float axialDistance = (5 + distance.y*1000/10)*imageShape[2]/10;
            axialDistance = Mathf.Clamp(axialDistance, 0, imageShape[2] - 1);

            Debug.Log("Distance from center:" + distance);

            float sagittalDistance = 287*(distance.x*100 + 8)/12 + 85;
            sagittalDistance = Mathf.Clamp(sagittalDistance, 0, imageShape[1]);

            float coronalDistance = 253/14*(distance.z*100 + 10) + 175;
            coronalDistance = Mathf.Clamp(coronalDistance, 0, imageShape[0]);

            if((int)axialDistance != axialSlice ||  (int) sagittalDistance != sagittalSlice ||
                (int) coronalDistance != coronalSlice) {
                ChangeCoronalSlice(coronalDistance / imageShape[0]);
                ChangeSagittalSlice(sagittalDistance / imageShape[1]);
                ChangeAxialSlice(axialDistance / imageShape[2]);

                axialSlice = (int)axialDistance;
                sagittalSlice = (int)sagittalDistance;
                coronalSlice = (int) coronalDistance;
            }   
        }
    }
}
