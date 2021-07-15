using System.Threading;

using UnityEngine;
using UnityToolbag;

public class CanvasBehaviour : MonoBehaviour
{
	[SerializeField] private int camWidth = 720;
	[SerializeField] private int camHeight = 480;
	[SerializeField] private int camFps = 30;
	[SerializeField] private int camFov = 60;
	[SerializeField] private bool showWebCam = false;

	//public Text debugText;

	[HideInInspector] public bool webCamInitialized { get; private set; } = false;

	public LoadingTextBehaviour loadingText;
	//public RawImage mainPic;

	private Thread initThread;

	public void ToggleWebCam() {
		showWebCam = !showWebCam;
	}

	private void StartEmguInit() {
		WebCamManager.instance.InitializeCameraAndClassifier(loadingText);
		Dispatcher.Invoke(() => {
			this.gameObject.SetActive(false);
		});
		WebCamManager.instance.StartCapture();
		webCamInitialized = true;
	}

	// Start is called before the first frame update
	void Start()
	{
		WebCamManager.camWidth = camWidth;
		WebCamManager.camHeight = camHeight;
		WebCamManager.camFps = camFps;
		WebCamManager.camFov = camFov;

		Cursor.visible = false;

		this.initThread = new Thread(new ThreadStart(StartEmguInit));
		this.initThread.Start();
	}

	// Update is called once per frame
	void Update()
	{
		//this.mainPic.enabled = this.showWebCam;
		//WebCamManager.instance.webcamFeedbackEnabled = this.showWebCam;
		//if (showWebCam) mainPic.texture = WebCamManager.instance.currentFrame;
	}

	private void OnDestroy() {
		if (!webCamInitialized)
			initThread.Abort();
		else {
			WebCamManager.instance.StopCapture();
			WebCamManager.instance.DestroyCamera();
		}
	}
}
