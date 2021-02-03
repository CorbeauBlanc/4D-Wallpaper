using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingTextBehaviour : MonoBehaviour
{
	public string loadingMsg = "Loading";
	public double animationTime = .5;

	private string currentDotTxt = "...";
	private float lastTime = 0;
	private Text loadingText;

	public void HideLoadingText() {
		this.gameObject.SetActive(false);
	}

	// Start is called before the first frame update
	void Start()
	{
		loadingText = GetComponent<Text>();
	}

	// Update is called once per frame
	void Update()
	{
		lastTime += Time.deltaTime;
		if (lastTime > animationTime) {
			lastTime = 0;
			switch (currentDotTxt) {
				case "   ":
					currentDotTxt = ".  ";
					break;
				case ".  ":
					currentDotTxt = ".. ";
					break;
				case ".. ":
					currentDotTxt = "...";
					break;
				case "...":
					currentDotTxt = "   ";
					break;
			}
		}
		loadingText.text = loadingMsg + currentDotTxt;
	}
}
