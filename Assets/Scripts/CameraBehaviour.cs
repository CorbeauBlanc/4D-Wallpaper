using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
	[SerializeField] private float screenWidth;
	[SerializeField] private float screenHeight;
	[SerializeField] private float minSimTriggerDistance;
	[SerializeField, Range(0, 1)] private float simTrustFactor = .7f;

	[Min(0)] public int camIndex = 0;
	public float scaleFactor = 1;
	public float minZMovementRatio;
	public float maxZMovementRatio;
	public float horizontalFov = 60;

	private Camera cam;
	private float tanCamFov;
	private Rect viewportRect;
	private bool drawEnabled = false;
	private Vector3[] positionsBuffer = new Vector3[3];
	private float simulatedTime = 0f;
	private float lastSimulatedTime = 0f;
	private Vector3 simulatedVelocity = Vector3.zero;
	private Vector3 simulatedPosition = Vector3.zero;

	public void enableDraw() {
		drawEnabled = true;
	}
	public void disableDraw() {
		drawEnabled = false;
	}

	// Start is called before the first frame update
	void Start() {
		WebCamManager.instance.setInGameCamera(this);
		this.cam = GetComponent<Camera>();
		tanCamFov = Mathf.Tan(Mathf.Deg2Rad * this.cam.fieldOfView / 2);
	}

	// Update is called once per frame
	void Update() {
		if (drawEnabled) {
			this.simulatedTime += Time.deltaTime;
			if (WebCamManager.instance.newFaceDetected) {
				updatePositionsBuffer(new Vector3(WebCamManager.instance.userPosition.x * scaleFactor,
				WebCamManager.instance.userPosition.y * scaleFactor,
				WebCamManager.instance.userPosition.z * scaleFactor));
				/* if (this.simulatedPosition != Vector3.zero && Vector3.Distance(this.positionsBuffer[0], this.simulatedPosition) > .3f)
					//Debug.Log(Vector3.Distance(this.positionsBuffer[0], this.simulatedPosition));
					Debug.Log(Vector3.Angle(this.positionsBuffer[0] - this.positionsBuffer[1], this.positionsBuffer[1] - this.positionsBuffer[2]) - Vector3.Angle(this.simulatedPosition - this.positionsBuffer[1], this.positionsBuffer[1] - this.positionsBuffer[2])); */
				this.transform.position = this.positionsBuffer[0];
				this.lastSimulatedTime = this.simulatedTime;
				this.simulatedTime = 0f;
				this.simulatedVelocity = Vector3.zero;
				this.simulatedPosition = Vector3.zero;
			} else if (canSimulatePosition()) {
				if (this.simulatedPosition == Vector3.zero) simulatePosition();
				this.transform.position = Vector3.SmoothDamp(
					this.transform.position,
					this.simulatedPosition,
					ref this.simulatedVelocity,
					this.lastSimulatedTime);
			}
			else return;
			viewportRect = GetNormalizedScreenRect();
			SetScissorRect(viewportRect);
		}
	}

	private void OnGUI() {
		if (Event.current.type.Equals(EventType.Repaint) && drawEnabled) {
			Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.cam.targetTexture, viewportRect, 0, 0, 0, 0);
		}
	}

	private bool canSimulatePosition() {
		return (this.positionsBuffer[2] != Vector3.zero &&
				this.positionsBuffer[1] != Vector3.zero &&
				this.positionsBuffer[0] != Vector3.zero &&
				Mathf.Abs(Vector3.Distance(this.positionsBuffer[2], this.positionsBuffer[1])) > this.minSimTriggerDistance &&
				Mathf.Abs(Vector3.Distance(this.positionsBuffer[1], this.positionsBuffer[0])) > this.minSimTriggerDistance);
	}

	private void updatePositionsBuffer(Vector3 newVect) {
		this.positionsBuffer[2] = this.positionsBuffer[1];
		this.positionsBuffer[1] = this.positionsBuffer[0];
		this.positionsBuffer[0] = newVect;
	}

	private void simulatePosition() {
		Vector3 v1 = this.positionsBuffer[1] - this.positionsBuffer[2];
		Vector3 v2 = this.positionsBuffer[0] - this.positionsBuffer[1];
		Vector3 crossV = Vector3.Cross(v1, v2);
		float angle = Vector3.SignedAngle(v1, v2, crossV);
		if (angle < 90 && angle > -90)
			angle = Mathf.LerpAngle(0, angle, this.simTrustFactor);
		else if (angle > 0)
			angle = Mathf.LerpAngle(180, angle, this.simTrustFactor);
		else angle = Mathf.LerpAngle(-180, angle, this.simTrustFactor);

		Vector3 newPos = (Quaternion.AngleAxis(angle, crossV) * v2) + this.positionsBuffer[0];
		this.simulatedPosition = Vector3.Lerp(this.positionsBuffer[0], newPos, this.simTrustFactor);
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
