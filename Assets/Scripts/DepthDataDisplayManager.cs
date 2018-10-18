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

    private Texture2D tex = null;
    private byte[] bytes = null;
    // Use this for initialization
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
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[0];

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
            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            mediaframereader.FrameArrived += FrameArrived;
            await mediaframereader.StartAsync();
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

            /*----------------*/


            /*
             * use pgmStorage() method to save bitmap image 
             * in TableKeyboard/localAppData/Template, this method
             * should be called in main thread
             */
            /*
           UnityEngine.WSA.Application.InvokeOnAppThread(() =>
           {

               //save 12 image in local app data file
               if (index_num % 10 == 0 && index_num > 180 && index_num <= 300)
               {                      
                   pgmStorage(softwarebitmap);
                   Debug.Log("succeed in saving image" + "-index_num:" + index_num.ToString());
               }
               index_num++;
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
            mediaframereference.Dispose();
        }
    }

    private async void pgmStorage(SoftwareBitmap softwareBitmap)
    {
        Debug.Log("Enter pgmStorage");
        StorageFile outputFile = await FindTempStorageFileAsync_pgm();
        // get pgm header
        int height = softwareBitmap.PixelHeight;
        int width = softwareBitmap.PixelWidth;
        int maxValue = 65535;
        String pgmHeader = "P5\n" + width + " " + height + "\n" + maxValue + "\n";
        // get pgm raw data
        byte[] temp_bytes = new byte[height * width * 2];
        softwareBitmap.CopyToBuffer(temp_bytes.AsBuffer());
        softwareBitmap.Dispose();
        temp_bytes = process_bytes(temp_bytes);
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

    private byte[] process_bytes(byte[] bytes)
    {
        int length = bytes.Length;
        for(int i = 0; i < length/2; i++)
        {
            byte temp = bytes[2 * i + 1];
            bytes[2 * i + 1] = bytes[2 * i];
            bytes[2 * i] = temp;
        }
        return bytes;
    }

    private async Task<StorageFile> FindTempStorageFileAsync_pgm()
    {
        Debug.Log("Enter FindTempStorageFileAsync");
        StorageFile outputFile = null;
        StorageFolder temperoryStorageFolder = ApplicationData.Current.TemporaryFolder;
        String outputFileName = Time.time.ToString() + ".pgm";
        outputFile = await temperoryStorageFolder.CreateFileAsync(outputFileName, CreationCollisionOption.GenerateUniqueName);
        return outputFile;
    }
#endif
}
