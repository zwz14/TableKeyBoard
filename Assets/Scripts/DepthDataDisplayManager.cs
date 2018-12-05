using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_UWP
using System;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public class DepthDataDisplayManager : MonoBehaviour
{

    private bool startMedia = false;
    private float StartTime;
    private float spatialMappingLastTime = new PlaySpaceManager().scanTime;
    // when index_num%10=0, save image
    private int index_num = 1;
    private int index_num1 = 1;
    private Texture2D tex = null;
    private byte[] bytes = null;

    void Start()
    {
        StartTime = Time.time;
    }

    public float get_smlTime()
    {
        return spatialMappingLastTime;
    }

    // Update is called once per frame
    void Update()
    {
        // After spatial mapping ends run Initsensor() only once
        if (!startMedia && Time.time - StartTime > spatialMappingLastTime)
        {
            startMedia = true;
            Debug.Log("Start Initsensor.");
#if UNITY_UWP
            InitSensor();
#endif
        }
    }

#if UNITY_UWP

    private async void InitSensor()
    {
        Debug.Log("Enter InitSensor");
        
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];

        Debug.Log("mediaFrameSourceGroup name:" + mediaFrameSourceGroup.DisplayName.ToString());

        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
        };

        try
        {
            await mediaCapture.InitializeAsync(settings);
            for(int i = 0; i < 2; i++)
            {
                var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[i];
                var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
                var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
                //Debug.Log("mediaFrameSource subtype" + i + " " + mediaFrameSource.CurrentFormat.Subtype.ToString());
                mediaframereader.FrameArrived += FrameArrived;
                await mediaframereader.StartAsync();
            }
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }

    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;           
            switch (mediaframereference.SourceKind.ToString())
            {
                case "Infrared":
                    /*
                     * use pgmStorage() method to save depth image 
                     * in TableKeyboard/localAppData/Template, this method
                     * should be called in main thread
                     */
                    /*
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {

                        //save image in local app data file
                        if (index_num % 10 == 0 && index_num > 100 && index_num <= 130)
                        {
                            pgmStorage(softwarebitmap, "Infrared");
                            Debug.Log("succeed in saving image" + "-index_num: Infrared" + index_num.ToString() + " " + Time.time.ToString());
                        }
                        index_num++;
                    }, true);
                    */                   
                    break;
                case "Depth":
                    /*
                     * use pgmStorage() method to save IR image 
                     * in TableKeyboard/localAppData/Template, this method
                     * should be called in main thread
                     */
                    /*
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {

                        //save image in local app data file
                        if (index_num % 10 == 0 && index_num > 100 && index_num <= 130)
                        {
                            pgmStorage(softwarebitmap, "Depth");
                            Debug.Log("succeed in saving image" + "-index_num: Depth" + index_num.ToString() + " " + Time.time.ToString());
                        }
                        index_num1++;
                    }, true);
                    */
                    if (softwarebitmap != null)
                    {
                        softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                        int w = softwarebitmap.PixelWidth;
                        int h = softwarebitmap.PixelHeight;
                        if (bytes == null)
                        {
                            bytes = new byte[w * h * 4];
                        }
                        softwarebitmap.CopyToBuffer(bytes.AsBuffer());
                        softwarebitmap.Dispose();
                        UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                            if (tex == null)
                            {
                                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                                GetComponent<Renderer>().material.mainTexture = tex;
                            }
                            for (int i = 0; i < bytes.Length / 4; ++i)
                            {
                                byte b = bytes[i * 4];
                                bytes[i * 4 + 0] = 0;
                                bytes[i * 4 + 1] = 0;
                                bytes[i * 4 + 2] = 0;
                                bytes[i * 4 + 3] = 255;
                                if (b == 0)
                                {
                                    bytes[i * 4 + 0] = 128;
                                }
                                if (b == 1)
                                {
                                    bytes[i * 4 + 0] = 255;
                                }
                            }
                            tex.LoadRawTextureData(bytes);

                            tex.Apply();
                        }, true);
                    }
                    
                    //mediaframereference.Dispose();
                    break;
            }
            
        }
    }

    private async void pgmStorage(SoftwareBitmap softwareBitmap, string sourceKind)
    {
        Debug.Log("Enter pgmStorage");
        StorageFile outputFile = await FindTempStorageFileAsync_pgm(sourceKind);
        // get pgm header
        int height = softwareBitmap.PixelHeight;
        int width = softwareBitmap.PixelWidth;
        int maxValue;
        byte[] temp_bytes;
        switch (sourceKind)
        {
            case "Infrared":
                maxValue = 255;
                temp_bytes = new byte[height * width];
                break;
            case "Depth":
                maxValue = 65535;
                temp_bytes = new byte[height * width * 2];
                break;
            default:
                maxValue = 65535;
                temp_bytes = new byte[height * width * 2];
                break;
        }      
        String pgmHeader = "P5\n" + width + " " + height + "\n" + maxValue + "\n";
        // get pgm raw data
        softwareBitmap.CopyToBuffer(temp_bytes.AsBuffer());
        softwareBitmap.Dispose();
        if(sourceKind == "Depth")
        {
            temp_bytes = process_bytes(temp_bytes);
        }
        // write image in pgm file
        var stream = await outputFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
        using (var outputStream = stream.GetOutputStreamAt(0))
        {
            using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
            {
                dataWriter.WriteString(pgmHeader);
                dataWriter.WriteBytes(temp_bytes);
                await dataWriter.StoreAsync();
                await outputStream.FlushAsync();
            }
        }
        stream.Dispose(); 
    }
    
    private async Task<StorageFile> FindTempStorageFileAsync_pgm(string sourceKind)
    {
        Debug.Log("Enter FindTempStorageFileAsync");
        StorageFile outputFile = null;
        StorageFolder temperoryStorageFolder = ApplicationData.Current.TemporaryFolder;
        String outputFileName = Time.time.ToString() + sourceKind + ".pgm";
        outputFile = await temperoryStorageFolder.CreateFileAsync(outputFileName, CreationCollisionOption.GenerateUniqueName);
        return outputFile;
    }

    // exchange high 8 bits with low 8 bits
    private byte[] process_bytes(byte[] bytes)
    {
        int length = bytes.Length;
        for (int i = 0; i < length / 2; i++)
        {
            byte temp = bytes[2 * i + 1];
            bytes[2 * i + 1] = bytes[2 * i];
            bytes[2 * i] = temp;
        }
        return bytes;
    }

#endif
}
