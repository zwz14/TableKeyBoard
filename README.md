TableKeyBoard APP in C#

---------- Purpose ----------

final purpose: develop AR keyboard and recognize typing word using depth camera image

current: get depth image and IR image from depth camera and simply process

---------- version ----------

Unity 2017.4.8f1

Visual Studio 2017

---------- procedure ----------

run /App on Hololens, look around to scan environment, wait for a while, then plane will show depth image, type keyboard to move, type again to put keyboard on surface in your environment.


---------- Assets/Scripts file ----------

DepthDataDisplayManager.cs: get depth and IR image, display on a plane Object and store image in Hololens

DebugWindow.cs: show debug message on 3D Text Object, used for debugging

ReadBytesFile.cs: load z_compensate.bytes from Assets/Resource


---------- other materials ----------

1) how to start developing on Hololens

   official guide: https://docs.microsoft.com/zh-cn/windows/mixed-reality/academy

   MixedRealityToolkit: https://github.com/Microsoft/MixedRealityToolkit-Unity

2) how to get data from depth camera from Hololens
    
   official project: https://github.com/Microsoft/HoloLensForCV

   foundations:

   	Media Foundation: https://docs.microsoft.com/zh-cn/windows/desktop/medfound/media-foundation-programming-guide

   	Hololens Research Mode: https://docs.microsoft.com/en-us/windows/mixed-reality/research-mode

   github project: https://github.com/akihiro0105/HoloLensResearchmodeDemoWithUnity
                   
		   http://akihiro-document.azurewebsites.net/post/hololens_researchmode2/

3) why change 8 high bits with low 8 bits 

   refer to https://github.com/Microsoft/HoloLensForCV/issues/19 answer by 'jiying61306'

4) where the depth and IR image store in

	access Hololens with Windows Device Portal(use IP), in System/File explorer/LocalAppData/TableKeyBoard/TempState

5) where I get keyboard model

   I model keyboard in Solidworks and convert format to .FBX in 3DMax.
