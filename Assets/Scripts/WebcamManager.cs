using System;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Cuda;
using Emgu.CV.Structure;

using UnityEngine;
using UnityToolbag;

public class WebCamManager
{
	#region Singleton settings
	private static WebCamManager _instance;
	public static WebCamManager instance {
		get {
			if (_instance == null)
				_instance = new WebCamManager();
			return _instance;
		}
	}
	private WebCamManager() { }
	#endregion

	public Vector3 userPosition {
		get {
			this.newFaceDetected = false;
			return this._userPosition;
		}
		private set {
			this._userPosition = value;
		}
	}
	private Vector3 _userPosition;

	public static int camWidth;
	public static int camHeight;
	public static int camFps;
	public static int camFov;

	public bool webcamFeedbackEnabled = false;
	public float userFaceSize = 15;
	public bool newFaceDetected = false;

	private CameraBehaviour inGameCamera;
	private Texture2D currentFrame;
	private Rectangle currentFace;
	private VideoCapture webcam;
	private CudaCascadeClassifier haarCascade;
	private float camDistanceRatio;

	public void setInGameCamera(CameraBehaviour cam) {
		this.inGameCamera = cam;
	}

	public void InitializeCameraAndClassifier(LoadingTextBehaviour loadingText) {
		if (!CudaInvoke.HasCuda) throw new Exception("Error! Cuda not detected!");

		loadingText.loadingMsg = "Creating buffers";
		Dispatcher.Invoke(() => {
			this.currentFace = new Rectangle(0, 0, -1, -1);
			this.currentFrame = new Texture2D(camWidth, camHeight, TextureFormat.RGB24, false);
			this.userPosition = new Vector3(
				inGameCamera.transform.position.x / inGameCamera.scaleFactor,
				inGameCamera.transform.position.y / inGameCamera.scaleFactor,
				inGameCamera.transform.position.z / inGameCamera.scaleFactor
			);
		});

		Debug.Log("*************Loading Haar cascade");
		loadingText.loadingMsg = "Loading Haar cascade";

		this.haarCascade = new CudaCascadeClassifier(@"C:\haarcascade_frontalface_default.xml");

		Debug.Log("*************Setting Haar cascade properties");
		loadingText.loadingMsg = "Initializing Haar cascade";

		this.haarCascade.ScaleFactor = 1.005;
		this.haarCascade.MinNeighbors = 20;
		this.haarCascade.MinObjectSize = Size.Empty;
		this.haarCascade.MaxNumObjects = 1;
		this.haarCascade.FindLargestObject = true;

		Debug.Log("*************Loading webcam");
		loadingText.loadingMsg = "Loading Webcam";

		try {
			this.webcam = new VideoCapture(inGameCamera.camIndex, VideoCapture.API.DShow);
		}
		catch (System.Exception error) {
			Debug.LogError(error);
			throw;
		}

		Debug.Log("*************Setting webcam properties");
		loadingText.loadingMsg = "Initializing Webcam";

		this.webcam.SetCaptureProperty(CapProp.Fps, camFps);
		this.webcam.SetCaptureProperty(CapProp.FrameHeight, camHeight);
		this.webcam.SetCaptureProperty(CapProp.FrameWidth, camWidth);
		this.webcam.SetCaptureProperty(CapProp.Buffersize, 2);
		this.camDistanceRatio = (camWidth * camFov / inGameCamera.horizontalFov) / Mathf.Tan(Mathf.Deg2Rad * camFov / 2);


		Debug.Log("*************Loading event handler");
		loadingText.loadingMsg = "Loading event handler";

		this.webcam.ImageGrabbed += ProcessFrame;

		Debug.Log("*************Starting capture");
	}

	public void StartCapture() {
		this.webcam.Start();
		this.inGameCamera.enableDraw();
		Debug.Log("*************Capture started");
	}

	public void StopCapture() {
		this.webcam.Stop();
		this.inGameCamera.disableDraw();
		Debug.Log("*************Capture stopped");
	}

	public void DestroyCamera() {
		this.webcam.Dispose();
		Debug.Log("*************Camera destroyed");
	}

	private bool IsRegionValid(Rectangle face) {
		if (currentFace.Width < 0) return true;

		float zMovementRatio = (float)Mathf.Max(face.Width, currentFace.Width) / (float)Mathf.Min(face.Width, currentFace.Width);
		return zMovementRatio < inGameCamera.maxZMovementRatio && zMovementRatio > inGameCamera.minZMovementRatio;
	}

	private void ProcessFrame(object sender, EventArgs e) {
		var mat = new Mat();
		this.webcam.Read(mat);

		CudaImage<Bgr, Byte> gpuImg = new CudaImage<Bgr, byte>();
		gpuImg.Upload(mat);
		CudaImage<Gray, Byte> grayImg = gpuImg.Convert<Gray, Byte>();
		GpuMat region = new GpuMat();
		haarCascade.DetectMultiScale(grayImg, region);
		Rectangle[] faceRegion = haarCascade.Convert(region);

		Rectangle face;
		if (faceRegion.Length > 0 && faceRegion[0].Width > 0) {
			if (!IsRegionValid(faceRegion[0])) return;

			face = faceRegion[0];
			float meterPerPxl = (userFaceSize / face.Width) / 100f;
			this._userPosition.x = -(face.X + (face.Width / 2) - (camWidth / 2)) * ((userFaceSize / face.Width) / 100);
			this._userPosition.y = -(face.Y + (face.Height / 2) - (camHeight / 2)) * ((userFaceSize / face.Width) / 100);
			this._userPosition.z = -camDistanceRatio * ((userFaceSize / face.Width) / 100);
			currentFace = face;
			this.newFaceDetected = true;
		}
		else currentFace.Width = -1;

		/*if (webcamFeedbackEnabled) {
			var img = mat.ToImage<Bgr, byte>();
			for (int i = 0; i < faceRegion.Length; i++) {
				if (i == 0)
					img.Draw(face, new Bgr(255, 255, 0), 4);
				else
					img.Draw(faceRegion[i], new Bgr(0, 255, 255), 4);
			}

			Dispatcher.InvokeAsync(() => {
				Debug.Log(img.Convert<Rgb, byte>().Bytes.Length);
				currentFrame.LoadRawTextureData(img.Convert<Rgb, byte>().Bytes);
				currentFrame.Apply();
				img.Dispose();
			});
		}*/
	}
}
