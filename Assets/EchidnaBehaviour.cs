using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class EchidnaBehaviour : MonoBehaviour
{
    [Serializable] struct Layer {
        public uint minWaitTime;
        public uint maxWaitTime;
        public string trigger;
        public string[] animations;
    }

	[SerializeField] private Animator animator;
    [SerializeField] private Layer[] layers;

    [HideInInspector] public bool lookAtCamera = false;

	private float[] nextAnimationsTime;

	private void ResetAnimationTime(int index) {
		nextAnimationsTime[index] = UnityEngine.Random.Range(layers[index].minWaitTime, layers[index].maxWaitTime + 1);
	}

    private void TriggerRandomAnimation(Layer layer) {
        var animIndex = UnityEngine.Random.Range(0, layer.animations.Length + 1);
        if (animIndex == layer.animations.Length)
            animator.SetBool(layer.trigger, false);
        else {
            animator.SetBool(layer.trigger, true);
            animator.SetTrigger(layer.animations[animIndex]);
        }
    }

	private void Start() {
		nextAnimationsTime = new float[layers.Length];
        for (var i = 0; i < layers.Length; ++i) ResetAnimationTime(i);
	}

	// Update is called once per frame
	private void Update()
	{
        for (var i = 0; i < layers.Length; ++i) {
            nextAnimationsTime[i] -= Time.deltaTime;
            if (nextAnimationsTime[i] < 0) {
                TriggerRandomAnimation(layers[i]);
                ResetAnimationTime(i);
            }
        }
    }
}
