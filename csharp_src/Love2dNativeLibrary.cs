using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;
using FileInfo = System.IO.FileInfo;
using System.Text;

namespace Love
{
    static public partial class Boot
    {
        static NativeLibraryUtil.FunctionAddrLoaderDelegate functionAddrLoader;
        public static IntPtr GetLibraryFunc(string name)
        {
            if (functionAddrLoader(name, out var funcPtr, out var errorInfo) == false)
            {
                throw new Exception($"load {name} error, info: {errorInfo}");
            }

            return funcPtr;
        }

        public static bool UseDebugLibs { get; set; } = false;

        public static void InitNativeLibrary()
        {
#if _LOVE_SHARP_DEBUG_SELF_GUID_f320fj032jf2j3fj2039
            var debug_loader = LibraryLoader.Load(new LibraryConfig()
            {
                Win32 = new LibraryContent[]
                {
                new LibraryContent("love.dll", () => System.IO.File.ReadAllBytes("love.dll")),
                },
            });
            functionAddrLoader = debug_loader.GetFunctionLoader("love.dll");
            Console.WriteLine(NativeLibraryUtil.NativlibTool.GetHashAssembly());
            return;
#endif
            
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);

            byte[] Load(string zipName, string entryName)
            {
                var zs = NativlibTool.GetEmbedResource((name) => name.Contains(zipName));
                return NativlibTool.GetZipFileContent(zs, entryName);
            }

            var winLibTableArray = new string[]
            {
                    "SDL2.dll",
                    "OpenAL32.dll",
                    "mpg123.dll",
                    "lua51.dll",
                    "love.dll",
            };

            var linuxLibTableArray = new string[]
            {
                    "usr/lib/libbrotlicommon.so.1",
                    "usr/lib/libbrotlidec.so.1",
                    "usr/lib/libbz2.so.1.0",
                    "usr/lib/libc.so.6",
                    "usr/lib/libfreetype.so.6",
                    "usr/lib/libgcc_s.so.1",
                    //"usr/lib/libgdiplus.so.0.0.0",
                    "usr/lib/libglib-2.0.so.0",
                    "usr/lib/libgraphite2.so.3",
                    "usr/lib/libharfbuzz.so.0",
                    "usr/lib/libluajit-5.1.so.2",
                    "usr/lib/libmodplug.so.1",
                    "usr/lib/libmpg123.so.0",
                    "usr/lib/libm.so.6",
                    "usr/lib/libogg.so.0",
                    "usr/lib/libopenal.so.1",
                    //"usr/lib/libpcre.so.1",
                    "usr/lib/libpng16.so.16",
                    //"usr/lib/libpthread.so.0",
                    "usr/lib/libSDL2-2.0.so.0",
                    "usr/lib/libstdc++.so.6",
                    "usr/lib/libtheoradec.so.1",
                    "usr/lib/libtheora.so.0",
                    "usr/lib/libvorbisfile.so.3",
                    "usr/lib/libvorbis.so.0",
                    "usr/lib/libz.so.1",
                    "usr/lib/libliblove.so"
            };

            var macLibTableArray = new string[]
            {
                    "Ogg",
                    "Theora",
                    "Vorbis",
                    "FreeType",
                    "SDL2",
                    "libmodplug",
                    "Lua",
                    "OpenAL-Soft",
                    "mpg123",
                    "love",
            };

            var pt = NativeLibraryUtil.LibraryLoader.GetPlatform(out var ixoader);
            Log.Info("Work on platform : " + pt);

            NativeLibraryUtil.LibraryContent[] LoadLibraryContent(string[] libTableArray, string zipName)
            {
                if (UseDebugLibs)
                    zipName += "_debug";
                return libTableArray.Select(libPath => new NativeLibraryUtil.LibraryContent(libPath, () => Load(zipName, libPath))).ToArray();
            }

            var config = new NativeLibraryUtil.LibraryConfig()
            {
                Linux64 = LoadLibraryContent(linuxLibTableArray, "native_lib_linux_x64"),
                Win32 = LoadLibraryContent(winLibTableArray, "native_lib_win_x86"),
                Win64 = LoadLibraryContent(winLibTableArray, "native_lib_win_x64"),
                Mac64 = LoadLibraryContent(winLibTableArray, "native_lib_mac_x64"),
            };

            var loader = NativeLibraryUtil.LibraryLoader.Load(config);

            switch (pt)
            {
                case NativeLibraryUtil.LibraryLoaderPlatform.Win32: functionAddrLoader = loader.GetFunctionLoader(winLibTableArray.Last()); break;
                case NativeLibraryUtil.LibraryLoaderPlatform.Win64: functionAddrLoader = loader.GetFunctionLoader(winLibTableArray.Last()); break;
                case NativeLibraryUtil.LibraryLoaderPlatform.Linux32: throw new Exception("unsupport platform : linux - 32 bit !");
                case NativeLibraryUtil.LibraryLoaderPlatform.Linux64: functionAddrLoader = loader.GetFunctionLoader(linuxLibTableArray.Last()); break;
                case NativeLibraryUtil.LibraryLoaderPlatform.Mac64: functionAddrLoader = loader.GetFunctionLoader(macLibTableArray.Last()); break;
                default: throw new Exception("unsupport platform !");
            }
        }
    }


    static class NativlibTool
    {
        public static System.IO.Stream GetEmbedResource(Func<string, bool> adj)
        {
            var assem = Assembly.GetExecutingAssembly();
            var names = assem.GetManifestResourceNames();
            var name = names.FirstOrDefault(adj);
            return name == null ? null : assem.GetManifestResourceStream(name);
        }

        public static byte[] GetZipFileContent(System.IO.Stream stream, string entryName) // CheckRewrite
        {
            using (var zipStream = stream)
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName == entryName)
                        {
                            using (var es = entry.Open())
                            {
                                return ReadFully(es);
                            }
                        }
                    }
                }
            }

            throw new FileNotFoundException($"Cannot find file {entryName} in lib stream!");
        }

        public static byte[] ReadFully(System.IO.Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (input)
            {
                if (input == null)
                {
                    throw new ArgumentNullException(nameof(input));
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
