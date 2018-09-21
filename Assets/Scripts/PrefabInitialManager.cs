using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInitialManager : MonoBehaviour {

    public GameObject DisplayPlane;
    public GameObject keyboard;
    public GameObject keyboard_verticle;
    private float StartTime;
    private float spatialMappingLastTime = new DepthDataDisplayManager().get_smlTime();
    private bool intialGameObject_unfinished = true;

    // Use this for initialization
    void Start () {
        StartTime = Time.time;
    }
	
    void intialGameObject()
    {
        GameObject kb_Obeject = Instantiate(keyboard);
        kb_Obeject.transform.position = new Vector3(0F, -0.6F, 1F);
        //wholeObeject.transform.eulerAngles = new Vector3(0F, 0F, 0F);
        //wholeObeject.transform.localScale = new Vector3(0F, 0F, 0F);
        GameObject kbv_Obeject = Instantiate(keyboard_verticle);
        kbv_Obeject.transform.position = new Vector3(0F, 0F, 1F);
    }

	// Update is called once per frame
	void Update () {
		if((Time.time - StartTime > spatialMappingLastTime) && intialGameObject_unfinished)
        {
            intialGameObject();
            intialGameObject_unfinished = false;
        }
	}
}
