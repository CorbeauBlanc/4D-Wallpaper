﻿using System.Threading;

using UnityEngine;
using UnityToolbag;

public class CanvasBehaviour : MonoBehaviour
{
    public int camWidth = 720;
    public int camHeight = 480;
    public int camFps = 30;
    public bool showWebCam = false;

    //public Text debugText;

    public bool webCamInitialized { get; private set; } = false;

    public LoadingTextBehaviour loadingText;
	//public RawImage mainPic;

    private Thread initThread;

    public void ToggleWebCam() {
		showWebCam = !showWebCam;
	}

	private void StartEmguInit() {
		WebCamManager.instance.InitializeCameraAndClassifier(loadingText);
		Dispatcher.Invoke(() => {
			loadingText.HideLoadingText();
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
