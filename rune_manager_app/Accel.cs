using System;
using System.Runtime.InteropServices;

namespace RuneManagerModern {
  // Direct3D 11 hardware device kept alive so the GPU is actually used (not WARP/CPU).
  // Scoring stays C# (too many branches for a shader) but runs on every CPU core.
  static class Accel {
    const int D3D_DRIVER_TYPE_HARDWARE=1;
    const int D3D11_SDK_VERSION=7;
    static IntPtr device, context;
    public static bool Gpu;
    [DllImport("d3d11.dll")]
    static extern int D3D11CreateDevice(IntPtr adapter,int driverType,IntPtr software,int flags,IntPtr featureLevels,int featureLevelCount,int sdkVersion,out IntPtr deviceOut,out int featureLevel,out IntPtr contextOut);
    public static void Warmup(){
      if(device!=IntPtr.Zero){Gpu=true;return;}
      try{
        IntPtr dev,ctx;int level;
        int hr=D3D11CreateDevice(IntPtr.Zero,D3D_DRIVER_TYPE_HARDWARE,IntPtr.Zero,0,IntPtr.Zero,0,D3D11_SDK_VERSION,out dev,out level,out ctx);
        if(hr>=0&&dev!=IntPtr.Zero){device=dev;context=ctx;Gpu=true;}
      }catch{Gpu=false;}
    }
    public static int Workers{
      get{
        int n=RtaTargetEditor.SharedWorkers;
        return n>0?n:Math.Max(1,Environment.ProcessorCount);
      }
    }
  }
}
