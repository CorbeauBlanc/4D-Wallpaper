using UnityEngine;
using UnityEngine.UI;

public class CamFeedbackBehaviour : MonoBehaviour
{
	public Text calibrationCross;
	public Text calibrationButtonLabel;

	private RawImage img;
	private bool calibrationMode = false;

	private void Start() {
		img = GetComponent<RawImage>();
	}

	public void ToggleCalibrationMode() {
		calibrationMode = !calibrationMode;
		img.rectTransform.sizeDelta = calibrationMode ?
			new Vector2(WebCamManager.camWidth, WebCamManager.camHeight) :
			new Vector2(320, 180);
		calibrationButtonLabel.text = calibrationMode ? "Done" : "Calibrate";
		calibrationCross.gameObject.SetActive(calibrationMode);
	}
}
