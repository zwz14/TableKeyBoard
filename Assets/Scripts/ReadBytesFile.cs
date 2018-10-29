using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ReadBytesFile : MonoBehaviour {

    private int bytes_len = 201600;
    public float[] z_compensate_bytes;

	// Use this for initialization
	void Start () {
        ReadBytes();

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    //
    private void ReadBytes()
    {
        if (z_compensate_bytes == null)
        {
            z_compensate_bytes = new float[bytes_len];
        }
        TextAsset asset = Resources.Load("z_compensate") as TextAsset;
        Stream binary_stream = new MemoryStream(asset.bytes);
        BinaryReader br = new BinaryReader(binary_stream);
        for (int i = 0; i < bytes_len; i++)
        {
            z_compensate_bytes[i] = br.ReadSingle();
        }       
        Debug.Log("read bin file and first float: " + z_compensate_bytes[bytes_len-1].ToString());
        
    }
    
}
