using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessCameraBehaviour : MonoBehaviour
{
    public Camera mainCamera;

    private Camera postProcessCamera;

	// Start is called before the first frame update
	void Start()
	{
        this.postProcessCamera = GetComponent<Camera>();
	}

	// Update is called once per frame
	void Update()
	{
        this.transform.position = new Vector3(-mainCamera.transform.position.x, -mainCamera.transform.position.y, mainCamera.transform.position.z);
        this.postProcessCamera.fieldOfView = mainCamera.fieldOfView;
	}
}
