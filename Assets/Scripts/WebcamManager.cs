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

	public static int camWidth;
	public static int camHeight;
	public static int camFps;

	private VideoCapture webcam;
	private CudaCascadeClassifier haarCascade;
    private float camDistanceRatio;

	public Texture2D currentFrame;
	public Rectangle currentFace;
	public bool webcamFeedbackEnabled = false;
	public Vector3 userPosition;
	public float userFaceSize = 15;
	public CameraBehaviour inGameCamera;

	public void InitializeCameraAndClassifier(LoadingTextBehaviour loadingText) {
		if (!CudaInvoke.HasCuda) throw new Exception("Error! Cuda not detected!");

		loadingText.loadingMsg = "Creating buffers";
		Dispatcher.Invoke(() => {
			this.currentFace = new Rectangle(0, 0, -1, -1);
			this.currentFrame = new Texture2D(camWidth, camHeight, TextureFormat.RGB24, false);
			this.userPosition = new Vector3();
		});

		Debug.Log("*************Loading Haar cascade");
		loadingText.loadingMsg = "Loading Haar cascade";

		this.haarCascade = new CudaCascadeClassifier(@"C:\haarcascade_frontalface_default.xml");

		Debug.Log("*************Setting Haar cascade properties");
		loadingText.loadingMsg = "Initializing Haar cascade";

		this.haarCascade.ScaleFactor = 1.1;
		this.haarCascade.MinNeighbors = 10;
		this.haarCascade.MinObjectSize = Size.Empty;

		Debug.Log("*************Loading webcam");
		loadingText.loadingMsg = "Loading Webcam";

		this.webcam = new VideoCapture();

		Debug.Log("*************Setting webcam properties");
		loadingText.loadingMsg = "Initializing Webcam";

		this.webcam.SetCaptureProperty(CapProp.Fps, camFps);
		this.webcam.SetCaptureProperty(CapProp.FrameHeight, camHeight);
		this.webcam.SetCaptureProperty(CapProp.FrameWidth, camWidth);
        this.camDistanceRatio = (camWidth / 2) / Mathf.Tan(Mathf.Deg2Rad * 30);


        Debug.Log("*************Loading event handler");
		loadingText.loadingMsg = "Loading event handler";

		this.webcam.ImageGrabbed += new EventHandler(ProcessFrame);

		Debug.Log("*************Starting capture");
	}

	public void StartCapture() {
		this.webcam.Start();
        this.inGameCamera.drawEnabled = true;
		Debug.Log("*************Capture started");
	}

	public void StopCapture() {
		this.webcam.Stop();
        this.inGameCamera.drawEnabled = false;
        Debug.Log("*************Capture stopped");
	}

	public void DestroyCamera() {
		this.webcam.Dispose();
		Debug.Log("*************Camera destroyed");
	}

	private bool IsRegionValid(Rectangle face) {
		return currentFace.Width < 0 || ((float)face.Width / (float)currentFace.Width) > inGameCamera.maxZMovementRatio;
	}

	private void ProcessFrame(object sender, EventArgs e) {
		var mat = new Mat();
		this.webcam.Read(mat);

		var img = mat.ToImage<Bgr, byte>();
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
            userPosition.x = -(face.X + (face.Width / 2) - (camWidth / 2)) * ((userFaceSize / face.Width) / 100);
            userPosition.y = -(face.Y + (face.Height / 2) - (camHeight / 2)) * ((userFaceSize / face.Width) / 100);
            userPosition.z = -camDistanceRatio * ((userFaceSize / face.Width) / 100);
            currentFace = face;
		}
		else currentFace.Width = -1;

		if (webcamFeedbackEnabled) {
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
		}
	}
}
