using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowTime : MonoBehaviour {

    TextMesh textMesh;

	// Use this for initialization
	void Start () {
        textMesh = gameObject.GetComponent<TextMesh>();
	}
	
	// Update is called once per frame
	void Update () {
        int CurrentTime = (int)Time.time;
        textMesh.text = CurrentTime.ToString();
	}
}
