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
    private float spatialMappingLastTime = 30;

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

            /*
             * use BitmapStorage() method to save bitmap image 
             * in TableKeyboard/localAppData/Template, this method
             * should be called in main thread
              
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                //save 12 image in local app data file
                if (index_num % 10 == 0 && index_num > 60 && index_num <= 180)
                {
                    SoftwareBitmap newSoftwareBitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                    BitmapStorage(newSoftwareBitmap);
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

    private async void BitmapStorage(SoftwareBitmap softwareBitmap)
    {
        Debug.Log("Enter BitmapStorage");
        StorageFile outputFile = await FindTempStorageFileAsync();
        using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
        {
            // Create an encoder with the desired format
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

            // Set the software bitmap
            encoder.SetSoftwareBitmap(softwareBitmap);

            // Set additional encoding parameters, if needed
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            encoder.IsThumbnailGenerated = true;

            try
            {
                Debug.Log("Begin FlushAsync ");
                await encoder.FlushAsync();
            }
            catch (Exception err)
            {
                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                switch (err.HResult)
                {
                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                        // If the encoder does not support writing a thumbnail, then try again
                        // but disable thumbnail generation.
                        encoder.IsThumbnailGenerated = false;
                        break;
                    default:
                        throw;
                }
            }

            if (encoder.IsThumbnailGenerated == false)
            {
                await encoder.FlushAsync();
            }


        }
    } 

    private async Task<StorageFile> FindTempStorageFileAsync()
    {
        Debug.Log("Enter FindTempStorageFileAsync");
        StorageFile outputFile = null;
        StorageFolder temperoryStorageFolder = ApplicationData.Current.TemporaryFolder;
        String outputFileName = Time.time.ToString() + "depthDataImage.jpg";
        outputFile = await temperoryStorageFolder.CreateFileAsync(outputFileName, CreationCollisionOption.GenerateUniqueName);
        return outputFile;
    }
#endif
}
