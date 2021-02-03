using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
	public float maxZMovementRatio;
	public float screenWidth;
	public float screenHeight;
	public float scaleFactor = 1;

	public bool drawEnabled = false;

	private Camera cam;
	private float tanCamFov;
    private Rect viewportRect;

	// Start is called before the first frame update
	void Start()
	{
		WebCamManager.instance.inGameCamera = this;
		this.cam = GetComponent<Camera>();
		tanCamFov = Mathf.Tan(Mathf.Deg2Rad * this.cam.fieldOfView / 2);
    }

	// Update is called once per frame
	void Update()
	{
        if (drawEnabled) {
            this.transform.position = new Vector3(WebCamManager.instance.userPosition.x * scaleFactor,
                WebCamManager.instance.userPosition.y * scaleFactor,
                WebCamManager.instance.userPosition.z * scaleFactor);
            viewportRect = GetNormalizedScreenRect();
            SetScissorRect(viewportRect);
        }
    }

	private void OnGUI() {
        if (Event.current.type.Equals(EventType.Repaint) && drawEnabled) {
            Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.cam.targetTexture, viewportRect, 0, 0, 0, 0);
        }
	}

    private Rect GetNormalizedScreenRect() {
        RenderTexture rText = this.cam.targetTexture;
        float meterToPxl = rText.height / (2 * (-this.transform.position.z / scaleFactor) * tanCamFov);

		float width = (screenWidth / 100f) * meterToPxl;
		float height = (screenHeight / 100f) * meterToPxl;
		float x = (rText.width / 2) - (this.transform.position.x / scaleFactor) * meterToPxl - (width / 2);
		float y = (rText.height / 2) - (this.transform.position.y / scaleFactor) * meterToPxl - (height / 2);

		return new Rect(x / rText.width, y / rText.height, width / rText.width, height / rText.height);
	}

	// https://answers.unity.com/questions/134413/how-do-i-render-only-a-part-of-the-cameras-view.html
	private void SetScissorRect(Rect r) {
        if (r.x < 0) {
            r.width += r.x;
            r.x = 0;
        }

        if (r.y < 0) {
            r.height += r.y;
            r.y = 0;
        }

        r.width = Mathf.Min(1 - r.x, r.width);
        r.height = Mathf.Min(1 - r.y, r.height);

        this.cam.rect = new Rect(0, 0, 1, 1);
		this.cam.ResetProjectionMatrix();
		Matrix4x4 m = this.cam.projectionMatrix;
		this.cam.rect = r;
		Matrix4x4 m2 = Matrix4x4.TRS(new Vector3((1 / r.width - 1), (1 / r.height - 1), 0), Quaternion.identity, new Vector3(1 / r.width, 1 / r.height, 1));
		Matrix4x4 m3 = Matrix4x4.TRS(new Vector3(-r.x * 2 / r.width, -r.y * 2 / r.height, 0), Quaternion.identity, Vector3.one);
		this.cam.projectionMatrix = m3 * m2 * m;
	}
}
